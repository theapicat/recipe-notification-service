using Contracts.Events;
using Infrastructure.EmailDelivery;
using Infrastructure.EmailDelivery.Configurations;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Exceptions;
using Infrastructure.State;
using Infrastructure.TemplateService.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

    [Theory]
    [InlineData("550 5.1.1 User unknown")]
    [InlineData("Recipient address rejected: Access denied")]
    [InlineData("Mailbox unavailable")]
    [InlineData("Address does not exist")]
    public async Task ProcessEmailWithRetryAsync_WhenHardBounce_ShouldPublishInvalidEmailDetectedEventAndAbort(
        string errorMessage)
    {
        // Arrange
        const string to = "ugyldig@example.com";
        const string subject = "Velkommen";
        const string body = "<html>Hei</html>";
        const string eventType = "UserRegisteredEvent";

        _emailDeliveryService
            .SendEmailAsync(to, subject, body, cancellationToken: Arg.Any<CancellationToken>())
            .ThrowsAsync(new EmailDeliveryException(errorMessage));

        // Act
        await _service.ProcessEmailWithRetryAsync(to, subject, body, eventType, CancellationToken.None);

        // Assert
        // Skal kun ha forsøkt 1 gang
        await _emailDeliveryService.Received(1)
            .SendEmailAsync(to, subject, body, cancellationToken: Arg.Any<CancellationToken>());

        // Skal publisere InvalidEmailDetectedEvent til Auth API
        await _publishEndpoint.Received(1)
            .Publish(Arg.Is<InvalidEmailDetectedEvent>(e =>
                    e.Email == to &&
                    e.Reason == errorMessage),
                Arg.Any<CancellationToken>());

        // Skal IKKE lagre i MongoDB
        await _failedNotificationRepository.DidNotReceiveWithAnyArgs()
            .AddAsync(Arg.Any<FailedNotification>(), Arg.Any<CancellationToken>());
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
    public async Task ProcessEmailWithRetryAsync_WhenAll5AttemptsFail_ShouldHtmlEncodeUntrustedFieldsInAdminAlert()
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

        // Assert: mottaker/emne/feilmelding skal HTML-encodes før de sendes til malmotoren
        await _templateRenderService.Received(1)
            .RenderTemplateAsync("AdminActions/PendingEmailAlert", Arg.Is<object>(model =>
                !model.ToString()!.Contains("<script>") &&
                !model.ToString()!.Contains("<img")));
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
}