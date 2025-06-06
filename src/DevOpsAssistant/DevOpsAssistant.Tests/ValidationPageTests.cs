using Bunit;
using DevOpsAssistant.Pages;
using DevOpsAssistant.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;

namespace DevOpsAssistant.Tests;

public class ValidationPageTests : TestContext
{
    [Fact]
    public void Validation_Renders_With_PopoverProvider()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(new DevOpsConfigService(new FakeLocalStorageService()));
        Services.AddSingleton<DevOpsApiService>(sp =>
            new DevOpsApiService(new HttpClient(), sp.GetRequiredService<DevOpsConfigService>()));

        var exception = Record.Exception(() => RenderComponent<Wrapper>());
        Assert.Null(exception);
    }

    private class TestPage : Validation
    {
        protected override Task OnInitializedAsync()
        {
            return Task.CompletedTask;
        }
    }

    private class Wrapper : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<MudPopoverProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<TestPage>(1);
            builder.CloseComponent();
        }
    }
}