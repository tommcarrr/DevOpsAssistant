using Microsoft.JSInterop;

namespace DevOpsAssistant.Services;

public class ThemeSessionService : IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private DotNetObjectReference<ThemeSessionService>? _objRef;

    public event Action? ThemeChanged;

    public bool IsDoom { get; private set; }

    public ThemeSessionService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task InitializeAsync()
    {
        _objRef = DotNetObjectReference.Create(this);
        await _jsRuntime.InvokeVoidAsync("themeShortcut.init", _objRef);
        await _jsRuntime.InvokeVoidAsync("themeShortcut.setDoom", IsDoom);
    }

    [JSInvokable]
    public async Task ToggleDoom()
    {
        IsDoom = !IsDoom;
        await _jsRuntime.InvokeVoidAsync("themeShortcut.setDoom", IsDoom);
        ThemeChanged?.Invoke();
    }

    public ValueTask DisposeAsync()
    {
        _objRef?.Dispose();
        return ValueTask.CompletedTask;
    }
}
