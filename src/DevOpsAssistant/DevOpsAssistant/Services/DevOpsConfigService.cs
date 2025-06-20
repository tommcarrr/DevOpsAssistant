using Blazored.LocalStorage;

namespace DevOpsAssistant.Services;

public class DevOpsConfigService
{
    private const string LegacyStorageKey = "devops-config";
    private const string StorageKey = "devops-projects";
    private const string GlobalPatKey = "devops-pat";
    private const string GlobalDarkKey = "devops-dark";
    private readonly ILocalStorageService _localStorage;

    public event Action? ProjectChanged;

    public DevOpsConfigService(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;

        var project = new DevOpsProject { Name = "default" };
        Projects.Add(project);
        CurrentProject = project;
    }

    public List<DevOpsProject> Projects { get; private set; } = new();
    public DevOpsProject CurrentProject { get; private set; } = new();

    public string GlobalPatToken { get; private set; } = string.Empty;
    public bool GlobalDarkMode { get; private set; }

    public DevOpsConfig Config => CurrentProject.Config;

    public bool IsCurrentProjectValid =>
        !string.IsNullOrWhiteSpace(CurrentProject.Name) &&
        !string.IsNullOrWhiteSpace(Config.Organization) &&
        !string.IsNullOrWhiteSpace(Config.Project) &&
        (!string.IsNullOrWhiteSpace(Config.PatToken) ||
         !string.IsNullOrWhiteSpace(GlobalPatToken));

    public async Task LoadAsync()
    {
        GlobalPatToken = await _localStorage.GetItemAsync<string>(GlobalPatKey) ?? string.Empty;
        GlobalDarkMode = await _localStorage.GetItemAsync<bool?>(GlobalDarkKey) ?? false;
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

    public async Task SaveGlobalPatAsync(string token)
    {
        GlobalPatToken = token.Trim();
        await _localStorage.SetItemAsync(GlobalPatKey, GlobalPatToken);
    }

    public async Task SaveGlobalDarkModeAsync(bool value)
    {
        GlobalDarkMode = value;
        await _localStorage.SetItemAsync(GlobalDarkKey, GlobalDarkMode);
    }

    public async Task UpdateProjectAsync(string existingName, string newName, DevOpsConfig config)
    {
        var proj = Projects.FirstOrDefault(p => p.Name == existingName);
        if (proj == null) return;
        proj.Name = newName.Trim();
        proj.Config = Normalize(config);
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
        OnProjectChanged();
    }

    public async Task RemoveProjectAsync(string name)
    {
        var proj = Projects.FirstOrDefault(p => p.Name == name);
        if (proj == null) return;
        Projects.Remove(proj);
        if (Projects.Count == 0)
            Projects.Add(new DevOpsProject { Name = "default" });
        if (CurrentProject.Name == name)
            CurrentProject = Projects[0];
        await SaveProjectsAsync();
        OnProjectChanged();
    }

    public async Task SelectProjectAsync(string name)
    {
        var proj = Projects.FirstOrDefault(p => p.Name == name);
        if (proj == null) return;
        Projects.Remove(proj);
        Projects.Insert(0, proj);
        CurrentProject = proj;
        await SaveProjectsAsync();
        OnProjectChanged();
    }

    private void OnProjectChanged()
    {
        ProjectChanged?.Invoke();
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

    public DevOpsConfig GetEffectiveConfig()
    {
        var cfg = Clone(Config);
        if (string.IsNullOrWhiteSpace(cfg.PatToken))
            cfg.PatToken = GlobalPatToken;
        return cfg;
    }

    public async Task ClearAsync()
    {
        Projects = new List<DevOpsProject> { new DevOpsProject { Name = "default" } };
        CurrentProject = Projects[0];
        GlobalPatToken = string.Empty;
        GlobalDarkMode = false;
        await _localStorage.RemoveItemAsync(StorageKey);
        await _localStorage.RemoveItemAsync(LegacyStorageKey);
        await _localStorage.RemoveItemAsync(GlobalPatKey);
        await _localStorage.RemoveItemAsync(GlobalDarkKey);
        OnProjectChanged();
    }

    private async Task SaveProjectsAsync()
    {
        await _localStorage.SetItemAsync(StorageKey, Projects);
    }
}