using Bunit;
using DevOpsAssistant.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;

namespace DevOpsAssistant.Tests.Utils;

public abstract class ComponentTestBase : TestContext
{
    protected DevOpsConfigService SetupServices(bool includeApi = false)
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        var config = new DevOpsConfigService(new FakeLocalStorageService());
        Services.AddSingleton<IDevOpsConfigService>(config);
        if (includeApi)
            Services.AddSingleton<IDevOpsApiService>(sp => new DevOpsApiService(new HttpClient(), config));
        return config;
    }

    protected IRenderedComponent<T> RenderWithProvider<T>() where T : IComponent
    {
        var wrapper = RenderComponent<PopoverWrapper<T>>();
        return wrapper.FindComponent<T>();
    }

    private class PopoverWrapper<T> : ComponentBase where T : IComponent
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<MudPopoverProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<T>(1);
            builder.CloseComponent();
        }
    }
}
