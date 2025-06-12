using System;
using System.Linq;

namespace DevOpsAssistant.Services;

public static class WorkItemHelpers
{
    public static void ComputeStatus(WorkItemNode node)
    {
        foreach (var child in node.Children)
            ComputeStatus(child);

        if (!node.Children.Any())
        {
            node.ExpectedState = node.Info.State;
            node.StatusValid = true;
            return;
        }

        var allClosed = node.Children.All(c =>
            c.ExpectedState.Equals("Closed", StringComparison.OrdinalIgnoreCase) ||
            c.ExpectedState.Equals("Removed", StringComparison.OrdinalIgnoreCase) ||
            c.ExpectedState.Equals("Done", StringComparison.OrdinalIgnoreCase));

        var anyNotNew = node.Children.Any(c => !c.ExpectedState.Equals("New", StringComparison.OrdinalIgnoreCase));

        var expected = allClosed ? "Closed" : anyNotNew ? "Active" : "New";

        node.ExpectedState = expected;
        node.StatusValid = node.Info.State.Equals(expected, StringComparison.OrdinalIgnoreCase);
    }

    public static string GetItemClass(string type)
    {
        return type.ToLowerInvariant() switch
        {
            "epic" => "work-item-epic",
            "feature" => "work-item-feature",
            "user story" => "work-item-story",
            "task" => "work-item-task",
            "bug" => "work-item-bug",
            _ => string.Empty
        };
    }
}
