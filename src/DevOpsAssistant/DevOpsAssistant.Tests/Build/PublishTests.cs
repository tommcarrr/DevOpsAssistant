using System.Xml.Linq;

namespace DevOpsAssistant.Tests.Build;

public class PublishTests
{
    [Fact]
    public void Publish_Config_Is_Copied_During_Build()
    {
        var projectPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../DevOpsAssistant/DevOpsAssistant.csproj"));
        var doc = XDocument.Load(projectPath);
        var ns = doc.Root!.Name.Namespace;

        var contentItem = doc.Descendants(ns + "Content")
            .FirstOrDefault(e => e.Attribute("Update")?.Value == "staticwebapp.config.json");

        Assert.NotNull(contentItem);
        Assert.Equal("wwwroot/staticwebapp.config.json", contentItem!.Element(ns + "Link")?.Value);
        Assert.Equal("PreserveNewest", contentItem.Element(ns + "CopyToPublishDirectory")?.Value);
    }
}
