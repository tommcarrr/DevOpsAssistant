using System.Text;

namespace DevOpsAssistant.Services;

public static class PromptHelpers
{
    public static IReadOnlyList<string> SplitPrompt(string text, int limit)
    {
        if (string.IsNullOrWhiteSpace(text) || limit <= 0 || text.Length <= limit)
            return new[] { text };

        var adjustedLimit = limit;
        var parts = SplitInternal(text, adjustedLimit);
        var prefixLength = PrefixLength(parts.Count);
        adjustedLimit = limit - prefixLength;

        while (adjustedLimit > 0 && parts.Any(p => p.Length > adjustedLimit))
        {
            parts = SplitInternal(text, adjustedLimit);
            var newPrefix = PrefixLength(parts.Count);
            if (newPrefix == prefixLength)
                break;

            prefixLength = newPrefix;
            adjustedLimit = limit - prefixLength;
        }

        for (int i = 0; i < parts.Count; i++)
        {
            parts[i] = $"[PART {i + 1}/{parts.Count}]\n" + parts[i];
        }
        return parts;
    }

    private static List<string> SplitInternal(string text, int limit)
    {
        var parts = new List<string>();
        var sb = new StringBuilder();
        foreach (var line in text.Split('\n'))
        {
            if (sb.Length + line.Length + 1 > limit && sb.Length > 0)
            {
                parts.Add(sb.ToString().TrimEnd());
                sb.Clear();
            }
            sb.AppendLine(line);
        }
        if (sb.Length > 0)
            parts.Add(sb.ToString().TrimEnd());

        return parts;
    }

    private static int PrefixLength(int count)
    {
        return $"[PART {count}/{count}]\n".Length;
    }
}
