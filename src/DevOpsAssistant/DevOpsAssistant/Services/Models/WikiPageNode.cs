namespace DevOpsAssistant.Services;

public class WikiPageNode
{
    public string Path { get; set; } = string.Empty;
    public List<WikiPageNode> Children { get; set; } = new();
    public string Name => string.IsNullOrEmpty(Path) || Path == "/" ? "Home" : System.IO.Path.GetFileName(Path);
}
