namespace Infrastructure.Exceptions;

public class EmailDeliveryException : Exception
{
    public EmailDeliveryException()
    {
    }

    public EmailDeliveryException(string message) : base(message)
    {
    }

    public EmailDeliveryException(string message, Exception inner) : base(message, inner)
    {
    }
}