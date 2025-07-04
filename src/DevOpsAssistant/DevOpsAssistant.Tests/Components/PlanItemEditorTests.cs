using Bunit;
using DevOpsAssistant.Components;
using DevOpsAssistant.Tests.Utils;
using DevOpsAssistant.Utils;
using System.Collections.Generic;
using System.Reflection;

namespace DevOpsAssistant.Tests.Components;

public class PlanItemEditorTests : ComponentTestBase
{
    [Fact]
    public void Renders_ChildContent()
    {
        SetupServices();
        var cut = RenderComponent<PlanItemEditor>(p => p
            .Add(c => c.Type, "Epic")
            .AddChildContent("<div class=\"inner\">content</div>")
        );

        cut.Find("div.inner").MarkupMatches("<div class=\"inner\">content</div>");
    }

    [Fact]
    public void Shows_DragHandle_When_Draggable()
    {
        SetupServices();
        var cut = RenderComponent<PlanItemEditor>(p => p
            .Add(c => c.Type, "Epic")
            .Add(c => c.Draggable, true)
        );

        Assert.NotNull(cut.Find(".drag-handle"));
    }

    [Fact]
    public void RemoveTag_Does_Not_Remove_AiGeneratedTag()
    {
        SetupServices();
        var cut = RenderComponent<PlanItemEditor>(p => p
            .Add(c => c.Type, "User Story")
            .Add(c => c.Tags, new List<string> { AppConstants.AiGeneratedTag, "other" })
        );

        var begin = typeof(PlanItemEditor).GetMethod("BeginEditTags", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var remove = typeof(PlanItemEditor).GetMethod("RemoveTag", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var field = typeof(PlanItemEditor).GetField("_tagsEdit", BindingFlags.NonPublic | BindingFlags.Instance)!;
        cut.InvokeAsync(() => begin.Invoke(cut.Instance, null));
        cut.InvokeAsync(() => remove.Invoke(cut.Instance, [AppConstants.AiGeneratedTag]));
        var tags = (List<string>)field.GetValue(cut.Instance)!;
        Assert.Contains(AppConstants.AiGeneratedTag, tags);
    }
}
