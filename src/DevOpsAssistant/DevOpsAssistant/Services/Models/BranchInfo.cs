namespace DevOpsAssistant.Services;

public class BranchInfo
{
    public string Name { get; set; } = string.Empty;
    public DateTime CommitDate { get; set; }
    public int Ahead { get; set; }
    public int Behind { get; set; }
}
