using DevOpsAssistant.Utils;

namespace DevOpsAssistant.Tests.Utils;

public class PromptHelpersTests
{
    [Fact]
    public void SplitPrompt_Respects_Limit()
    {
        var text = "line1\nline2\nline3";

        var limit = 16;
        var result = PromptHelpers.SplitPrompt(text, limit);

        Assert.True(result.Count > 1);
        foreach (var part in result)
        {
            Assert.True(part.Length <= limit);
        }
        Assert.StartsWith("[PART 1/" + result.Count + "]", result[0]);
    }

    [Fact]
    public void SplitPrompt_Returns_Original_When_Limit_Zero()
    {
        var text = "test";

        var result = PromptHelpers.SplitPrompt(text, 0);

        Assert.Single(result);
        Assert.Equal(text, result[0]);
    }

    [Fact]
    public void SplitPrompt_Normalizes_Windows_Newlines()
    {
        var text = "line1\r\nline2\r\nline3";

        var limit = 16;
        var result = PromptHelpers.SplitPrompt(text, limit);

        Assert.True(result.Count > 1);
        Assert.All(result, part => Assert.True(part.Length <= limit));
        Assert.All(result, part => Assert.Contains("\r\n", part));
    }

    [Fact]
    public void SplitPrompt_Splits_Long_Lines()
    {
        var text = new string('a', 50);

        var limit = 20;
        var result = PromptHelpers.SplitPrompt(text, limit);

        Assert.True(result.Count > 1);
        Assert.All(result, part => Assert.True(part.Length <= limit));
    }

    [Fact]
    public void SplitPrompt_Splits_When_Newline_Expansion_Exceeds_Limit()
    {
        var text = "line1\nline2\nline3";

        var limit = text.Length; // 17 characters
        var result = PromptHelpers.SplitPrompt(text, limit);

        Assert.True(result.Count > 1);
        Assert.All(result, part => Assert.True(part.Length <= limit));
    }
}
