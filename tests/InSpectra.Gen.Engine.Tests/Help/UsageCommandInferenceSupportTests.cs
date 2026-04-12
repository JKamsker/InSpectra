namespace InSpectra.Gen.Engine.Tests.Help;

using InSpectra.Gen.Engine.Modes.Help.Inference.Usage.Commands;

public sealed class UsageCommandInferenceSupportTests
{
    [Fact]
    public void LooksLikeCommandHub_Returns_True_For_Command_Placeholders()
    {
        var usageLines = new[]
        {
            "demo [command]",
            "demo config [options]",
        };

        Assert.True(UsageCommandInferenceSupport.LooksLikeCommandHub("demo", usageLines));
    }

    [Fact]
    public void InferCommands_Extracts_Commands_And_Prefers_Richer_Descriptions()
    {
        var usageLines = new[]
        {
            "demo serve  Start the server.",
            "demo serve  Server mode.",
            "demo sync   Synchronize data.",
        };

        var commands = UsageCommandInferenceSupport.InferCommands(usageLines)
            .OrderBy(item => item.Key, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(2, commands.Length);
        Assert.Equal("serve", commands[0].Key);
        Assert.Equal("Start the server.", commands[0].Description);
        Assert.Equal("sync", commands[1].Key);
        Assert.Equal("Synchronize data.", commands[1].Description);
    }

    [Fact]
    public void InferChildCommands_Extracts_Immediate_Children_For_A_Command_Path()
    {
        var usageLines = new[]
        {
            "demo config set  Set a value.",
            "demo config get  Get a value.",
            "demo other list  List other items.",
        };

        var children = UsageCommandInferenceSupport.InferChildCommands("demo", ["config"], usageLines)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["config get", "config set"], children);
    }

    [Fact]
    public void InferChildCommands_Does_Not_Treat_Aligned_Descriptions_As_Children()
    {
        var usageLines = new[]
        {
            "git remote  Manage remotes.",
        };

        var children = UsageCommandInferenceSupport.InferChildCommands("git", ["remote"], usageLines);

        Assert.Empty(children);
    }

    [Fact]
    public void LooksLikeCommandHub_Does_Not_Fall_Back_To_Unmatched_Root_Inference()
    {
        var usageLines = new[]
        {
            "other serve  Start the server.",
            "other sync   Synchronize data.",
        };

        Assert.False(UsageCommandInferenceSupport.LooksLikeCommandHub("demo", usageLines));
    }
}
