using Bunit;
using DevOpsAssistant.Pages;
using DevOpsAssistant.Tests.Utils;

namespace DevOpsAssistant.Tests.Pages;

public class StoryReviewPageTests : ComponentTestBase
{
    [Fact]
    public void StoryReview_Renders_With_PopoverProvider()
    {
        SetupServices(includeApi: true);

        var exception = Record.Exception(() => RenderWithProvider<TestPage>());
        Assert.Null(exception);
    }

    private class TestPage : StoryReview
    {
        protected override Task OnInitializedAsync()
        {
            return Task.CompletedTask;
        }
    }
}
