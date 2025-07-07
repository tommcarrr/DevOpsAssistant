using System.Reflection;
using Bunit;
using DevOpsAssistant.Components;
using DevOpsAssistant.Tests.Utils;
using MudBlazor;

namespace DevOpsAssistant.Tests.Components;

public class GlobalOptionsDialogTests : ComponentTestBase
{
    private class FakeDialog : IMudDialogInstance
    {
        public Guid Id => Guid.Empty;
        public string ElementId => "1";
        public DialogOptions Options => new();
        public string Title { get; set; } = string.Empty;
        public Task SetOptionsAsync(DialogOptions options) => Task.CompletedTask;
        public Task SetTitleAsync(string? title) { Title = title ?? string.Empty; return Task.CompletedTask; }
        public void Close() { }
        public void Close(DialogResult result) { }
        public void Close<T>(T returnValue) { }
        public void Cancel() { }
        public void CancelAll() { }
        public void StateHasChanged() { }
    }

    [Fact]
    public async Task Save_Updates_GlobalCulture()
    {
        var config = SetupServices();
        var fake = new FakeDialog();
        var cut = RenderComponent<GlobalOptionsDialog>(p => p.AddCascadingValue(fake));

        var field = cut.Instance.GetType().GetField("_culture", BindingFlags.NonPublic | BindingFlags.Instance)!;
        field.SetValue(cut.Instance, "es");
        var method = cut.Instance.GetType().GetMethod("Save", BindingFlags.NonPublic | BindingFlags.Instance)!;

        await cut.InvokeAsync(() => (Task)method.Invoke(cut.Instance, null)!);

        Assert.Equal("es", config.GlobalCulture);
    }
}
