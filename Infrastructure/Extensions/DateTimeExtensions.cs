namespace Infrastructure.Extensions;

public static class DateTimeExtensions
{
    private const string NorwegianDisplayFormat = "dd.MM.yyyy HH:mm";

    public static string ToNorwegianDisplayFormat(this DateTime dateTime)
    {
        return dateTime.ToString(NorwegianDisplayFormat);
    }
}
