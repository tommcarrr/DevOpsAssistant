using Bunit;
using DevOpsAssistant.Pages;
using DevOpsAssistant.Services;
using DevOpsAssistant.Tests.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace DevOpsAssistant.Tests.Pages;

public class ValidationPageTests : ComponentTestBase
{
    [Fact]
    public void Validation_Renders_With_PopoverProvider()
    {
        SetupServices(includeApi: true);

        var exception = Record.Exception(() => RenderWithProvider<TestPage>());
        Assert.Null(exception);
    }

    [Fact]
    public void Results_Show_State_When_Set()
    {
        SetupServices(includeApi: true);

        var cut = RenderWithProvider<TestPage>();
        var type = typeof(Validation);
        var nested = type.GetNestedType("ResultItem", BindingFlags.NonPublic)!;
        var listType = typeof(List<>).MakeGenericType(nested);
        var list = (IList)Activator.CreateInstance(listType)!;
        var item = Activator.CreateInstance(nested)!;
        nested.GetProperty("Info")!.SetValue(item, new WorkItemInfo
        {
            Id = 1,
            Title = "Story",
            State = "Active",
            WorkItemType = "User Story",
            Url = "http://localhost"
        });
        nested.GetProperty("Violations")!.SetValue(item, new List<string> { "V" });
        list.Add(item);
        var field = type.GetField("_results", BindingFlags.NonPublic | BindingFlags.Instance)!;
        field.SetValue(cut.Instance, list);
        cut.Render();

        Assert.Contains("Active", cut.Markup);
    }

    [Fact]
    public async Task Rules_Are_Collapsed_By_Default()
    {
        var config = SetupServices(includeApi: true);
        await config.SaveAsync(new DevOpsConfig
        {
            Rules = new ValidationRules { StoryHasDescription = true }
        });

        var cut = RenderWithProvider<TestPage>();
        var method = typeof(Validation).GetMethod("ComputeRules", BindingFlags.NonPublic | BindingFlags.Instance)!;
        await cut.InvokeAsync(() => method.Invoke(cut.Instance, null));
        cut.Render();

        var expandedField = typeof(Validation).GetField("_rulesExpanded", BindingFlags.NonPublic | BindingFlags.Instance)!;
        Assert.False((bool)expandedField.GetValue(cut.Instance)!);

        cut.Find("button.rules-toggle").Click();
        cut.WaitForAssertion(() => Assert.True((bool)expandedField.GetValue(cut.Instance)!));
    }

    private class TestPage : Validation
    {
        protected override Task OnInitializedAsync()
        {
            return Task.CompletedTask;
        }
    }

    // Rendering uses ComponentTestBase.RenderWithProvider
}
