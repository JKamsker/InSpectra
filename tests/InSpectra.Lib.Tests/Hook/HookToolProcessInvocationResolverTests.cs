namespace InSpectra.Lib.Tests.Hook;

using InSpectra.Lib.Modes.Hook.Execution;

public sealed class HookToolProcessInvocationResolverTests
{
    [Fact]
    public void BuildHelpFallbackInvocations_Uses_The_Full_Help_Variant_Set_For_Direct_Commands()
    {
        var invocation = new HookToolProcessInvocation("demo", ["--help"], PreferredAssemblyPath: null);

        var fallbacks = HookToolProcessInvocationResolver.BuildHelpFallbackInvocations(invocation)
            .Select(fallback => fallback.ArgumentList.ToArray())
            .ToArray();

        Assert.Contains(fallbacks, arguments => arguments.SequenceEqual(["-h"]));
        Assert.Contains(fallbacks, arguments => arguments.SequenceEqual(["-?"]));
        Assert.Contains(fallbacks, arguments => arguments.SequenceEqual(["--h"]));
        Assert.Contains(fallbacks, arguments => arguments.SequenceEqual(["/help"]));
        Assert.Contains(fallbacks, arguments => arguments.SequenceEqual(["/?"]));
        Assert.Contains(fallbacks, arguments => arguments.SequenceEqual(["help"]));
        Assert.DoesNotContain(fallbacks, arguments => arguments.SequenceEqual(["--help"]));
        Assert.DoesNotContain(fallbacks, arguments => arguments.Length == 0);
    }

    [Fact]
    public void BuildHelpFallbackInvocations_Keeps_Dotnet_Entry_Point_As_A_Prefix()
    {
        var invocation = new HookToolProcessInvocation(
            "dotnet",
            [@"C:\tools\demo.dll", "config", "--help"],
            PreferredAssemblyPath: @"C:\tools\demo.dll");

        var fallbacks = HookToolProcessInvocationResolver.BuildHelpFallbackInvocations(invocation)
            .Select(fallback => fallback.ArgumentList.ToArray())
            .ToArray();

        Assert.Contains(fallbacks, arguments => arguments.SequenceEqual([@"C:\tools\demo.dll", "config", "/help"]));
        Assert.Contains(fallbacks, arguments => arguments.SequenceEqual([@"C:\tools\demo.dll", "help", "config"]));
        Assert.Contains(fallbacks, arguments => arguments.SequenceEqual([@"C:\tools\demo.dll", "config", "help"]));
        Assert.DoesNotContain(fallbacks, arguments => arguments.SequenceEqual([@"C:\tools\demo.dll", "config"]));
        Assert.DoesNotContain(fallbacks, arguments => arguments.SequenceEqual(["help", @"C:\tools\demo.dll", "config"]));
    }
}
