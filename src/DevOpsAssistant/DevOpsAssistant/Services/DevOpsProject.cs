namespace DevOpsAssistant.Services;

public class DevOpsProject
{
    public string Name { get; set; } = string.Empty;
    public DevOpsConfig Config { get; set; } = new();
    public string Color { get; set; } = string.Empty;
}
