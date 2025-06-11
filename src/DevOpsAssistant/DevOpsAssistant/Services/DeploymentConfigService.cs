using System.Text.Json;

namespace DevOpsAssistant.Services;

public class DeploymentConfig
{
    public string? DevOpsApiBaseUrl { get; set; }
    public string? DevOpsSearchApiBaseUrl { get; set; }
    public string? StaticApiPath { get; set; }
}

public class DeploymentConfigService
{
    private readonly HttpClient _httpClient;

    public DeploymentConfig Config { get; private set; } = new();

    public DeploymentConfigService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task LoadAsync()
    {
        try
        {
            var json = await _httpClient.GetStringAsync("staging-config.json");
            Config = JsonSerializer.Deserialize<DeploymentConfig>(json) ?? new();
        }
        catch
        {
            Config = new();
        }
    }
}
