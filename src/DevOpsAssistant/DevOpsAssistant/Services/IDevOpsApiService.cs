namespace DevOpsAssistant.Services;

public interface IDevOpsApiService
{
    Task<List<WorkItemNode>> GetWorkItemHierarchyAsync(string areaPath);
    Task<string[]> GetBacklogsAsync();
    Task<List<WorkItemDetails>> GetValidationItemsAsync(string areaPath);
    Task UpdateWorkItemStateAsync(int id, string state);
    Task<List<WorkItemInfo>> SearchUserStoriesAsync(string term);
    Task<List<StoryHierarchyDetails>> GetStoryHierarchyDetailsAsync(IEnumerable<int> storyIds);
    Task<List<StoryMetric>> GetStoryMetricsAsync(string areaPath, DateTime? startDate = null);
}
