using Bunit;
using DevOpsAssistant.Pages;
using DevOpsAssistant.Tests.Utils;

namespace DevOpsAssistant.Tests.Pages;

public class NewProjectPageTests : ComponentTestBase
{
    [Fact]
    public void NewProject_Renders()
    {
        SetupServices();
        var cut = RenderComponent<NewProject>();
        Assert.Contains("New Project", cut.Markup);
    }
}
