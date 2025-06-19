using Blazored.LocalStorage;

namespace DevOpsAssistant.Services;

public class PageStateService
{
    private readonly ILocalStorageService _localStorage;
    private readonly DevOpsConfigService _configService;

    public PageStateService(ILocalStorageService localStorage, DevOpsConfigService configService)
    {
        _localStorage = localStorage;
        _configService = configService;
    }

    public async Task SaveAsync<T>(string key, T state)
    {
        await _localStorage.SetItemAsync(GetKey(key), state);
    }

    public async Task<T?> LoadAsync<T>(string key)
    {
        return await _localStorage.GetItemAsync<T>(GetKey(key));
    }

    public async Task ClearAsync(string key)
    {
        await _localStorage.RemoveItemAsync(GetKey(key));
    }

    private string GetKey(string key) => $"{_configService.CurrentProject.Name}-{key}";
}
