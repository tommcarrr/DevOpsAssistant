using System.Net;
using System.Reflection;
using DevOpsAssistant.Services;
using DevOpsAssistant.Tests.Utils;
using Xunit;

namespace DevOpsAssistant.Tests.Services;

public class DeploymentConfigServiceTests
{
    [Fact]
    public async Task LoadAsync_Sets_Config_From_File()
    {
        var json = "{\"DevOpsApiBaseUrl\":\"http://api\"}";
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            });
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var service = new DeploymentConfigService(client);

        await service.LoadAsync();

        Assert.Equal("http://api", service.Config.DevOpsApiBaseUrl);
    }

    [Fact]
    public async Task LoadAsync_Resets_Config_On_Error()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.NotFound));
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var service = new DeploymentConfigService(client);

        typeof(DeploymentConfigService)
            .GetProperty("Config")!
            .SetValue(service, new DeploymentConfig { DevOpsApiBaseUrl = "old" });

        await service.LoadAsync();

        Assert.Null(service.Config.DevOpsApiBaseUrl);
    }
}
