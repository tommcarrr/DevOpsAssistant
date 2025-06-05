using Bunit;
using DevOpsAssistant.Pages;
using DevOpsAssistant.Services;
using MudBlazor.Services;
using MudBlazor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Xunit;

namespace DevOpsAssistant.Tests;

public class WorkItemsPageTests : TestContext
{
    private class TestWorkItems : WorkItems
    {
        protected override Task OnInitializedAsync() => Task.CompletedTask;
    }

    private class Wrapper : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<MudPopoverProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<TestWorkItems>(1);
            builder.CloseComponent();
        }
    }

    [Fact]
    public void WorkItems_Renders_With_PopoverProvider()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(new DevOpsConfigService(new FakeLocalStorageService()));
        Services.AddSingleton<DevOpsApiService>(sp => new DevOpsApiService(new HttpClient(), sp.GetRequiredService<DevOpsConfigService>()));

        var exception = Record.Exception(() => RenderComponent<Wrapper>());
        Assert.Null(exception);
    }
}
