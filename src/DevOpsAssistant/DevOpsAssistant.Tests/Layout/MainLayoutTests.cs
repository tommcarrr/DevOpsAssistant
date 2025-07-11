using Bunit;
using DevOpsAssistant.Layout;
using DevOpsAssistant.Services;
using DevOpsAssistant.Tests.Utils;
using System.Reflection;
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
        var link = cut.Find("a[href^='/projects/'][href$='/settings']");

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
        var method = typeof(MainLayout).GetMethod("SignOut", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var task = cut.InvokeAsync(() => (Task)method.Invoke(cut.Instance, null)!);
        var dialog = cut.WaitForElement("div.mud-dialog");
        dialog.GetElementsByTagName("button")[0].Click();
        await task;

        Assert.Equal(string.Empty, config.CurrentProject.Name);
    }

    [Fact]
    public void Footer_Shows_Version()
    {
        SetupServices();

        var cut = RenderComponent<MainLayout>();

        cut.WaitForAssertion(() => Assert.Contains("Version 1.0", cut.Markup));
    }

    [Fact]
    public void Footer_Displays_Disclaimer()
    {
        SetupServices();

        var cut = RenderComponent<MainLayout>();

        cut.WaitForAssertion(() => Assert.Contains("This is a prototype", cut.Markup));
    }

    [Fact]
    public void Footer_Uses_Sticky_Flexbox_Css()
    {
        var cssPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "../../../../DevOpsAssistant/wwwroot/css/app.css"));
        Assert.True(File.Exists(cssPath));
        var css = File.ReadAllText(cssPath);

        Assert.Contains("body {", css);
        Assert.Contains("display: flex", css);
        Assert.Contains("min-height: 100vh", css);
        Assert.Contains(".app-footer {", css);
        Assert.Contains("margin-top: auto", css);
        Assert.Contains(".mud-main-content", css);
        Assert.Contains("flex-direction: column", css);
        Assert.Contains(".mud-layout", css);
        Assert.Contains("flex: 1", css);
    }

    [Fact]
    public async Task Project_Select_Changes_Current_Project()
    {
        var config = SetupServices();
        await config.AddProjectAsync("One");
        await config.AddProjectAsync("Two");
        await config.SelectProjectAsync("One");
        var cut = RenderComponent<MainLayout>();
        var method = typeof(MainLayout).GetMethod("ChangeProject", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var task = cut.InvokeAsync(() => (Task)method.Invoke(cut.Instance, ["Two"])!);
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
        await config.SelectProjectAsync("One");

        var nav = Services.GetRequiredService<NavigationManager>() as FakeNavigationManager;
        nav!.NavigateTo("projects/new");

        var cut = RenderComponent<MainLayout>();
        var method = typeof(MainLayout).GetMethod("ChangeProject", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var task = cut.InvokeAsync(() => (Task)method.Invoke(cut.Instance, ["Two"])!);
        var dialog = cut.WaitForElement("div.mud-dialog");
        dialog.GetElementsByTagName("button")[0].Click();
        await task;

        Assert.Equal("http://localhost/projects/Two/settings", nav.Uri);
    }

    [Fact]
    public async Task Current_Project_Name_Displayed_In_Project_Menu()
    {
        var config = SetupServices();
        await config.AddProjectAsync("Demo");
        await config.SelectProjectAsync("Demo");

        var cut = RenderComponent<MainLayout>();

        var button = cut.Find("div.mud-menu button");
        Assert.Contains("Demo", button.TextContent);
    }

    [Fact]
    public async Task Project_MenuItem_Uses_Project_Color()
    {
        var config = SetupServices();
        await config.AddProjectAsync("Demo");
        config.Projects[0].Color = "#123456";
        await config.SelectProjectAsync("Demo");

        var cut = RenderComponent<MainLayout>();

        var button = cut.Find("div.mud-menu button");
        button.Click();
        var item = cut.Find("div.mud-popover-provider .mud-menu-item");
        Assert.Contains("#123456", item.GetAttribute("style"));
    }
}
