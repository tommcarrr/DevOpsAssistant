using Bunit;
using DevOpsAssistant.Pages;
using DevOpsAssistant.Tests.Utils;

namespace DevOpsAssistant.Tests.Pages;

public class HomePageTests : ComponentTestBase
{
    [Fact]
    public void Home_Has_WorkItems_Link()
    {
        SetupServices();
        var cut = RenderComponent<Home>();

        Assert.Contains("href=\"epics-features\"", cut.Markup);
        Assert.Contains("href=\"validation\"", cut.Markup);
    }
}