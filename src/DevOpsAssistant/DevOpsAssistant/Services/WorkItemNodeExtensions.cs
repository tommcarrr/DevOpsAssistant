using System;
using System.Linq;

namespace DevOpsAssistant.Services;

public static class WorkItemNodeExtensions
{
    public static bool Contains(this WorkItemNode parent, int id)
    {
        if (parent.Info.Id == id)
            return true;
        return parent.Children.Any(child => child.Contains(id));
    }

    public static void ComputeStatus(this WorkItemNode node)
    {
        foreach (var child in node.Children)
            child.ComputeStatus();

        if (!node.Children.Any())
        {
            node.ExpectedState = node.Info.State;
            node.StatusValid = true;
            return;
        }

        var allClosed = node.Children.All(c =>
            c.Info.State.Equals("Closed", StringComparison.OrdinalIgnoreCase) ||
            c.Info.State.Equals("Removed", StringComparison.OrdinalIgnoreCase) ||
            c.Info.State.Equals("Done", StringComparison.OrdinalIgnoreCase));

        var anyNotNew = node.Children.Any(c => !c.Info.State.Equals("New", StringComparison.OrdinalIgnoreCase));

        var expected = allClosed ? "Closed" : anyNotNew ? "Active" : "New";

        node.ExpectedState = expected;
        node.StatusValid = node.Info.State.Equals(expected, StringComparison.OrdinalIgnoreCase);
    }
}
