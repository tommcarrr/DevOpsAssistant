using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace DevOpsAssistant.SmokeTests;

public class SmokeTests
{

    [Theory]
    [InlineData("/")]
    [InlineData("/metrics")]
    [InlineData("/release-notes")]
    [InlineData("/requirements-planner")]
    [InlineData("/story-review")]
    [InlineData("/validation")]
    [InlineData("/epics-features")]
    public async Task Page_Returns_Success(string path)
    {
        var baseUrl = Environment.GetEnvironmentVariable("STAGING_URL");
        if (string.IsNullOrEmpty(baseUrl))
        {
            return; // Skip when not running in CI
        }

        using var client = new HttpClient();
        var response = await client.GetAsync(baseUrl.TrimEnd('/') + path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/favicon.ico")]
    [InlineData("/site.webmanifest")]
    [InlineData("/version.txt")]
    [InlineData("/index.html")]
    public async Task Asset_Returns_Success(string path)
    {
        var baseUrl = Environment.GetEnvironmentVariable("STAGING_URL");
        if (string.IsNullOrEmpty(baseUrl))
        {
            return; // Skip when not running in CI
        }

        using var client = new HttpClient();
        var response = await client.GetAsync(baseUrl.TrimEnd('/') + path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

}
