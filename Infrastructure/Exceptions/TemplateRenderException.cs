namespace Infrastructure.Exceptions;

public class TemplateRenderException : Exception
{
    public TemplateRenderException() { }
    public TemplateRenderException(string message) : base(message) { }
    public TemplateRenderException(string message, Exception inner) : base(message, inner) { }
}