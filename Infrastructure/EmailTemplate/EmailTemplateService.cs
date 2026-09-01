using Infrastructure.EmailTemplate.Interfaces;
using Microsoft.Extensions.Logging;
using Scriban;

namespace Infrastructure.EmailTemplate;

public class EmailTemplateService(ILogger<EmailTemplateService> logger) : IEmailTemplateService
{
    private readonly string _templatesFolder = Path.Combine(AppContext.BaseDirectory, "Infrastructure", "EmailTemplate", "Templates");

    public async Task<string> RenderTemplateAsync<T>(string templateName, T model)
    {
        var filePath = Path.Combine(_templatesFolder, $"{templateName}.html");

        if (!File.Exists(filePath))
        {
            logger.LogError("E-postmalen ble ikke funnet på stien: {FilePath}", filePath);
            throw new FileNotFoundException($"E-postmalen '{templateName}.html' ble ikke funnet.", filePath);
        }

        var templateSource = await File.ReadAllTextAsync(filePath);
        var template = Template.Parse(templateSource);

        if (template.HasErrors)
        {
            var errors = string.Join(", ", template.Messages.Select(m => m.Message));
            logger.LogError("Feil under kompilering av Scriban-mal {TemplateName}: {Errors}", templateName, errors);
            throw new InvalidOperationException($"Feil i Scriban-mal '{templateName}': {errors}");
        }

        var renderedHtml = await template.RenderAsync(model);
        return renderedHtml;
    }
}