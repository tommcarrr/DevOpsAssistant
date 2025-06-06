using Blazored.LocalStorage;
using DevOpsAssistant;
using DevOpsAssistant.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddMudServices();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<IDevOpsConfigService, DevOpsConfigService>();
builder.Services.AddScoped<IDevOpsApiService, DevOpsApiService>();

await builder.Build().RunAsync();
