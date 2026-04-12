namespace InSpectra.Gen.Engine.Tests.Live;

using InSpectra.Gen.Engine.Contracts;

using Xunit;

[Collection("LiveToolAnalysis")]
public sealed class AutoHookFallbackLiveTests
{
    public static TheoryData<HookFallbackToolCase> Cases()
    {
        var data = new TheoryData<HookFallbackToolCase>();
        data.Add(new HookFallbackToolCase(
            "System.CommandLine + Argu",
            "csharp-ls",
            "0.22.0",
            "csharp-ls",
            "csharp-ls",
            "0.22.0",
            "hook-no-assembly-loaded"));
        data.Add(new HookFallbackToolCase(
            "System.CommandLine",
            "DotNetAnalyzer",
            "1.5.0",
            "dotnet-analyzer",
            "DotNetAnalyzer - .NET MCP Server for Claude Code",
            "1.5.0",
            "hook-no-assembly-loaded",
            expectedAnalysisMode: "help",
            expectedArtifactSource: "crawled-from-help"));
        data.Add(new HookFallbackToolCase(
            "System.CommandLine",
            "SoftwareExtravaganza.Whizbang.CLI",
            "0.54.2-alpha.76",
            "whizbang",
            "Whizbang CLI - Command-line tool for Whizbang",
            "0.54.2-alpha.76",
            "hook-no-assembly-loaded",
            expectedAnalysisMode: "help",
            expectedArtifactSource: "crawled-from-help"));
        data.Add(new HookFallbackToolCase(
            "System.CommandLine",
            "SaigonMio.Generata",
            "1.1.36",
            "generata",
            "Mio.Generata",
            "1.1.36",
            "hook-no-patchable-method",
            expectedAnalysisMode: "help",
            expectedArtifactSource: "crawled-from-help"));
        data.Add(new HookFallbackToolCase(
            "McMaster.Extensions.CommandLineUtils",
            "sqlite-global-tool",
            "1.2.2",
            "sqlite-tool",
            "=> Welcome to sqlite .net core global tool version",
            "1.2.2",
            "hook-no-assembly-loaded",
            expectedAnalysisMode: "help",
            expectedArtifactSource: "crawled-from-help",
            requireHookFailure: false));
        return data;
    }

    public static TheoryData<HookTerminalFailureToolCase> TerminalFailureCases()
    {
        var data = new TheoryData<HookTerminalFailureToolCase>();
        data.Add(new HookTerminalFailureToolCase(
            "McMaster.Extensions.CommandLineUtils",
            "Meadow.Cli",
            "0.3.225",
            "meadow",
            "custom-parser-no-attributes",
            expectedSelectedMode: "static",
            expectedFallbackClassification: "invalid-opencli-artifact"));
        return data;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    [Trait("Category", "Live")]
    public async Task RunAsync_Falls_Back_To_Expected_Mode_For_Real_World_Hook_Regressions(HookFallbackToolCase testCase)
    {
        if (!HookLiveTestSupport.ShouldRun())
        {
            return;
        }

        var report = await AutoAcquisitionLiveTestSupport.RunAsync(
            testCase.PackageId,
            testCase.Version,
            testCase.CommandName,
            timeoutSeconds: 300,
            CancellationToken.None);

        Assert.True(report.Success, BuildFailureMessage(testCase.PackageId, testCase.Version, report));
        Assert.Equal("static", report.Descriptor.PreferredAnalysisMode);
        Assert.Equal(testCase.Framework, report.Descriptor.CliFramework);
        Assert.True(
            string.Equals(testCase.ExpectedAnalysisMode, report.SelectedMode, StringComparison.Ordinal),
            BuildModeMismatchMessage(testCase, report));
        Assert.Equal(testCase.ExpectedOpenCliTitle, report.OpenCliDocument?["info"]?["title"]?.GetValue<string>());
        Assert.Equal(testCase.ExpectedOpenCliVersion, report.OpenCliDocument?["info"]?["version"]?.GetValue<string>());
        Assert.Equal(testCase.ExpectedArtifactSource, report.OpenCliDocument?["x-inspectra"]?["artifactSource"]?.GetValue<string>());

        if (testCase.RequireHookFailure)
        {
            AssertContainsFailureClassification(
                report.Attempts,
                mode: AnalysisMode.Hook,
                testCase.ExpectedHookFailureClassifications);
        }

        HookOpenCliSnapshotSupport.AssertMatchesFixture(testCase.PackageId, testCase.Version, report.OpenCliDocument);
    }

    [Theory]
    [MemberData(nameof(TerminalFailureCases))]
    [Trait("Category", "Live")]
    public async Task RunAsync_Reports_Expected_Terminal_Failures_For_Real_World_Tools(HookTerminalFailureToolCase testCase)
    {
        if (!HookLiveTestSupport.ShouldRun())
        {
            return;
        }

        var report = await AutoAcquisitionLiveTestSupport.RunAsync(
            testCase.PackageId,
            testCase.Version,
            testCase.CommandName,
            timeoutSeconds: 300,
            CancellationToken.None);

        Assert.False(report.Success, BuildFailureMessage(testCase.PackageId, testCase.Version, report));
        Assert.Equal("static", report.Descriptor.PreferredAnalysisMode);
        Assert.Equal(testCase.Framework, report.Descriptor.CliFramework);

        var terminalAttempts = report.Attempts.Where(attempt =>
            string.Equals(attempt.Mode, testCase.ExpectedSelectedMode, StringComparison.Ordinal)
            && string.Equals(attempt.Outcome, AnalysisDisposition.Failed, StringComparison.Ordinal)).ToArray();
        Assert.True(
            terminalAttempts.Length == 1,
            BuildTerminalAttemptMessage(testCase, report, terminalAttempts));
        var terminalAttempt = terminalAttempts[0];
        AssertClassificationOrMessageContains(terminalAttempt, testCase.ExpectedClassification);

        if (string.IsNullOrWhiteSpace(testCase.ExpectedFallbackClassification))
        {
            Assert.DoesNotContain(report.Attempts, attempt =>
                string.Equals(attempt.Mode, AnalysisMode.Static, StringComparison.Ordinal)
                && string.Equals(attempt.Outcome, AnalysisDisposition.Failed, StringComparison.Ordinal));
            return;
        }

        var hookAttempts = report.Attempts.Where(attempt =>
            string.Equals(attempt.Mode, AnalysisMode.Hook, StringComparison.Ordinal)
            && string.Equals(attempt.Outcome, AnalysisDisposition.Failed, StringComparison.Ordinal)).ToArray();
        Assert.True(
            hookAttempts.Length == 1,
            BuildHookAttemptMessage(testCase, report, hookAttempts));
        var hookAttempt = hookAttempts[0];
        AssertClassificationOrMessageContains(hookAttempt, testCase.ExpectedFallbackClassification);
    }

    private static void AssertContainsFailureClassification(
        IEnumerable<AutoAcquisitionAttemptReport> attempts,
        string mode,
        IReadOnlyList<string> expectedClassifications)
    {
        var candidates = attempts.Where(attempt =>
            string.Equals(attempt.Mode, mode, StringComparison.Ordinal)
            && string.Equals(attempt.Outcome, AnalysisDisposition.Failed, StringComparison.Ordinal)).ToArray();
        Assert.True(
            candidates.Length > 0,
            $"Expected at least one failed {mode} attempt. Attempts:{Environment.NewLine}{string.Join(Environment.NewLine, attempts.Select(FormatAttempt))}");

        foreach (var candidate in candidates)
        {
            if (expectedClassifications.Any(expected => Matches(candidate, expected)))
            {
                return;
            }
        }

        Assert.Fail(
            $"None of the {mode} failures matched the expected classifications: {string.Join(", ", expectedClassifications)}."
            + Environment.NewLine
            + string.Join(Environment.NewLine, candidates.Select(FormatAttempt)));
    }

    private static void AssertClassificationOrMessageContains(AutoAcquisitionAttemptReport attempt, string expectedClassification)
    {
        Assert.True(
            Matches(attempt, expectedClassification),
            $"Expected '{expectedClassification}' in attempt '{FormatAttempt(attempt)}'.");
    }

    private static bool Matches(AutoAcquisitionAttemptReport attempt, string expectedClassification)
        => string.Equals(attempt.Classification, expectedClassification, StringComparison.Ordinal)
            || (!string.IsNullOrWhiteSpace(attempt.Message)
                && attempt.Message.Contains(expectedClassification, StringComparison.Ordinal));

    private static string BuildModeMismatchMessage(HookFallbackToolCase testCase, AutoAcquisitionReport report)
        => BuildFailureMessage(testCase.PackageId, testCase.Version, report)
            + Environment.NewLine
            + $"Expected selected mode '{testCase.ExpectedAnalysisMode}' but got '{report.SelectedMode ?? "<none>"}'.";

    private static string BuildTerminalAttemptMessage(
        HookTerminalFailureToolCase testCase,
        AutoAcquisitionReport report,
        IReadOnlyList<AutoAcquisitionAttemptReport> terminalAttempts)
        => BuildFailureMessage(testCase.PackageId, testCase.Version, report)
            + Environment.NewLine
            + $"Expected exactly one failed '{testCase.ExpectedSelectedMode}' attempt, but found {terminalAttempts.Count}.";

    private static string BuildHookAttemptMessage(
        HookTerminalFailureToolCase testCase,
        AutoAcquisitionReport report,
        IReadOnlyList<AutoAcquisitionAttemptReport> hookAttempts)
        => BuildFailureMessage(testCase.PackageId, testCase.Version, report)
            + Environment.NewLine
            + $"Expected exactly one failed '{AnalysisMode.Hook}' attempt, but found {hookAttempts.Count}.";

    private static string BuildFailureMessage(string packageId, string version, AutoAcquisitionReport report)
    {
        var lines = new List<string>
        {
            $"Auto acquisition did not match the expected discovery contract for {packageId} {version}.",
            $"Preferred mode: {report.Descriptor.PreferredAnalysisMode} ({report.Descriptor.SelectionReason})",
        };

        lines.AddRange(report.Attempts.Select(FormatAttempt));
        return string.Join(Environment.NewLine, lines);
    }

    private static string FormatAttempt(AutoAcquisitionAttemptReport attempt)
        => $"{attempt.Mode} [{attempt.Framework ?? "<none>"}]: {attempt.Outcome}"
            + FormatTail(attempt.Classification)
            + FormatTail(attempt.ArtifactSource)
            + FormatTail(attempt.Message);

    private static string FormatTail(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : $" | {value}";

    public sealed record HookFallbackToolCase(
        string Framework,
        string PackageId,
        string Version,
        string CommandName,
        string ExpectedOpenCliTitle,
        string ExpectedOpenCliVersion,
        string expectedHookFailureClassification,
        string expectedAnalysisMode = "static",
        string expectedArtifactSource = "static-analysis",
        bool requireHookFailure = true,
        params string[] expectedHookFailureClassifications)
    {
        public IReadOnlyList<string> ExpectedHookFailureClassifications { get; } = expectedHookFailureClassifications.Length == 0
            ? [expectedHookFailureClassification]
            : expectedHookFailureClassifications;

        public string ExpectedAnalysisMode { get; } = expectedAnalysisMode;
        public string ExpectedArtifactSource { get; } = expectedArtifactSource;
        public bool RequireHookFailure { get; } = requireHookFailure;

        public override string ToString()
            => $"{PackageId} {Version}";
    }

    public sealed record HookTerminalFailureToolCase(
        string Framework,
        string PackageId,
        string Version,
        string CommandName,
        string ExpectedClassification,
        string expectedSelectedMode = "hook",
        string? expectedFallbackClassification = null)
    {
        public string ExpectedSelectedMode { get; } = expectedSelectedMode;
        public string? ExpectedFallbackClassification { get; } = expectedFallbackClassification;

        public override string ToString()
            => $"{PackageId} {Version}";
    }
}
