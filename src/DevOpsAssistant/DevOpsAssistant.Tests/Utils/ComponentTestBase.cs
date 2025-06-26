using Bunit;
using DevOpsAssistant.Services;
using System.Net;
using System.Net.Http;
using DevOpsAssistant.Tests;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using MudBlazor;
using MudBlazor.Services;

namespace DevOpsAssistant.Tests.Utils;

public abstract class ComponentTestBase : TestContext
{
    protected DevOpsConfigService SetupServices(bool includeApi = false)
    {
        Services.AddMudServices();
        Services.AddLocalization();
        JSInterop.Mode = JSRuntimeMode.Loose;
        var storage = new FakeLocalStorageService();
        var config = new DevOpsConfigService(storage);
        Services.AddSingleton(config);
        Services.AddSingleton(new PageStateService(storage, config));
        Services.AddSingleton(new ThemeSessionService(JSInterop.JSRuntime));
        if (includeApi)
        {
            var deployment = new DeploymentConfigService(new HttpClient());
            Services.AddSingleton(deployment);
            Services.AddSingleton(sp => new DevOpsApiService(
                new HttpClient(),
                config,
                deployment,
                sp.GetRequiredService<IStringLocalizer<DevOpsApiService>>()));
        }
        var client = new HttpClient(new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("1.0")
            }))
        { BaseAddress = new Uri("http://localhost") };
        var versionSvc = new VersionService(client);
        versionSvc.LoadAsync().GetAwaiter().GetResult();
        Services.AddSingleton(versionSvc);
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
