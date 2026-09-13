using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;

namespace Infrastructure.TemplateService;

public class TemplateFileSystemLoader(string templatesFolder) : ITemplateLoader
{
    public string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName)
    {
        var fileName = templateName.EndsWith(".html", StringComparison.OrdinalIgnoreCase)
            ? templateName
            : $"{templateName}.html";

        return Path.Combine(templatesFolder, fileName);
    }

    public string Load(TemplateContext context, SourceSpan callerSpan, string templatePath)
    {
        return File.ReadAllText(templatePath);
    }

    public async ValueTask<string> LoadAsync(TemplateContext context, SourceSpan callerSpan, string templatePath)
    {
        return await File.ReadAllTextAsync(templatePath);
    }
}
