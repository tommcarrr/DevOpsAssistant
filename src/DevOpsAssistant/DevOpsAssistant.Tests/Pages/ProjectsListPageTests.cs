using Bunit;
using DevOpsAssistant.Pages;
using DevOpsAssistant.Tests.Utils;

namespace DevOpsAssistant.Tests.Pages;

public class ProjectsListPageTests : ComponentTestBase
{
    [Fact]
    public void ProjectsList_Shows_Projects()
    {
        var config = SetupServices();
        config.AddProjectAsync("One").GetAwaiter().GetResult();
        config.AddProjectAsync("Two").GetAwaiter().GetResult();

        var cut = RenderComponent<ProjectsList>();

        Assert.Contains("One", cut.Markup);
        Assert.Contains("Two", cut.Markup);
    }
}
