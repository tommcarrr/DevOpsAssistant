using DevOpsAssistant.Services;

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
            Organization = " Org ",
            Project = " Proj ",
            PatToken = " Token ",
            DarkMode = true,
            DefinitionOfReady = " DOR ",
            DefaultStates = " Resolved ",
            MainBranch = " main ",
            Rules = new ValidationRules { EpicHasDescription = true }
        };

        await service.SaveAsync(config);

        Assert.Equal("Org", service.Config.Organization);
        var stored = await storage.GetItemAsync<DevOpsConfig>("devops-config");
        Assert.NotNull(stored);
        Assert.Equal("Org", stored.Organization);
        Assert.Equal("Proj", stored.Project);
        Assert.Equal("Token", stored.PatToken);
        Assert.True(stored.DarkMode);
        Assert.Equal("DOR", stored.DefinitionOfReady);
        Assert.Equal("Resolved", stored.DefaultStates);
        Assert.Equal("main", stored.MainBranch);
        Assert.True(stored.Rules.EpicHasDescription);
    }

    [Fact]
    public async Task LoadAsync_Loads_Config_When_Present()
    {
        var storage = new FakeLocalStorageService();
        var stored = new DevOpsConfig
        {
            Organization = "Org",
            Project = "Proj",
            PatToken = "Token",
            DarkMode = true,
            DefinitionOfReady = "DOR",
            DefaultStates = "Active",
            Rules = new ValidationRules { EpicHasDescription = true }
        };
        await storage.SetItemAsync("devops-config", stored);
        var service = new DevOpsConfigService(storage);

        await service.LoadAsync();

        Assert.Equal("Org", service.Config.Organization);
        Assert.Equal("Proj", service.Config.Project);
        Assert.Equal("Token", service.Config.PatToken);
        Assert.True(service.Config.DarkMode);
        Assert.Equal("DOR", service.Config.DefinitionOfReady);
        Assert.Equal("Active", service.Config.DefaultStates);
        Assert.True(service.Config.Rules.EpicHasDescription);
    }

    [Fact]
    public async Task LoadAsync_Trims_Whitespace()
    {
        var storage = new FakeLocalStorageService();
        var stored = new DevOpsConfig
        {
            Organization = " Org ",
            Project = " Proj ",
            PatToken = " Token ",
            MainBranch = " main ",
            DefinitionOfReady = " DOR ",
            DefaultStates = " Active ",
            Rules = new ValidationRules()
        };
        await storage.SetItemAsync("devops-config", stored);
        var service = new DevOpsConfigService(storage);

        await service.LoadAsync();

        Assert.Equal("Org", service.Config.Organization);
        Assert.Equal("Proj", service.Config.Project);
        Assert.Equal("Token", service.Config.PatToken);
        Assert.Equal("main", service.Config.MainBranch);
        Assert.Equal("DOR", service.Config.DefinitionOfReady);
        Assert.Equal("Active", service.Config.DefaultStates);
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
        Assert.Equal(string.Empty, service.Config.DefaultStates);
        Assert.NotNull(service.Config.Rules);
    }

    [Fact]
    public async Task ClearAsync_Removes_Config_And_Resets_Property()
    {
        var storage = new FakeLocalStorageService();
        var service = new DevOpsConfigService(storage);
        await service.SaveAsync(new DevOpsConfig { Organization = "Org", Project = "Proj", PatToken = "Token" });

        await service.ClearAsync();

        Assert.Equal(string.Empty, service.Config.Organization);
        Assert.Equal(string.Empty, service.Config.DefaultStates);
        Assert.False(await storage.ContainKeyAsync("devops-config"));
    }
}