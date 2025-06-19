using System.IO;
using System.Text;
using DevOpsAssistant.Services;
using Xunit;

namespace DevOpsAssistant.Tests.Utils;

public class DocumentHelpersTests
{
    [Fact]
    public void ExtractText_Returns_Content_For_Markdown()
    {
        var text = "# Heading\nContent";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));

        var result = DocumentHelpers.ExtractText(stream, "test.md");

        Assert.Equal(text, result);
    }
}
