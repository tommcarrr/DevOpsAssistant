namespace DevOpsAssistant.Services;

public static class PromptHelpers
{
    private const char NewLine = '\n';

    public static IReadOnlyList<string> SplitPrompt(string text, int limit)
    {
        if (string.IsNullOrWhiteSpace(text) || limit <= 0 || text.Length <= limit)
            return new[] { text };

        text = text.Replace("\r\n", "\n").Replace('\r', '\n');

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
            parts[i] = $"[PART {i + 1}/{parts.Count}]" + NewLine + parts[i];
        }
        return parts;
    }

    private static List<string> SplitInternal(string text, int limit)
    {
        var parts = new List<string>();
        var remaining = text.Trim();

        while (remaining.Length > limit)
        {
            var splitPos = remaining.LastIndexOfAny(new[] { ' ', '\n' }, limit);
            if (splitPos <= 0)
                splitPos = limit;

            parts.Add(remaining.Substring(0, splitPos).TrimEnd());
            remaining = remaining.Substring(splitPos).TrimStart();
        }

        if (remaining.Length > 0)
            parts.Add(remaining);

        return parts;
    }

    private static int PrefixLength(int count)
    {
        return $"[PART {count}/{count}]".Length + 1; // +1 for newline
    }
}
