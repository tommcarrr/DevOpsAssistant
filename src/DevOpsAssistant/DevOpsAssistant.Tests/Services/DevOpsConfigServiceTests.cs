using DevOpsAssistant.Services;
using DevOpsAssistant.Services.Models;
using DevOpsAssistant.Tests.Utils;

namespace DevOpsAssistant.Tests.Services;

public class DevOpsConfigServiceTests
{
    [Fact]
    public async Task SaveAsync_Persists_Config_And_Updates_Property()
    {
        var storage = new FakeLocalStorageService();
        var service = new DevOpsConfigService(storage);
        await service.AddProjectAsync("default");
        var config = new DevOpsConfig
        {
            Organization = " Org ",
            Project = " Proj ",
            PatToken = " Token ",
            DarkMode = true,
            ReleaseNotesTreeView = true,
            DefinitionOfReady = "DOR",
            StoryQualityPrompt = "SQ",
            ReleaseNotesPrompt = "RN",
            RequirementsPrompt = "RP",
            DefaultStates = "Resolved",
            MainBranch = " main ",
            Rules = new ValidationRules
            {
                Epic = new EpicRules { HasDescription = true },
                Bug = new BugRules
                {
                    IncludeReproSteps = false,
                    IncludeSystemInfo = false,
                    HasStoryPoints = false
                }
            }
        };

        await service.SaveAsync(config);

        Assert.Equal("Org", service.Config.Organization);
        var stored = await storage.GetItemAsync<List<DevOpsProject>>("devops-projects");
        Assert.NotNull(stored);
        var p = Assert.Single(stored!);
        Assert.Equal("default", p.Name);
        var storedCfg = p.Config;
        Assert.Equal("Org", storedCfg.Organization);
        Assert.Equal("Proj", storedCfg.Project);
        Assert.Equal("Token", storedCfg.PatToken);
        Assert.True(storedCfg.DarkMode);
        Assert.True(storedCfg.ReleaseNotesTreeView);
        Assert.False(storedCfg.Rules.Bug.IncludeReproSteps);
        Assert.False(storedCfg.Rules.Bug.IncludeSystemInfo);
        Assert.False(storedCfg.Rules.Bug.HasStoryPoints);
        Assert.Equal("DOR", storedCfg.DefinitionOfReady);
        Assert.Equal("SQ", storedCfg.StoryQualityPrompt);
        Assert.Equal("RN", storedCfg.ReleaseNotesPrompt);
        Assert.Equal("RP", storedCfg.RequirementsPrompt);
        Assert.Equal("Resolved", storedCfg.DefaultStates);
        Assert.Equal("main", storedCfg.MainBranch);
        Assert.True(storedCfg.Rules.Epic.HasDescription);
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
            ReleaseNotesTreeView = true,
            DefinitionOfReady = "DOR",
            StoryQualityPrompt = "SQ",
            ReleaseNotesPrompt = "RN",
            RequirementsPrompt = "RP",
            DefaultStates = "Active",
            Rules = new ValidationRules
            {
                Epic = new EpicRules { HasDescription = true },
                Bug = new BugRules
                {
                    IncludeReproSteps = false,
                    IncludeSystemInfo = false,
                    HasStoryPoints = false
                }
            }
        };
        var project = new DevOpsProject { Name = "proj", Config = stored };
        await storage.SetItemAsync("devops-projects", new List<DevOpsProject> { project });
        var service = new DevOpsConfigService(storage);

        await service.LoadAsync();

        Assert.Equal("Org", service.Config.Organization);
        Assert.Equal("Proj", service.Config.Project);
        Assert.Equal("Token", service.Config.PatToken);
        Assert.True(service.Config.DarkMode);
        Assert.True(service.Config.ReleaseNotesTreeView);
        Assert.False(service.Config.Rules.Bug.IncludeReproSteps);
        Assert.False(service.Config.Rules.Bug.IncludeSystemInfo);
        Assert.False(service.Config.Rules.Bug.HasStoryPoints);
        Assert.False(service.Config.Rules.Bug.HasStoryPoints);
        Assert.Equal("DOR", service.Config.DefinitionOfReady);
        Assert.Equal("SQ", service.Config.StoryQualityPrompt);
        Assert.Equal("RN", service.Config.ReleaseNotesPrompt);
        Assert.Equal("RP", service.Config.RequirementsPrompt);
        Assert.Equal("Active", service.Config.DefaultStates);
        Assert.True(service.Config.Rules.Epic.HasDescription);
        Assert.Equal("proj", service.CurrentProject.Name);
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
            StoryQualityPrompt = " SQ ",
            ReleaseNotesPrompt = " RN ",
            RequirementsPrompt = " RP ",
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
        Assert.Equal("SQ", service.Config.StoryQualityPrompt);
        Assert.Equal("RN", service.Config.ReleaseNotesPrompt);
        Assert.Equal("RP", service.Config.RequirementsPrompt);
        Assert.Equal("Active", service.Config.DefaultStates);
        Assert.Equal("default", service.CurrentProject.Name);
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
        Assert.False(service.Config.ReleaseNotesTreeView);
        Assert.True(service.Config.Rules.Bug.IncludeReproSteps);
        Assert.True(service.Config.Rules.Bug.IncludeSystemInfo);
        Assert.True(service.Config.Rules.Bug.HasStoryPoints);
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
        Assert.Equal(string.Empty, service.Config.StoryQualityPrompt);
        Assert.Equal(string.Empty, service.Config.ReleaseNotesPrompt);
        Assert.Equal(string.Empty, service.Config.RequirementsPrompt);
        Assert.False(service.Config.ReleaseNotesTreeView);
        Assert.True(service.Config.Rules.Bug.IncludeReproSteps);
        Assert.True(service.Config.Rules.Bug.IncludeSystemInfo);
        Assert.True(service.Config.Rules.Bug.HasStoryPoints);
        Assert.False(await storage.ContainKeyAsync("devops-projects"));
    }

    [Fact]
    public async Task RemoveProjectAsync_Removes_Project_And_Updates_Current()
    {
        var storage = new FakeLocalStorageService();
        var service = new DevOpsConfigService(storage);
        await service.AddProjectAsync("proj1");
        await service.AddProjectAsync("proj2");

        await service.RemoveProjectAsync("proj2");

        Assert.DoesNotContain(service.Projects, p => p.Name == "proj2");
        Assert.Equal("proj1", service.CurrentProject.Name);
    }

    [Fact]
    public async Task UpdateProjectAsync_Updates_Project_Without_Changing_Current()
    {
        var storage = new FakeLocalStorageService();
        var service = new DevOpsConfigService(storage);
        await service.AddProjectAsync("one");
        await service.AddProjectAsync("two");
        await service.SelectProjectAsync("one");

        var cfg = new DevOpsConfig { Organization = "Org" };
        await service.UpdateProjectAsync("two", "two", cfg);

        Assert.Equal("one", service.CurrentProject.Name);
        var other = service.Projects.First(p => p.Name == "two");
        Assert.Equal("Org", other.Config.Organization);
    }

    [Fact]
    public async Task AddProjectAsync_Returns_False_For_Duplicate()
    {
        var storage = new FakeLocalStorageService();
        var service = new DevOpsConfigService(storage);

        await service.AddProjectAsync("proj");
        var result = await service.AddProjectAsync("proj");

        Assert.False(result);
        Assert.Single(service.Projects);
    }

    [Fact]
    public async Task SaveCurrentAsync_Returns_False_When_NewName_Exists()
    {
        var storage = new FakeLocalStorageService();
        var service = new DevOpsConfigService(storage);
        await service.AddProjectAsync("one");
        await service.AddProjectAsync("two");
        await service.SelectProjectAsync("one");

        var result = await service.SaveCurrentAsync("two", new DevOpsConfig());

        Assert.False(result);
        Assert.Equal("one", service.CurrentProject.Name);
    }

    [Fact]
    public async Task UpdateProjectAsync_Returns_False_When_NewName_Exists()
    {
        var storage = new FakeLocalStorageService();
        var service = new DevOpsConfigService(storage);
        await service.AddProjectAsync("one");
        await service.AddProjectAsync("two");

        var result = await service.UpdateProjectAsync("two", "one", new DevOpsConfig());

        Assert.False(result);
    }

    [Fact]
    public async Task SaveGlobalDarkModeAsync_Persists_Value()
    {
        var storage = new FakeLocalStorageService();
        var service = new DevOpsConfigService(storage);

        await service.SaveGlobalDarkModeAsync(true);

        Assert.True(service.GlobalDarkMode);
        var stored = await storage.GetItemAsync<bool?>("devops-dark");
        Assert.True(stored);
    }

    [Fact]
    public async Task SaveGlobalOrganizationAsync_Persists_Value()
    {
        var storage = new FakeLocalStorageService();
        var service = new DevOpsConfigService(storage);

        await service.SaveGlobalOrganizationAsync("Org");

        Assert.Equal("Org", service.GlobalOrganization);
        var stored = await storage.GetItemAsync<string>("devops-org");
        Assert.Equal("Org", stored);
    }

    [Fact]
    public async Task SaveGlobalHighContrastAsync_Persists_Value()
    {
        var storage = new FakeLocalStorageService();
        var service = new DevOpsConfigService(storage);

        await service.SaveGlobalHighContrastAsync(true);

        Assert.True(service.GlobalHighContrast);
        var stored = await storage.GetItemAsync<bool?>("devops-contrast");
        Assert.True(stored);
    }

    [Fact]
    public async Task SaveCurrentAsync_Raises_Event_When_Validity_Changes()
    {
        var storage = new FakeLocalStorageService();
        var service = new DevOpsConfigService(storage);
        await service.AddProjectAsync("one");
        bool raised = false;
        service.ProjectChanged += () => raised = true;

        await service.SaveCurrentAsync("one", new DevOpsConfig
        {
            Organization = "Org",
            Project = "Proj",
            PatToken = "token"
        });

        Assert.True(raised);
    }

    [Fact]
    public async Task SaveCurrentAsync_Does_Not_Raise_Event_When_Validity_Unchanged()
    {
        var storage = new FakeLocalStorageService();
        var service = new DevOpsConfigService(storage);
        await service.AddProjectAsync("one");
        await service.SaveCurrentAsync("one", new DevOpsConfig { Organization = "Org", Project = "Proj", PatToken = "token" });
        bool raised = false;
        service.ProjectChanged += () => raised = true;

        await service.SaveCurrentAsync("one", new DevOpsConfig
        {
            Organization = "Org2",
            Project = "Proj2",
            PatToken = "token2"
        });

        Assert.False(raised);
    }
}
