using Infrastructure.Exceptions;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;
using Scriban;
using Scriban.Functions;
using Scriban.Runtime;

namespace Infrastructure.TemplateService;

public class TemplateRenderService(ILogger<TemplateRenderService> logger) : ITemplateRenderService
{
    private readonly string _templatesFolder = Path.Combine(AppContext.BaseDirectory, "TemplateService", "Templates");
    private readonly ITemplateLoader _templateLoader =
        new TemplateFileSystemLoader(Path.Combine(AppContext.BaseDirectory, "TemplateService", "Templates"));

    public async Task<string> RenderTemplateAsync<T>(string templateName, T model)
    {
        var filePath = Path.Combine(_templatesFolder, $"{templateName}.html");

        // 1. Manglende malfil
        if (!File.Exists(filePath))
        {
            logger.LogError("E-postmalen ble ikke funnet på stien: {FilePath}", filePath);
            throw new TemplateRenderException(
                $"E-postmalen '{templateName}.html' ble ikke funnet på stien: {filePath}");
        }

        string templateSource;
        try
        {
            templateSource = await File.ReadAllTextAsync(filePath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Klarte ikke å lese e-postmal fra disk: {FilePath}", filePath);
            throw new TemplateRenderException($"Lesefeil for e-postmalen '{templateName}.html'.", ex);
        }

        // 2. Syntaks- / parsing-feil i Scriban
        var template = Template.Parse(templateSource);

        if (template.HasErrors)
        {
            var errors = string.Join(", ", template.Messages.Select(m => m.Message));
            logger.LogError("Feil under kompilering av Scriban-mal {TemplateName}: {Errors}", templateName, errors);
            throw new TemplateRenderException($"Kompileringsfeil i Scriban-mal '{templateName}': {errors}");
        }

        // 3. Kjøretidsfeil under rendering
        try
        {
            var scriptObject = new ScriptObject();
            scriptObject.Import(model);
            EscapeStringValues(scriptObject);

            var context = new TemplateContext
            {
                TemplateLoader = _templateLoader,
                MemberRenamer = StandardMemberRenamer.Default
            };
            context.PushGlobal(scriptObject);

            return await template.RenderAsync(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Feil under rendering av Scriban-mal {TemplateName}", templateName);
            throw new TemplateRenderException($"Kjøretidsfeil under rendering av malen '{templateName}'.", ex);
        }
    }

    // Scriban escaper ikke automatisk. Alle strengverdier i modellen kommer utenfra (brukere, admin, klienter),
    // så de HTML-escapes her før rendering. Verdier som settes inne i malene (title, footer_note, capture content)
    // er ikke en del av modellen og berøres ikke.
    private static void EscapeStringValues(ScriptObject scriptObject)
    {
        foreach (var key in scriptObject.Keys.ToList())
        {
            if (scriptObject[key] is string value)
                scriptObject.SetValue(key, HtmlFunctions.Escape(value), false);
        }
    }
}