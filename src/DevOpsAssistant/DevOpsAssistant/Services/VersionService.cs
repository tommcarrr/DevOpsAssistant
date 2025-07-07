namespace DevOpsAssistant.Services;

public class VersionService
{
    private readonly HttpClient _httpClient;

    public string Version { get; private set; } = "dev";

    public VersionService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task LoadAsync()
    {
        try
        {
            var text = await _httpClient.GetStringAsync("version.txt");
            Version = text.Trim();
        }
        catch
        {
            // keep default on error
        }
    }
}
