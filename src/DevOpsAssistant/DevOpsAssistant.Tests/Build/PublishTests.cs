using System.Diagnostics;
using Xunit;

namespace DevOpsAssistant.Tests.Build;

public class PublishTests
{
    [Fact]
    public void Publish_Copies_StaticWebAppConfig_To_Wwwroot()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        var projectPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../DevOpsAssistant/DevOpsAssistant.csproj"));
        var info = new ProcessStartInfo("dotnet",
            $"publish \"{projectPath}\" -c Release -o \"{tempDir}\" --no-restore --nologo")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(projectPath)!
        };

        using var process = Process.Start(info);
        process!.WaitForExit();
        if (process.ExitCode != 0)
        {
            var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
            throw new Xunit.Sdk.XunitException($"dotnet publish failed: {output}");
        }

        var configPath = Path.Combine(tempDir, "wwwroot", "staticwebapp.config.json");
        Assert.True(File.Exists(configPath), $"Expected file '{configPath}' to exist.");
    }
}
