namespace DevOpsAssistant.Utils;

public static class PromptHelpers
{
    private const string NewLine = "\r\n";

    public static IReadOnlyList<string> SplitPrompt(string text, int limit)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new[] { text };

        text = text.Replace("\r\n", "\n").Replace('\r', '\n');

        if (limit <= 0 || AdjustedLength(text) <= limit)
            return new[] { text.Replace("\n", NewLine) };

        var adjustedLimit = limit;
        var parts = SplitInternal(text, adjustedLimit);
        var prefixLength = PrefixLength(parts.Count);
        adjustedLimit = limit - prefixLength;

        while (adjustedLimit > 0 && parts.Any(p => AdjustedLength(p) > adjustedLimit))
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
            parts[i] = $"[PART {i + 1}/{parts.Count}]" + NewLine + parts[i].Replace("\n", NewLine);
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

    private static int PrefixLength(int totalPages)
    {
        // Calculate using the longest possible prefix
        return $"[PART {totalPages}/{totalPages}]".Length + NewLine.Length;
    }

    private static int AdjustedLength(string text)
    {
        return text.Length + text.Count(c => c == '\n');
    }
}
