using Blazored.LocalStorage;

namespace DevOpsAssistant.Services;

public class DevOpsConfigService
{
    private const string StorageKey = "devops-config";
    private readonly ILocalStorageService _localStorage;

    public DevOpsConfigService(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public DevOpsConfig Config { get; private set; } = new();

    public async Task LoadAsync()
    {
        var config = await _localStorage.GetItemAsync<DevOpsConfig>(StorageKey);
        if (config != null) Config = Normalize(config);
    }

    public async Task SaveAsync(DevOpsConfig config)
    {
        var normalized = Normalize(config);

        Config = normalized;
        await _localStorage.SetItemAsync(StorageKey, normalized);
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

    public async Task ClearAsync()
    {
        Config = new DevOpsConfig();
        await _localStorage.RemoveItemAsync(StorageKey);
    }
}