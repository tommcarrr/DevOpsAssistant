using DevOpsAssistant.Services;

namespace DevOpsAssistant.Tests.Utils;

public class PromptHelpersTests
{
    [Fact]
    public void SplitPrompt_Respects_Limit()
    {
        var text = "line1\nline2\nline3";

        var result = PromptHelpers.SplitPrompt(text, 10);

        Assert.Equal(3, result.Count);
        Assert.StartsWith("[PART 1/3]", result[0]);
        Assert.StartsWith("[PART 2/3]", result[1]);
    }

    [Fact]
    public void SplitPrompt_Returns_Original_When_Limit_Zero()
    {
        var text = "test";

        var result = PromptHelpers.SplitPrompt(text, 0);

        Assert.Single(result);
        Assert.Equal(text, result[0]);
    }
}
