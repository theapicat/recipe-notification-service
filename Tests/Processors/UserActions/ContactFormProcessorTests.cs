using Contracts.Events.UserActions;
using Infrastructure.EmailDelivery.Configurations;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Processors.UserActions;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Tests.Processors.UserActions;

public class ContactFormProcessorTests
{
    private readonly ITemplateRenderService _templateRenderService = Substitute.For<ITemplateRenderService>();
    private readonly IPendingEmailService _pendingEmailService = Substitute.For<IPendingEmailService>();
    private readonly IOptions<SmtpSettings> _smtpSettings = Options.Create(new SmtpSettings 
    { 
        AdminNotificationEmail = "admin@kjokkenhylla.no" 
    });
    private readonly ILogger<ContactFormProcessor> _logger = Substitute.For<ILogger<ContactFormProcessor>>();

    private readonly ContactFormProcessor _processor;

    public ContactFormProcessorTests()
    {
        _processor = new ContactFormProcessor(
            _templateRenderService,
            _pendingEmailService,
            _smtpSettings,
            _logger);
    }

    [Fact]
    public async Task ProcessAsync_WhenSubmitted_ShouldSendEmailToBothAdminAndUserReceipt()
    {
        // Arrange
        const string adminHtml = "<html>Admin notification</html>";
        const string userHtml = "<html>Bruker kvittering</html>";

        _templateRenderService
            .RenderTemplateAsync("UserActions/ContactFormAdminNotification", Arg.Any<object>())
            .Returns(Task.FromResult(adminHtml));

        _templateRenderService
            .RenderTemplateAsync("UserActions/ContactFormUserReceipt", Arg.Any<object>())
            .Returns(Task.FromResult(userHtml));

        var @event = new ContactFormSubmittedEvent
        {
            Name = "Kari Nordmann",
            Email = "kari@example.com",
            Subject = "Spørsmål angående oppskrifter",
            Message = "Hvordan legger jeg til favoritter?",
            SubmittedAt = DateTime.UtcNow
        };

        // Act
        await _processor.ProcessAsync(@event, CancellationToken.None);

        // Assert 1: Admin-varsel
        await _pendingEmailService.Received(1)
            .ProcessEmailWithRetryAsync(
                "admin@kjokkenhylla.no",
                "[Kontaktskjema] Spørsmål angående oppskrifter",
                adminHtml,
                nameof(ContactFormSubmittedEvent),
                Arg.Any<CancellationToken>());

        // Assert 2: Kvittering til bruker
        await _pendingEmailService.Received(1)
            .ProcessEmailWithRetryAsync(
                "kari@example.com",
                "Takk for din henvendelse: Spørsmål angående oppskrifter",
                userHtml,
                nameof(ContactFormSubmittedEvent),
                Arg.Any<CancellationToken>());
    }
}