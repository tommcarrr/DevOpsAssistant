using Blazored.LocalStorage;

namespace DevOpsAssistant.Services;

public class DevOpsConfigService
{
    private const string LegacyStorageKey = "devops-config";
    private const string StorageKey = "devops-projects";
    private const string GlobalPatKey = "devops-pat";
    private const string GlobalDarkKey = "devops-dark";
    private const string GlobalOrgKey = "devops-org";
    private readonly ILocalStorageService _localStorage;

    public event Action? ProjectChanged;

    public DevOpsConfigService(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;

        CurrentProject = new DevOpsProject();
    }

    public List<DevOpsProject> Projects { get; private set; } = new();
    public DevOpsProject CurrentProject { get; private set; } = new();

    public string GlobalPatToken { get; private set; } = string.Empty;
    public string GlobalOrganization { get; private set; } = string.Empty;
    public bool GlobalDarkMode { get; private set; }

    public DevOpsConfig Config => CurrentProject.Config;

    public bool IsCurrentProjectValid =>
        !string.IsNullOrWhiteSpace(CurrentProject.Name) &&
        (!string.IsNullOrWhiteSpace(Config.Organization) ||
         !string.IsNullOrWhiteSpace(GlobalOrganization)) &&
        !string.IsNullOrWhiteSpace(Config.Project) &&
        (!string.IsNullOrWhiteSpace(Config.PatToken) ||
         !string.IsNullOrWhiteSpace(GlobalPatToken));

    public async Task LoadAsync()
    {
        GlobalPatToken = await _localStorage.GetItemAsync<string>(GlobalPatKey) ?? string.Empty;
        GlobalOrganization = await _localStorage.GetItemAsync<string>(GlobalOrgKey) ?? string.Empty;
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

    public async Task<bool> SaveCurrentAsync(string name, DevOpsConfig config)
    {
        var wasValid = IsCurrentProjectValid;
        name = name.Trim();
        if (!CurrentProject.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
            Projects.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            return false;

        CurrentProject.Name = name;
        CurrentProject.Config = Normalize(config);
        await SaveProjectsAsync();
        if (wasValid != IsCurrentProjectValid)
            OnProjectChanged();
        return true;
    }

    public async Task SaveGlobalPatAsync(string token)
    {
        GlobalPatToken = token.Trim();
        await _localStorage.SetItemAsync(GlobalPatKey, GlobalPatToken);
    }

    public async Task SaveGlobalOrganizationAsync(string organization)
    {
        GlobalOrganization = organization.Trim();
        await _localStorage.SetItemAsync(GlobalOrgKey, GlobalOrganization);
    }

    public async Task SaveGlobalDarkModeAsync(bool value)
    {
        GlobalDarkMode = value;
        await _localStorage.SetItemAsync(GlobalDarkKey, GlobalDarkMode);
    }

    public async Task<bool> UpdateProjectAsync(string existingName, string newName, DevOpsConfig config)
    {
        var proj = Projects.FirstOrDefault(p => p.Name == existingName);
        if (proj == null) return false;
        newName = newName.Trim();
        if (!existingName.Equals(newName, StringComparison.OrdinalIgnoreCase) &&
            Projects.Any(p => p.Name.Equals(newName, StringComparison.OrdinalIgnoreCase)))
            return false;
        proj.Name = newName;
        proj.Config = Normalize(config);
        await SaveProjectsAsync();
        return true;
    }

    public async Task<bool> AddProjectAsync(string name, DevOpsProject? source = null)
    {
        name = name.Trim();
        if (Projects.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            return false;
        var project = new DevOpsProject
        {
            Name = name,
            Config = source != null ? Clone(source.Config) : new DevOpsConfig()
        };
        Projects.Add(Normalize(project));
        CurrentProject = project;
        await SaveProjectsAsync();
        OnProjectChanged();
        return true;
    }

    public async Task RemoveProjectAsync(string name)
    {
        var proj = Projects.FirstOrDefault(p => p.Name == name);
        if (proj == null) return;
        Projects.Remove(proj);
        if (Projects.Count == 0)
            CurrentProject = new DevOpsProject();
        else if (CurrentProject.Name == name)
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
        if (string.IsNullOrWhiteSpace(cfg.Organization))
            cfg.Organization = GlobalOrganization;
        if (string.IsNullOrWhiteSpace(cfg.PatToken))
            cfg.PatToken = GlobalPatToken;
        return cfg;
    }

    public async Task ClearAsync()
    {
        Projects = new List<DevOpsProject>();
        CurrentProject = new DevOpsProject();
        GlobalPatToken = string.Empty;
        GlobalOrganization = string.Empty;
        GlobalDarkMode = false;
        await _localStorage.RemoveItemAsync(StorageKey);
        await _localStorage.RemoveItemAsync(LegacyStorageKey);
        await _localStorage.RemoveItemAsync(GlobalPatKey);
        await _localStorage.RemoveItemAsync(GlobalOrgKey);
        await _localStorage.RemoveItemAsync(GlobalDarkKey);
        OnProjectChanged();
    }

    public async Task RemoveGlobalPatAsync()
    {
        GlobalPatToken = string.Empty;
        await _localStorage.RemoveItemAsync(GlobalPatKey);
    }

    public async Task RemoveGlobalOrganizationAsync()
    {
        GlobalOrganization = string.Empty;
        await _localStorage.RemoveItemAsync(GlobalOrgKey);
    }

    private async Task SaveProjectsAsync()
    {
        await _localStorage.SetItemAsync(StorageKey, Projects);
    }
}