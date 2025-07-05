using System.Reflection;
using System.Collections.Immutable;
using PromptGenerator;

namespace DevOpsAssistant.Tests.Generators;

public class PromptGeneratorTests
{
    private static ImmutableArray<(string Name, string Content)> InvokeParseFile(string text)
    {
        var method = typeof(PromptGenerator.PromptGenerator).GetMethod("ParseFile", BindingFlags.NonPublic | BindingFlags.Static)!
            ?? throw new InvalidOperationException("Method not found");
        return (ImmutableArray<(string, string)>)method.Invoke(null, ["Test.txt", text])!;
    }

    [Fact]
    public void ParseFile_Ignores_Comments()
    {
        var text = """
==== Section1 ====
Line1
// a comment
Line2
/*
Multiline
comment
*/
Line3
==== Section2 ====
Line4
""";

        var result = InvokeParseFile(text);

        Assert.Equal(2, result.Length);
        Assert.Equal("Section1", result[0].Name);
        Assert.Equal($"Line1{System.Environment.NewLine}Line2{System.Environment.NewLine}Line3{System.Environment.NewLine}", result[0].Content);
        Assert.Equal("Section2", result[1].Name);
        Assert.Equal($"Line4{System.Environment.NewLine}", result[1].Content);
    }
}
