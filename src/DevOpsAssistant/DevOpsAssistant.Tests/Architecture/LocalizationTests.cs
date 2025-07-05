using System.Text.RegularExpressions;
using Xunit;

namespace DevOpsAssistant.Tests.Architecture;

public class LocalizationTests
{
    private static readonly Regex PlainTextPattern = new(@">([^@<]*[A-Za-z][^@<]*)<", RegexOptions.Compiled);


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

        var insideCode = false;
        var braceDepth = 0;
        foreach (var line in File.ReadLines(file))
        {
            var trimmed = line.Trim();
            if (!insideCode && trimmed.StartsWith("@code"))
            {
                insideCode = true;
                braceDepth = trimmed.Count(c => c == '{') - trimmed.Count(c => c == '}');
                continue;
            }
            if (insideCode)
            {
                braceDepth += trimmed.Count(c => c == '{');
                braceDepth -= trimmed.Count(c => c == '}');
                if (braceDepth <= 0)
                {
                    insideCode = false;
                }
                if (insideCode)
                    continue;
            }

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
