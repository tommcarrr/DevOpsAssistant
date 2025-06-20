using System.Globalization;

namespace DevOpsAssistant.Utils;

public static class DateHelpers
{
    public static string ToLocalDateString(this DateTime date)
    {
        return date.ToString("d", CultureInfo.CurrentCulture);
    }

    public static string ToLocalDateString(this DateTime? date)
    {
        return date?.ToString("d", CultureInfo.CurrentCulture) ?? string.Empty;
    }
}
