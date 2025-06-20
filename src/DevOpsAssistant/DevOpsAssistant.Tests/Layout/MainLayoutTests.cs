using Bunit;
using DevOpsAssistant.Layout;
using DevOpsAssistant.Services;
using DevOpsAssistant.Tests.Utils;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components;
using Bunit.TestDoubles;

namespace DevOpsAssistant.Tests.Layout;

public class MainLayoutTests : ComponentTestBase
{
    [Fact]
    public void Layout_Has_PopoverProvider()
    {
        SetupServices();
        var cut = RenderComponent<MainLayout>();

        // Verify that the MudPopoverProvider is present in the rendered markup
        cut.Markup.Contains("mud-popover-provider");
    }

    [Fact]
    public async Task Layout_Uses_Global_DarkMode()
    {
        var config = SetupServices();
        await config.SaveGlobalDarkModeAsync(true);

        var cut = RenderComponent<MainLayout>();

        Assert.Contains("--mud-native-html-color-scheme: dark", cut.Markup);
    }

    [Fact]
    public void Settings_Link_Is_Always_Enabled()
    {
        SetupServices();

        var cut = RenderComponent<MainLayout>();
        var link = cut.Find($"a[href='/projects/default/settings']");

        Assert.DoesNotContain("mud-nav-link-disabled", link.ClassName);

        var config = cut.Services.GetRequiredService<DevOpsConfigService>();
        Assert.NotNull(config);
    }

    [Fact]
    public async Task SignOut_Clears_Config()
    {
        var config = SetupServices();
        await config.SaveAsync(new DevOpsConfig { Organization = "Org", Project = "Proj", PatToken = "token" });

        var cut = RenderComponent<MainLayout>();
        cut.Find("button[title='Sign Out']").Click();
        var dialog = cut.WaitForElement("div.mud-dialog");
        dialog.GetElementsByTagName("button")[0].Click();

        Assert.Equal("default", config.CurrentProject.Name);
    }

    [Fact]
    public void Footer_Shows_Version()
    {
        SetupServices();

        var cut = RenderComponent<MainLayout>();

        cut.WaitForAssertion(() => Assert.Contains("Version 1.0", cut.Markup));
    }

    [Fact]
    public async Task Project_Select_Changes_Current_Project()
    {
        var config = SetupServices();
        await config.AddProjectAsync("One");
        await config.AddProjectAsync("Two");
        await config.SelectProjectAsync("One");
        var cut = RenderComponent<MainLayout>();
        var method = typeof(MainLayout).GetMethod("ChangeProject", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var task = cut.InvokeAsync(() => (Task)method.Invoke(cut.Instance, new object[] { "Two" })!);
        var dialog = cut.WaitForElement("div.mud-dialog");
        dialog.GetElementsByTagName("button")[0].Click();
        await task;

        Assert.Equal("Two", config.CurrentProject.Name);
    }

    [Fact]
    public async Task ChangeProject_Replaces_Project_Segment_In_Url()
    {
        var config = SetupServices();
        await config.AddProjectAsync("One");
        await config.AddProjectAsync("Two");

        var nav = Services.GetRequiredService<NavigationManager>() as FakeNavigationManager;
        nav!.NavigateTo("projects/new");

        var cut = RenderComponent<MainLayout>();
        var method = typeof(MainLayout).GetMethod("ChangeProject", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var task = cut.InvokeAsync(() => (Task)method.Invoke(cut.Instance, new object[] { "Two" })!);
        var dialog = cut.WaitForElement("div.mud-dialog");
        dialog.GetElementsByTagName("button")[0].Click();
        await task;

        Assert.Equal("http://localhost/projects/Two/settings", nav.Uri);
    }
}
