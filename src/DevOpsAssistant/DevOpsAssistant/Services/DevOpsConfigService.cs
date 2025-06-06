using Blazored.LocalStorage;

namespace DevOpsAssistant.Services;

public class DevOpsConfigService : IDevOpsConfigService
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
        if (config != null) Config = config;
    }

    public async Task SaveAsync(DevOpsConfig config)
    {
        Config = config;
        await _localStorage.SetItemAsync(StorageKey, config);
    }
}