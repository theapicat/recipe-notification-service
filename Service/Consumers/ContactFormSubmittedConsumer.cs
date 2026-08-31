using Contracts.Events;
using MassTransit;

// Importer e-posttjenesten din her (f.eks. IEmailService / IEmailSender)

namespace Service.Consumers;

public class ContactFormSubmittedConsumer(ILogger<ContactFormSubmittedConsumer> logger)
    : IConsumer<ContactFormSubmittedEvent>
{
    public async Task Consume(ConsumeContext<ContactFormSubmittedEvent> context)
    {
        var message = context.Message;

        logger.LogInformation(
            " [Mottatt fra RabbitMQ] Kontaktskjema fra: {Name} ({Email}) | Emne: '{Subject}'",
            message.Name,
            message.Email,
            message.Subject
        );

        // epost tjeneste her ;)
    }

    private static bool IsValidEmail(string email)
    {
        // Enkel sjekk – kan utvides med dypere verifisering ved behov
        return !string.IsNullOrWhiteSpace(email) && email.Contains("@");
    }
}