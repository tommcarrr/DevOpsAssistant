using System.Net;
using DevOpsAssistant.Services;

namespace DevOpsAssistant.Tests.Services;

public class VersionServiceTests
{
    [Fact]
    public async Task LoadAsync_Sets_Version_From_File()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("1.2.3")
            });
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var service = new VersionService(client);

        await service.LoadAsync();

        Assert.Equal("1.2.3", service.Version);
    }

    [Fact]
    public async Task LoadAsync_Ignores_Errors()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.NotFound));
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var service = new VersionService(client);

        await service.LoadAsync();

        Assert.Equal("dev", service.Version);
    }
}
