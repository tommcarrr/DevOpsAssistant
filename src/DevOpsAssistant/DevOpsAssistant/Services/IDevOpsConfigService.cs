namespace DevOpsAssistant.Services;

public interface IDevOpsConfigService
{
    DevOpsConfig Config { get; }
    Task LoadAsync();
    Task SaveAsync(DevOpsConfig config);
}
