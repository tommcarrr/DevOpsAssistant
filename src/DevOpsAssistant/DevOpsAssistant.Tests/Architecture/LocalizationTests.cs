using System.Text.RegularExpressions;
using Xunit;

namespace DevOpsAssistant.Tests.Architecture;

public class LocalizationTests
{
    private static readonly Regex PlainTextPattern = new(@">([^@<]*[A-Za-z][^@<]*)<", RegexOptions.Compiled);

    private static readonly HashSet<string> PendingFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Validation.razor",
        "WorkItemQuality.razor",
        "WorkItemSelector.razor",
        "WorkItemViewer.razor",
        "WorkItems.razor",
    };

    public static IEnumerable<object[]> RazorPageFiles()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../DevOpsAssistant"));
        return Directory.GetFiles(root, "*.razor", SearchOption.AllDirectories)
            .Where(f => !f.EndsWith("_Imports.razor", StringComparison.OrdinalIgnoreCase))
            .Select(f => new object[] { f });
    }

    [Theory]
    [MemberData(nameof(RazorPageFiles))]
    public void Razor_page_should_not_contain_plain_text(string file)
    {
        if (PendingFiles.Contains(Path.GetFileName(file)))
        {
            return; // Localization pending
        }

        var insideCode = false;
        foreach (var line in File.ReadLines(file))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("@code"))
            {
                insideCode = true;
            }
            if (insideCode && trimmed.StartsWith("}"))
            {
                insideCode = false;
            }
            if (insideCode)
                continue;

            foreach (Match m in PlainTextPattern.Matches(line))
            {
                var text = m.Groups[1].Value.Trim();
                if (string.IsNullOrWhiteSpace(text))
                    continue;
                if (!Regex.IsMatch(text, "[A-Za-z]"))
                    continue;
                Assert.Fail($"Untranslated text '{text}' in {file}");
            }
        }
    }
}
