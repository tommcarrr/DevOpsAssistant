using System.Text;

namespace DevOpsAssistant.Services;

public static class PromptHelpers
{
    public static IReadOnlyList<string> SplitPrompt(string text, int limit)
    {
        if (string.IsNullOrWhiteSpace(text) || limit <= 0 || text.Length <= limit)
            return new[] { text };

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

        for (int i = 0; i < parts.Count; i++)
        {
            parts[i] = $"[PART {i + 1}/{parts.Count}]\n" + parts[i];
        }
        return parts;
    }
}
