using Bunit;
using DevOpsAssistant.Pages;
using DevOpsAssistant.Tests.Utils;
using DevOpsAssistant.Services;
using Microsoft.AspNetCore.Components;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;

namespace DevOpsAssistant.Tests.Pages;

public class HomePageTests : ComponentTestBase
{
    [Fact]
    public async Task Home_Shows_Description_When_Project_Selected()
    {
        var config = SetupServices();
        await config.AddProjectAsync("Demo");
        await config.SaveCurrentAsync("Demo", new DevOpsConfig { Organization = "Org", Project = "Proj", PatToken = "token" }, "");
        var cut = RenderComponent<Home>(p => p.Add(c => c.ProjectName, config.CurrentProject.Name));

        Assert.Contains("DevOpsAssistant provides", cut.Markup);
    }

    [Fact]
    public async Task Home_Redirects_To_Current_Project_When_No_Name_In_Url()
    {
        var config = SetupServices();
        await config.AddProjectAsync("Demo");
        await config.SaveCurrentAsync("Demo", new DevOpsConfig { Organization = "Org", Project = "Proj", PatToken = "token" }, "");

        var nav = Services.GetRequiredService<NavigationManager>() as FakeNavigationManager;
        RenderComponent<Home>();

        Assert.Equal($"http://localhost/projects/{config.CurrentProject.Name}", nav!.Uri);
    }

    [Fact]
    public void Home_Navigates_To_New_When_No_Projects()
    {
        SetupServices();
        var nav = Services.GetRequiredService<NavigationManager>() as FakeNavigationManager;

        RenderComponent<Home>();

        Assert.Equal("http://localhost/projects/new", nav!.Uri);
    }
}
