using Microsoft.AspNetCore.Components;
using DevOpsAssistant.Services;

namespace DevOpsAssistant.Utils;

public abstract class ProjectComponentBase : ComponentBase, IDisposable
{
    [Inject]
    protected DevOpsConfigService ConfigService { get; set; } = default!;

    protected override void OnInitialized()
    {
        ConfigService.ProjectChanged += HandleProjectChanged;
    }

    protected virtual Task OnProjectChangedAsync() => Task.CompletedTask;

    private void HandleProjectChanged()
    {
        _ = InvokeAsync(async () =>
        {
            await OnProjectChangedAsync();
            StateHasChanged();
        });
    }

    public void Dispose()
    {
        ConfigService.ProjectChanged -= HandleProjectChanged;
    }
}
