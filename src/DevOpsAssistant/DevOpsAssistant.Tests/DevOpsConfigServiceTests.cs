using System.Threading.Tasks;
using DevOpsAssistant.Services;
using Xunit;

namespace DevOpsAssistant.Tests;

public class DevOpsConfigServiceTests
{
    [Fact]
    public async Task SaveAsync_Persists_Config_And_Updates_Property()
    {
        var storage = new FakeLocalStorageService();
        var service = new DevOpsConfigService(storage);
        var config = new DevOpsConfig
        {
            Organization = "Org",
            Project = "Proj",
            PatToken = "Token",
            DarkMode = true
        };

        await service.SaveAsync(config);

        Assert.Equal("Org", service.Config.Organization);
        var stored = await storage.GetItemAsync<DevOpsConfig>("devops-config");
        Assert.Equal("Org", stored.Organization);
        Assert.Equal("Proj", stored.Project);
        Assert.Equal("Token", stored.PatToken);
        Assert.True(stored.DarkMode);
    }

    [Fact]
    public async Task LoadAsync_Loads_Config_When_Present()
    {
        var storage = new FakeLocalStorageService();
        var stored = new DevOpsConfig { Organization = "Org", Project = "Proj", PatToken = "Token", DarkMode = true };
        await storage.SetItemAsync("devops-config", stored);
        var service = new DevOpsConfigService(storage);

        await service.LoadAsync();

        Assert.Equal("Org", service.Config.Organization);
        Assert.Equal("Proj", service.Config.Project);
        Assert.Equal("Token", service.Config.PatToken);
        Assert.True(service.Config.DarkMode);
    }

    [Fact]
    public async Task LoadAsync_Keeps_Default_When_No_Config()
    {
        var storage = new FakeLocalStorageService();
        var service = new DevOpsConfigService(storage);

        await service.LoadAsync();

        Assert.Equal(string.Empty, service.Config.Organization);
        Assert.Equal(string.Empty, service.Config.Project);
        Assert.Equal(string.Empty, service.Config.PatToken);
        Assert.False(service.Config.DarkMode);
    }
}
