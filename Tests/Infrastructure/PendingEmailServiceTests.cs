using Contracts.Events.SystemActions;
using Infrastructure.EmailDelivery;
using Infrastructure.EmailDelivery.Configurations;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Exceptions;
using Infrastructure.State;
using Infrastructure.TemplateService.Interfaces;
using MailKit.Net.Smtp;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Persistence.Entities;
using Persistence.Repositories.Interfaces;
using Shouldly;

namespace Tests.Infrastructure;

public class PendingEmailServiceTests
{
    private const string AdminEmail = "admin@kjokkenhylla.no";

    private readonly IEmailDeliveryService _emailDeliveryService = Substitute.For<IEmailDeliveryService>();

    private readonly IFailedNotificationRepository _failedNotificationRepository =
        Substitute.For<IFailedNotificationRepository>();

    private readonly ILogger<PendingEmailService> _logger = Substitute.For<ILogger<PendingEmailService>>();
    private readonly IPublishEndpoint _publishEndpoint = Substitute.For<IPublishEndpoint>();
    private readonly ITemplateRenderService _templateRenderService = Substitute.For<ITemplateRenderService>();

    private readonly IOptions<SmtpSettings> _smtpSettings =
        Options.Create(new SmtpSettings { AdminNotificationEmail = AdminEmail });

    private readonly PendingEmailService _service;
    private readonly NotificationStateStore _stateStore = new();

    public PendingEmailServiceTests()
    {
        _templateRenderService
            .RenderTemplateAsync("AdminActions/PendingEmailAlert", Arg.Any<object>())
            .Returns(Task.FromResult("<html>Admin varsel</html>"));

        _service = new PendingEmailService(
            _emailDeliveryService,
            _failedNotificationRepository,
            _stateStore,
            _publishEndpoint,
            _templateRenderService,
            _smtpSettings,
            _logger);
    }

    [Fact]
    public async Task ProcessEmailWithRetryAsync_WhenFirstAttemptSucceeds_ShouldNotRetryOrSaveToRepo()
    {
        // Arrange
        const string to = "bruker@example.com";
        const string subject = "Test emne";
        const string body = "<html>Test</html>";
        const string eventType = "TestEvent";

        // Act
        await _service.ProcessEmailWithRetryAsync(to, subject, body, eventType, CancellationToken.None);

        // Assert
        await _emailDeliveryService.Received(1)
            .SendEmailAsync(to, subject, body, cancellationToken: Arg.Any<CancellationToken>());

        await _failedNotificationRepository.DidNotReceiveWithAnyArgs()
            .AddAsync(Arg.Any<FailedNotification>(), Arg.Any<CancellationToken>());

        _stateStore.HasPendingNotifications.ShouldBeFalse();
    }

    [Fact]
    public async Task ProcessEmailWithRetryAsync_WhenSecondAttemptSucceeds_ShouldRetryOnceAndNotSaveToRepo()
    {
        // Arrange
        const string to = "bruker@example.com";
        const string subject = "Test emne";
        const string body = "<html>Test</html>";
        const string eventType = "TestEvent";

        // 1. kall: Kaster EmailDeliveryException
        // 2. kall: Returnerer Task.CompletedTask (suksess)
        _emailDeliveryService
            .SendEmailAsync(to, subject, body, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(
                _ => Task.FromException(new EmailDeliveryException("SMTP server connection timeout")),
                _ => Task.CompletedTask
            );

        // Act
        await _service.ProcessEmailWithRetryAsync(to, subject, body, eventType, CancellationToken.None);

        // Assert
        await _emailDeliveryService.Received(2)
            .SendEmailAsync(to, subject, body, cancellationToken: Arg.Any<CancellationToken>());

        await _failedNotificationRepository.DidNotReceiveWithAnyArgs()
            .AddAsync(Arg.Any<FailedNotification>(), Arg.Any<CancellationToken>());

        _stateStore.HasPendingNotifications.ShouldBeFalse();
    }

    private static EmailDeliveryException SmtpFailure(SmtpErrorCode errorCode, SmtpStatusCode statusCode,
        string serverMessage, string to = "ugyldig@example.com")
    {
        // Samme innpakning som EmailDeliveryService gjør rundt MailKit-unntaket
        return new EmailDeliveryException($"Klarte ikke å sende e-post til {to} med emne 'Test'",
            new SmtpCommandException(errorCode, statusCode, MailboxAddress.Parse(to), serverMessage));
    }

    [Theory]
    [InlineData(SmtpStatusCode.MailboxUnavailable, "5.1.1 User unknown")]
    [InlineData(SmtpStatusCode.MailboxUnavailable, "Recipient address rejected: User does not exist")]
    [InlineData(SmtpStatusCode.UserNotLocalTryAlternatePath, "User not local")]
    [InlineData(SmtpStatusCode.MailboxNameNotAllowed, "5.1.3 Bad recipient address syntax")]
    public async Task ProcessEmailWithRetryAsync_WhenHardBounce_ShouldPublishInvalidEmailDetectedEventAndAbort(
        SmtpStatusCode statusCode, string serverMessage)
    {
        // Arrange
        const string to = "ugyldig@example.com";
        const string subject = "Velkommen";
        const string body = "<html>Hei</html>";
        const string eventType = "UserRegisteredEvent";

        _emailDeliveryService
            .SendEmailAsync(to, subject, body, cancellationToken: Arg.Any<CancellationToken>())
            .ThrowsAsync(SmtpFailure(SmtpErrorCode.RecipientNotAccepted, statusCode, serverMessage, to));

        // Act
        await _service.ProcessEmailWithRetryAsync(to, subject, body, eventType, CancellationToken.None);

        // Assert
        // Skal kun ha forsøkt 1 gang
        await _emailDeliveryService.Received(1)
            .SendEmailAsync(to, subject, body, cancellationToken: Arg.Any<CancellationToken>());

        // Skal publisere InvalidEmailDetectedEvent til Auth API, med SMTP-serverens kode og svar som begrunnelse
        await _publishEndpoint.Received(1)
            .Publish(Arg.Is<InvalidEmailDetectedEvent>(e =>
                    e.Email == to &&
                    e.Reason == $"{(int)statusCode} {serverMessage}"),
                Arg.Any<CancellationToken>());

        // Skal IKKE lagre i MongoDB
        await _failedNotificationRepository.DidNotReceiveWithAnyArgs()
            .AddAsync(Arg.Any<FailedNotification>(), Arg.Any<CancellationToken>());
    }

    public static TheoryData<EmailDeliveryException> NotHardBounceFailures => new()
    {
        // Tekst som tidligere ga falske treff: "550" i adressen, "does not exist" i en vanlig feil
        new EmailDeliveryException("Klarte ikke å sende e-post til ola550@example.com med emne 'Test'",
            new TimeoutException("The operation has timed out.")),
        new EmailDeliveryException("SMTP host does not exist"),
        // Meldingen avvist (spam/innhold) - adressen kan være gyldig
        SmtpFailure(SmtpErrorCode.MessageNotAccepted, SmtpStatusCode.MailboxUnavailable,
            "5.7.1 Message rejected as spam"),
        // Mottaker avvist av policy (5.7.x), ikke fordi adressen er ugyldig
        SmtpFailure(SmtpErrorCode.RecipientNotAccepted, SmtpStatusCode.MailboxUnavailable,
            "5.7.1 Relay access denied"),
        // Midlertidig avvisning (4xx)
        SmtpFailure(SmtpErrorCode.RecipientNotAccepted, SmtpStatusCode.MailboxBusy, "4.2.1 Mailbox busy")
    };

    [Theory]
    [MemberData(nameof(NotHardBounceFailures))]
    public async Task ProcessEmailWithRetryAsync_WhenFailureIsNotHardBounce_ShouldRetryAndNotPublishInvalidEmail(
        EmailDeliveryException failure)
    {
        // Arrange
        const string to = "ola550@example.com";
        const string subject = "Test";
        const string body = "<html>Test</html>";

        // Første forsøk feiler, andre lykkes (holder testen kort; backoff er 2 s før forsøk 2)
        _emailDeliveryService
            .SendEmailAsync(to, subject, body, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException(failure), _ => Task.CompletedTask);

        // Act
        var wasDelivered =
            await _service.ProcessEmailWithRetryAsync(to, subject, body, "TestEvent", CancellationToken.None);

        // Assert: feilen ble behandlet som forbigående (nytt forsøk), og auth varsles ikke om ugyldig adresse
        wasDelivered.ShouldBeTrue();
        await _emailDeliveryService.Received(2)
            .SendEmailAsync(to, subject, body, cancellationToken: Arg.Any<CancellationToken>());

        await _publishEndpoint.DidNotReceiveWithAnyArgs()
            .Publish(Arg.Any<InvalidEmailDetectedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessEmailWithRetryAsync_WhenAll5AttemptsFail_ShouldSaveToRepoAndNotifyAdmin()
    {
        // Arrange
        const string to = "feilet@example.com";
        const string subject = "Inaktivitetsvarsel";
        const string body = "<html>Inaktiv</html>";
        const string eventType = "Inactivity6MonthsWarningEvent";
        const string errorMessage = "Connection refused to SMTP server";

        _emailDeliveryService
            .SendEmailAsync(to, subject, body, cancellationToken: Arg.Any<CancellationToken>())
            .ThrowsAsync(new EmailDeliveryException(errorMessage));

        // Act
        await _service.ProcessEmailWithRetryAsync(to, subject, body, eventType, CancellationToken.None);

        // Assert 1: Prøvd 5 ganger mot mottaker + 1 gang mot admin = totalt 6 SendEmailAsync-kall
        await _failedNotificationRepository.Received(1)
            .AddAsync(Arg.Is<FailedNotification>(n =>
                    n.RecipientEmail == to &&
                    n.Subject == subject &&
                    n.HtmlBody == body &&
                    n.EventType == eventType &&
                    n.RetryCount == 5 &&
                    n.LastErrorMessage == errorMessage),
                Arg.Any<CancellationToken>());

        // Assert 2: Admin skal ha fått overført kritisk e-postvarsel til konfigurert admin-adresse
        await _emailDeliveryService.Received(1)
            .SendEmailAsync(AdminEmail, Arg.Is<string>(s => s.Contains("[KRITISK]")), Arg.Any<string>(),
                cancellationToken: Arg.Any<CancellationToken>());

        // Assert 2b: Admin-varselet skal rendres via Scriban-malen, ikke bygges som rå streng
        await _templateRenderService.Received(1)
            .RenderTemplateAsync("AdminActions/PendingEmailAlert", Arg.Any<object>());

        // Assert 3: Tilstandsstore skal merkes med pending = true og notified = true
        _stateStore.HasPendingNotifications.ShouldBeTrue();
        _stateStore.HasNotifiedAdmin.ShouldBeTrue();
    }

    [Fact]
    public async Task ProcessEmailWithRetryAsync_WhenAll5AttemptsFail_ShouldPassRawFieldsToAdminAlert()
    {
        // Arrange
        const string to = "feilet@example.com";
        const string subject = "<script>alert(1)</script>";
        const string body = "<html>Test</html>";
        const string eventType = "TestEvent";
        const string errorMessage = "SMTP feil: <img src=x onerror=alert(1)>";

        _emailDeliveryService
            .SendEmailAsync(to, subject, body, cancellationToken: Arg.Any<CancellationToken>())
            .ThrowsAsync(new EmailDeliveryException(errorMessage));

        // Act
        await _service.ProcessEmailWithRetryAsync(to, subject, body, eventType, CancellationToken.None);

        // Assert: verdiene sendes uendret; TemplateRenderService escaper dem (ellers blir de escapet to ganger)
        await _templateRenderService.Received(1)
            .RenderTemplateAsync("AdminActions/PendingEmailAlert", Arg.Is<object>(model =>
                model.ToString()!.Contains(subject) &&
                model.ToString()!.Contains(errorMessage)));
    }

    [Fact]
    public async Task ProcessEmailWithRetryAsync_WhenAll5AttemptsFailAndAdminAlreadyNotified_ShouldNotResendAdminEmail()
    {
        // Arrange
        const string to = "feilet2@example.com";
        const string subject = "Test 2";
        const string body = "<html>Test 2</html>";
        const string eventType = "TestEvent";

        _stateStore.SetPendingStatus(true);
        _stateStore.SetAdminNotified(true); // Admin er allerede varslet for tidligere feil

        _emailDeliveryService
            .SendEmailAsync(to, subject, body, cancellationToken: Arg.Any<CancellationToken>())
            .ThrowsAsync(new EmailDeliveryException("SMTP Down"));

        // Act
        await _service.ProcessEmailWithRetryAsync(to, subject, body, eventType, CancellationToken.None);

        // Assert: MongoDB mottar ny feil
        await _failedNotificationRepository.Received(1)
            .AddAsync(Arg.Any<FailedNotification>(), Arg.Any<CancellationToken>());

        // Admin e-post skal IKKE ha blitt kalt på nytt
        await _emailDeliveryService.DidNotReceive()
            .SendEmailAsync(AdminEmail, Arg.Any<string>(), Arg.Any<string>(),
                cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessEmailWithRetryAsync_WhenRetryOfExistingNotificationSucceeds_ShouldDeleteThatDocument()
    {
        // Arrange
        var existingId = Guid.NewGuid();
        const string to = "retry@example.com";
        const string subject = "Emne";
        const string body = "<html>Body</html>";
        const string eventType = "UserRegisteredEvent";

        // Act
        var wasDelivered = await _service.ProcessEmailWithRetryAsync(to, subject, body, eventType,
            CancellationToken.None, existingId);

        // Assert
        wasDelivered.ShouldBeTrue();

        await _failedNotificationRepository.Received(1)
            .DeleteAsync(existingId, Arg.Any<CancellationToken>());

        await _failedNotificationRepository.DidNotReceiveWithAnyArgs()
            .AddAsync(Arg.Any<FailedNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task
        ProcessEmailWithRetryAsync_WhenRetryOfExistingNotificationFailsAgain_ShouldUpdateInPlaceNotDeleteOrDuplicate()
    {
        // Arrange
        var existingId = Guid.NewGuid();
        const string to = "retry-feilet@example.com";
        const string subject = "Emne";
        const string body = "<html>Body</html>";
        const string eventType = "UserRegisteredEvent";
        const string errorMessage = "SMTP timeout";

        _emailDeliveryService
            .SendEmailAsync(to, subject, body, cancellationToken: Arg.Any<CancellationToken>())
            .ThrowsAsync(new EmailDeliveryException(errorMessage));

        // Act
        var wasDelivered = await _service.ProcessEmailWithRetryAsync(to, subject, body, eventType,
            CancellationToken.None, existingId);

        // Assert: dokumentet skal ALDRI slettes automatisk når re-forsøket feiler
        wasDelivered.ShouldBeFalse();

        await _failedNotificationRepository.DidNotReceive()
            .DeleteAsync(existingId, Arg.Any<CancellationToken>());

        // Det eksisterende dokumentet oppdateres in-place - det opprettes IKKE et duplikat
        await _failedNotificationRepository.Received(1)
            .MarkRetryFailedAsync(existingId, errorMessage, 5, Arg.Any<CancellationToken>());

        await _failedNotificationRepository.DidNotReceiveWithAnyArgs()
            .AddAsync(Arg.Any<FailedNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task
        ProcessEmailWithRetryAsync_WhenRetryOfExistingNotificationHitsHardBounce_ShouldNotDeleteOrDuplicate()
    {
        // Arrange
        var existingId = Guid.NewGuid();
        const string to = "ugyldig@example.com";
        const string subject = "Emne";
        const string body = "<html>Body</html>";
        const string eventType = "UserRegisteredEvent";

        _emailDeliveryService
            .SendEmailAsync(to, subject, body, cancellationToken: Arg.Any<CancellationToken>())
            .ThrowsAsync(SmtpFailure(SmtpErrorCode.RecipientNotAccepted, SmtpStatusCode.MailboxUnavailable,
                "5.1.1 User unknown", to));

        // Act
        var wasDelivered = await _service.ProcessEmailWithRetryAsync(to, subject, body, eventType,
            CancellationToken.None, existingId);

        // Assert: hard bounce er heller ikke en vellykket levering - dokumentet skal forbli urørt
        wasDelivered.ShouldBeFalse();

        await _failedNotificationRepository.DidNotReceive()
            .DeleteAsync(existingId, Arg.Any<CancellationToken>());

        await _failedNotificationRepository.DidNotReceiveWithAnyArgs()
            .MarkRetryFailedAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>());

        await _failedNotificationRepository.DidNotReceiveWithAnyArgs()
            .AddAsync(Arg.Any<FailedNotification>(), Arg.Any<CancellationToken>());
    }
}