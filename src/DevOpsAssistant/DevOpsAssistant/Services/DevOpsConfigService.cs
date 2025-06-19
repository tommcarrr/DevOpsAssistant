using Blazored.LocalStorage;

namespace DevOpsAssistant.Services;

public class DevOpsConfigService
{
    private const string LegacyStorageKey = "devops-config";
    private const string StorageKey = "devops-projects";
    private readonly ILocalStorageService _localStorage;

    public DevOpsConfigService(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;

        var project = new DevOpsProject { Name = "default" };
        Projects.Add(project);
        CurrentProject = project;
    }

    public List<DevOpsProject> Projects { get; private set; } = new();
    public DevOpsProject CurrentProject { get; private set; } = new();

    public DevOpsConfig Config => CurrentProject.Config;

    public async Task LoadAsync()
    {
        var projects = await _localStorage.GetItemAsync<List<DevOpsProject>>(StorageKey);
        if (projects != null && projects.Count > 0)
        {
            Projects = projects.Select(Normalize).ToList();
            CurrentProject = Projects[0];
            return;
        }

        var legacy = await _localStorage.GetItemAsync<DevOpsConfig>(LegacyStorageKey);
        if (legacy != null)
        {
            var project = new DevOpsProject { Name = "default", Config = Normalize(legacy) };
            Projects = new List<DevOpsProject> { project };
            CurrentProject = project;
            await SaveProjectsAsync();
            await _localStorage.RemoveItemAsync(LegacyStorageKey);
        }
    }

    public async Task SaveAsync(DevOpsConfig config)
    {
        var normalized = Normalize(config);

        CurrentProject.Config = normalized;
        await SaveProjectsAsync();
    }

    public async Task SaveCurrentAsync(string name, DevOpsConfig config)
    {
        CurrentProject.Name = name.Trim();
        CurrentProject.Config = Normalize(config);
        await SaveProjectsAsync();
    }

    public async Task AddProjectAsync(string name, DevOpsProject? source = null)
    {
        var project = new DevOpsProject
        {
            Name = name.Trim(),
            Config = source != null ? Clone(source.Config) : new DevOpsConfig()
        };
        Projects.Add(Normalize(project));
        CurrentProject = project;
        await SaveProjectsAsync();
    }

    public void SelectProject(string name)
    {
        var proj = Projects.FirstOrDefault(p => p.Name == name);
        if (proj != null) CurrentProject = proj;
    }

    private static DevOpsConfig Normalize(DevOpsConfig config)
    {
        return new DevOpsConfig
        {
            Organization = config.Organization.Trim(),
            Project = config.Project.Trim(),
            PatToken = config.PatToken.Trim(),
            MainBranch = config.MainBranch.Trim(),
            DarkMode = config.DarkMode,
            ReleaseNotesTreeView = config.ReleaseNotesTreeView,
            DefaultStates = config.DefaultStates.Trim(),
            DefinitionOfReady = config.DefinitionOfReady.Trim(),
            StoryQualityPrompt = config.StoryQualityPrompt.Trim(),
            ReleaseNotesPrompt = config.ReleaseNotesPrompt.Trim(),
            RequirementsPrompt = config.RequirementsPrompt.Trim(),
            PromptCharacterLimit = config.PromptCharacterLimit,
            Rules = config.Rules
        };
    }

    private static DevOpsProject Normalize(DevOpsProject project)
    {
        project.Name = project.Name.Trim();
        project.Config = Normalize(project.Config);
        return project;
    }

    private static DevOpsConfig Clone(DevOpsConfig cfg)
    {
        return new DevOpsConfig
        {
            Organization = cfg.Organization,
            Project = cfg.Project,
            PatToken = cfg.PatToken,
            MainBranch = cfg.MainBranch,
            DarkMode = cfg.DarkMode,
            ReleaseNotesTreeView = cfg.ReleaseNotesTreeView,
            DefaultStates = cfg.DefaultStates,
            DefinitionOfReady = cfg.DefinitionOfReady,
            StoryQualityPrompt = cfg.StoryQualityPrompt,
            ReleaseNotesPrompt = cfg.ReleaseNotesPrompt,
            RequirementsPrompt = cfg.RequirementsPrompt,
            PromptCharacterLimit = cfg.PromptCharacterLimit,
            Rules = cfg.Rules
        };
    }

    public async Task ClearAsync()
    {
        Projects = new List<DevOpsProject> { new DevOpsProject { Name = "default" } };
        CurrentProject = Projects[0];
        await _localStorage.RemoveItemAsync(StorageKey);
        await _localStorage.RemoveItemAsync(LegacyStorageKey);
    }

    private async Task SaveProjectsAsync()
    {
        await _localStorage.SetItemAsync(StorageKey, Projects);
    }
}