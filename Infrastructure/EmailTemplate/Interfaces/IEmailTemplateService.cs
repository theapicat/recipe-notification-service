namespace Infrastructure.EmailTemplate;

public interface IEmailTemplateService
{
    Task RenderTemplateAsync<TModel>(string templateName, TModel model);
}