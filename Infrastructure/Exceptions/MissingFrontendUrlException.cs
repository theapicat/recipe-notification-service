namespace Infrastructure.Exceptions;

public class MissingFrontendUrlException : Exception
{
    public MissingFrontendUrlException()
    {
    }

    public MissingFrontendUrlException(string message) : base(message)
    {
    }

    public MissingFrontendUrlException(string message, Exception inner) : base(message, inner)
    {
    }
}