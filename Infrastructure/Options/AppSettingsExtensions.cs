namespace Infrastructure.Options;

public static class AppSettingsExtensions
{
    public static string GetTermsLink(this AppSettings settings)
    {
        return $"{settings.FrontendUrl.TrimEnd('/')}/legal/terms";
    }
}
