namespace Infrastructure.EmailTemplate;

public class EmailTemplateService : IEmailTemplateService
{
    public Task RenderTemplateAsync<TModel>(string templateName, TModel model)
    {
        var dummyHtml = $"<h1>E-post fra mal: {templateName}</h1><p>Dette er en generert e-post.</p>";
        return Task.CompletedTask;
    }
}