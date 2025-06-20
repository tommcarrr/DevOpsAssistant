using System.Net;
using System.Text.RegularExpressions;

namespace DevOpsAssistant.Utils;

public static class TextHelpers
{
    public static string Sanitize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;
        var text = WebUtility.HtmlDecode(input);
        text = Regex.Replace(text, "<.*?>", string.Empty);
        text = Regex.Replace(text, "\\s+", " ");
        return text.Trim();
    }
}

