using Bunit;
using DevOpsAssistant.Components;
using DevOpsAssistant.Tests.Utils;

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
}
