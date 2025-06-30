using Blazored.LocalStorage;
using DevOpsAssistant;
using DevOpsAssistant.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Microsoft.JSInterop;
using Microsoft.Extensions.Localization;
using DevOpsAssistant.Resources;
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
builder.Services.AddScoped<ThemeSessionService>();

var host = builder.Build();
var js = host.Services.GetRequiredService<IJSRuntime>();
string cultureName = "en-GB";
try
{
    var stored = await js.InvokeAsync<string>("blazorCulture.get");
    if (!string.IsNullOrWhiteSpace(stored))
        cultureName = stored;
}
catch
{
    cultureName = "en-GB";
}

CultureInfo culture;
try
{
    culture = new CultureInfo(cultureName);
}
catch (CultureNotFoundException)
{
    culture = new CultureInfo("en-GB");
}
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;
var labelLocalizer = host.Services.GetRequiredService<IStringLocalizer<ErrorUi>>();
await js.InvokeVoidAsync("setErrorDismissLabel", labelLocalizer["DismissError"].Value);
var themeService = host.Services.GetRequiredService<ThemeSessionService>();
await themeService.InitializeAsync();
await host.Services.GetRequiredService<DeploymentConfigService>().LoadAsync();
await host.RunAsync();
