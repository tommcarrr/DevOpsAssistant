namespace DevOpsAssistant.Services.Models;

public class WorkItemInfo : IEquatable<WorkItemInfo>
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string WorkItemType { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;

    public bool Equals(WorkItemInfo? other)
    {
        return other != null && other.Id == Id;
    }

    public override bool Equals(object? obj)
    {
        return obj is WorkItemInfo wi && Equals(wi);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}