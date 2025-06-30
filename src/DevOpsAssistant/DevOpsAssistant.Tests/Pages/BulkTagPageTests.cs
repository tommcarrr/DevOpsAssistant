using Bunit;
using DevOpsAssistant.Pages;
using DevOpsAssistant.Tests.Utils;
using DevOpsAssistant.Services;

namespace DevOpsAssistant.Tests.Pages;

public class BulkTagPageTests : ComponentTestBase
{
    [Fact]
    public async Task BulkTag_Renders()
    {
        var config = SetupServices(includeApi: true);
        await config.SaveAsync(new DevOpsConfig { Organization = "Org", Project = "Proj", PatToken = "token" });

        var exception = Record.Exception(() => RenderWithProvider<TestPage>());
        Assert.Null(exception);
    }

    private class TestPage : BulkTag
    {
        protected override Task OnInitializedAsync()
        {
            return Task.CompletedTask;
        }
    }
}
