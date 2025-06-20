using System.Globalization;
using DevOpsAssistant.Services;
using DevOpsAssistant.Utils;

namespace DevOpsAssistant.Tests.Utils;

public class DateHelpersTests
{
    [Fact]
    public void ToLocalDateString_Uses_Current_Culture()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            var culture = new CultureInfo("fr-FR");
            CultureInfo.CurrentCulture = culture;
            var dt = new DateTime(2024, 1, 2);

            var result = dt.ToLocalDateString();

            Assert.Equal(dt.ToString("d", culture), result);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
