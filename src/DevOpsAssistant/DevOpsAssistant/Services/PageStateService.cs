using Blazored.LocalStorage;

namespace DevOpsAssistant.Services;

public class PageStateService
{
    private readonly ILocalStorageService _localStorage;

    public PageStateService(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public async Task SaveAsync<T>(string key, T state)
    {
        await _localStorage.SetItemAsync(key, state);
    }

    public async Task<T?> LoadAsync<T>(string key)
    {
        return await _localStorage.GetItemAsync<T>(key);
    }

    public async Task ClearAsync(string key)
    {
        await _localStorage.RemoveItemAsync(key);
    }
}
