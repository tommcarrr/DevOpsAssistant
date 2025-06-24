using DevOpsAssistant.Utils;
using Xunit;

namespace DevOpsAssistant.Tests.Utils;

public class TextHelpersTests
{
    [Fact]
    public void Sanitize_Removes_Html_And_Decodes_Entities()
    {
        var input = "  <div>Hello &amp; <b>World</b></div>\n";

        var result = TextHelpers.Sanitize(input);

        Assert.Equal("Hello & World", result);
    }

    [Fact]
    public void Sanitize_Collapses_Whitespace()
    {
        var input = "A    B\nC";

        var result = TextHelpers.Sanitize(input);

        Assert.Equal("A B C", result);
    }

    [Fact]
    public void Sanitize_Returns_Empty_For_NullOrWhitespace()
    {
        Assert.Equal(string.Empty, TextHelpers.Sanitize(null));
        Assert.Equal(string.Empty, TextHelpers.Sanitize("   "));
    }
}
