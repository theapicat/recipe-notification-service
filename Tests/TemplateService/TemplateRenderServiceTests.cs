using Infrastructure.Exceptions;
using Infrastructure.TemplateService;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace Tests.TemplateService;

public class TemplateRenderServiceTests
{
    private readonly ILogger<TemplateRenderService> _logger = Substitute.For<ILogger<TemplateRenderService>>();
    private readonly TemplateRenderService _service;

    public TemplateRenderServiceTests()
    {
        _service = new TemplateRenderService(_logger);
    }

    [Fact]
    public async Task RenderTemplateAsync_WhenTemplateDoesNotExist_ShouldThrowTemplateRenderException()
    {
        // Arrange
        const string nonExistentTemplate = "NonExistentFolder/DoesNotExist";
        var model = new { name = "Test" };

        // Act & Assert
        var exception = await Should.ThrowAsync<TemplateRenderException>(() =>
            _service.RenderTemplateAsync(nonExistentTemplate, model));

        exception.Message.ShouldContain("ble ikke funnet");
    }

    [Theory]
    // User Actions
    [InlineData("UserActions/AccountDeletedByUser")]
    [InlineData("UserActions/ContactFormAdminNotification")]
    [InlineData("UserActions/ContactFormUserReceipt")]
    [InlineData("UserActions/PasswordChangedSecurityNotice")]
    [InlineData("UserActions/PasswordResetRequested")]
    [InlineData("UserActions/ResendEmailConfirmation")]
    [InlineData("UserActions/UserRegisteredWelcome")]
    [InlineData("UserActions/UserRegisteredWithGoogleWelcome")]
    // System Actions
    [InlineData("SystemActions/AccountDeletedBySystem")]
    [InlineData("SystemActions/Confirmation7DaysReminder")]
    [InlineData("SystemActions/Confirmation14DaysReminder")]
    [InlineData("SystemActions/Inactivity6MonthsWarning")]
    [InlineData("SystemActions/Inactivity1YearLocked")]
    // Admin Actions
    [InlineData("AdminActions/AdminCustomEmail")]
    [InlineData("AdminActions/EmailManuallyConfirmedByAdmin")]
    [InlineData("AdminActions/PendingEmailAlert")]
    [InlineData("AdminActions/UserAccountDeletedByAdmin")]
    [InlineData("AdminActions/UserDeletedAndBlacklistedByAdmin")]
    [InlineData("AdminActions/UserLockedByAdmin")]
    [InlineData("AdminActions/UserUnlockedByAdmin")]
    [InlineData("AdminActions/UserUpdatedByAdmin")]
    public async Task RenderTemplateAsync_AllExistingTemplates_ShouldCompileAndRenderWithoutErrors(string templateName)
    {
        // Arrange
        var testModel = new
        {
            name = "Test Person",
            email = "test@example.com",
            old_email = "gammel@example.com",
            new_email = "ny@example.com",
            confirmation_link = "https://kjokkenhylla.no/confirm?token=123",
            reset_link = "https://kjokkenhylla.no/reset?token=456",
            login_link = "https://kjokkenhylla.no/login",
            terms_link = "https://kjokkenhylla.no/legal/terms",
            frontend_url = "https://kjokkenhylla.no",
            subject = "Test emne",
            message = "Test melding",
            reason = "Upassende innhold",
            reason_details = "Spamming",
            deletion_reason = "Brukerforespørsel",
            changed_at = "10.10.2026 12:00",
            locked_at = "10.10.2026 12:00",
            unlocked_at = "10.10.2026 12:00",
            updated_at = "10.10.2026 12:00",
            submitted_at = "10.10.2026 12:00",
            deleted_at = "10.10.2026 12:00",
            device_info = "Chrome / Windows",
            ip_address = "127.0.0.1",
            recipient = "mottaker@example.com",
            error_message = "SMTP timeout"
        };

        // Act
        var renderedHtml = await _service.RenderTemplateAsync(templateName, testModel);

        // Assert
        renderedHtml.ShouldNotBeNullOrWhiteSpace();
        renderedHtml.ShouldContain("Kjøkkenhylla");
        renderedHtml.ShouldContain("<!DOCTYPE html>");
    }

    [Fact]
    public async Task RenderTemplateAsync_ShouldReplaceScribanPlaceholdersCorrectly()
    {
        // Arrange
        const string templateName = "UserActions/UserRegisteredWelcome";
        var model = new
        {
            name = "UnikTestBruker",
            confirmation_link = "https://kjokkenhylla.no/confirm?token=unik_token_999",
            terms_link = "https://kjokkenhylla.no/legal/terms"
        };

        // Act
        var html = await _service.RenderTemplateAsync(templateName, model);

        // Assert
        html.ShouldContain("UnikTestBruker");
        html.ShouldContain("https://kjokkenhylla.no/confirm?token=unik_token_999");
    }

    [Fact]
    public async Task RenderTemplateAsync_ShouldIncludeSharedLayoutWithTitleAndDefaultFooter()
    {
        // Arrange
        const string templateName = "UserActions/UserRegisteredWelcome";
        var model = new
        {
            name = "Test",
            confirmation_link = "https://kjokkenhylla.no/confirm",
            terms_link = "https://kjokkenhylla.no/legal/terms"
        };

        // Act
        var html = await _service.RenderTemplateAsync(templateName, model);

        // Assert: _Layout.html er inkludert med riktig <title> og standard-footer
        html.ShouldContain("<title>Velkommen til Kjøkkenhylla!</title>");
        html.ShouldContain("Med vennlig hilsen,");
        html.ShouldContain("Kjøkkenhylla-teamet");
    }

    [Fact]
    public async Task RenderTemplateAsync_ShouldUseCustomFooterNoteWhenTemplateOverridesIt()
    {
        // Arrange
        const string templateName = "AdminActions/PendingEmailAlert";
        var model = new
        {
            recipient = "mottaker@example.com",
            subject = "Test emne",
            error_message = "SMTP timeout"
        };

        // Act
        var html = await _service.RenderTemplateAsync(templateName, model);

        // Assert: denne malen setter footer_note, som skal overstyre standard-footeren i layouten
        html.ShouldContain("Automatisk systemvarsel fra recipe-notification-service.");
        html.ShouldNotContain("Med vennlig hilsen,");
    }
}