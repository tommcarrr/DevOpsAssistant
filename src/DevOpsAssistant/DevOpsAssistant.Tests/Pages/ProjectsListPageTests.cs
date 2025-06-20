using Bunit;
using DevOpsAssistant.Pages;
using DevOpsAssistant.Tests.Utils;

namespace DevOpsAssistant.Tests.Pages;

public class ProjectsListPageTests : ComponentTestBase
{
    [Fact]
    public async Task ProjectsList_Shows_Projects()
    {
        var config = SetupServices();
        await config.AddProjectAsync("One");
        await config.AddProjectAsync("Two");

        var cut = RenderComponent<ProjectsList>();

        Assert.Contains("One", cut.Markup);
        Assert.Contains("Two", cut.Markup);
    }
}
