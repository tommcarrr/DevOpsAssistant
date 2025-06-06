namespace DevOpsAssistant.Services;

public class WorkItemNode
{
    public WorkItemInfo Info { get; set; } = new();
    public List<WorkItemNode> Children { get; } = [];
    public string ExpectedState { get; set; } = string.Empty;
    public bool StatusValid { get; set; }
}