namespace DevOpsAssistant.Services.Models;

public class WikiSearchResult
{
    public string WikiId { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string Name => System.IO.Path.GetFileName(Path);
}
