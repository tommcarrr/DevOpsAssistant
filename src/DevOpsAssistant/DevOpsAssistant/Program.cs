using Blazored.LocalStorage;
using DevOpsAssistant;
using DevOpsAssistant.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Microsoft.JSInterop;
using System.Globalization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp =>
{
    return new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
});
builder.Services.AddMudServices();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<DevOpsConfigService>();
builder.Services.AddScoped<PageStateService>();
builder.Services.AddScoped<DevOpsApiService>();
builder.Services.AddScoped<VersionService>();
builder.Services.AddScoped<DeploymentConfigService>();
builder.Services.AddLocalization();

var host = builder.Build();
var js = host.Services.GetRequiredService<IJSRuntime>();
var cultureName = await js.InvokeAsync<string>("blazorCulture.get");
if (!string.IsNullOrWhiteSpace(cultureName))
{
    var culture = new CultureInfo(cultureName);
    CultureInfo.DefaultThreadCurrentCulture = culture;
    CultureInfo.DefaultThreadCurrentUICulture = culture;
}
await host.Services.GetRequiredService<DeploymentConfigService>().LoadAsync();
await host.RunAsync();
