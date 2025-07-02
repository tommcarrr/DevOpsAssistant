using Bunit;
using DevOpsAssistant.Pages;
using DevOpsAssistant.Tests.Utils;
using DevOpsAssistant.Services;
using System.Threading.Tasks;

namespace DevOpsAssistant.Tests.Pages;

public class WorkItemViewerPageTests : ComponentTestBase
{
    [Fact]
    public async Task WorkItemViewer_Renders()
    {
        var config = SetupServices(includeApi: true);
        await config.SaveAsync(new DevOpsConfig { Organization = "Org", Project = "Proj", PatToken = "token" });

        var exception = Record.Exception(() => RenderWithProvider<TestPage>());
        Assert.Null(exception);
    }

    private class TestPage : WorkItemViewer
    {
        protected override Task OnInitializedAsync() => Task.CompletedTask;
    }
}
