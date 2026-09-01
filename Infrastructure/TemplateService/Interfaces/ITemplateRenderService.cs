namespace Infrastructure.TemplateService.Interfaces;

public interface ITemplateRenderService
{
    Task<string> RenderTemplateAsync<T>(string templateName, T model);
}