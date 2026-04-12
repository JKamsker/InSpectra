namespace InSpectra.Lib.Tests.Live;

using Xunit;

[Collection("LiveToolAnalysis")]
public sealed class AutoAnalysisServiceLiveTests
{
    public static TheoryData<LiveAutoToolCase> Cases()
        => ValidatedGenericHelpFrameworkCases.LoadForAutoLiveTests();

    [Theory]
    [MemberData(nameof(Cases))]
    [Trait("Category", "Live")]
    public async Task RunAsync_Chooses_Expected_Mode_For_Real_World_Tools(LiveAutoToolCase testCase)
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

        Assert.True(report.Success, BuildFailureMessage(testCase, report));
        Assert.True(
            string.Equals(testCase.ExpectedAnalysisMode, report.SelectedMode, StringComparison.Ordinal),
            BuildModeMismatchMessage(testCase, report));
        Assert.Equal(testCase.Framework, report.Descriptor.CliFramework);

        AutoAcquisitionLiveTestSupport.AssertExpectedCommands(report.OpenCliDocument, testCase.ExpectedCommands);
        AutoAcquisitionLiveTestSupport.AssertExpectedOptions(report.OpenCliDocument, testCase.ExpectedOptions);
        AutoAcquisitionLiveTestSupport.AssertExpectedArguments(report.OpenCliDocument, testCase.ExpectedArguments);
    }

    private static string BuildFailureMessage(LiveAutoToolCase testCase, AutoAcquisitionReport report)
    {
        var lines = new List<string>
        {
            $"Auto acquisition failed for {testCase.PackageId} {testCase.Version}.",
            $"Preferred mode: {report.Descriptor.PreferredAnalysisMode} ({report.Descriptor.SelectionReason})",
        };

        foreach (var attempt in report.Attempts)
        {
            lines.Add(
                $"{attempt.Mode} [{attempt.Framework ?? "<none>"}]: {attempt.Outcome}"
                + $"{FormatTail(attempt.Classification)}{FormatTail(attempt.Message)}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string BuildModeMismatchMessage(LiveAutoToolCase testCase, AutoAcquisitionReport report)
    {
        var lines = new List<string>
        {
            $"Auto acquisition selected '{report.SelectedMode ?? "<none>"}' instead of '{testCase.ExpectedAnalysisMode}' for {testCase.PackageId} {testCase.Version}.",
            $"Preferred mode: {report.Descriptor.PreferredAnalysisMode} ({report.Descriptor.SelectionReason})",
        };

        foreach (var attempt in report.Attempts)
        {
            lines.Add(
                $"{attempt.Mode} [{attempt.Framework ?? "<none>"}]: {attempt.Outcome}"
                + $"{FormatTail(attempt.Classification)}{FormatTail(attempt.ArtifactSource)}{FormatTail(attempt.Message)}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string FormatTail(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : $" | {value}";

    public sealed record LiveAutoToolCase(
        string Framework,
        string ExpectedAnalysisMode,
        string PackageId,
        string Version,
        string CommandName,
        IReadOnlyList<string>? expectedCommands = null,
        IReadOnlyList<string>? expectedOptions = null,
        IReadOnlyList<string>? expectedArguments = null)
    {
        public IReadOnlyList<string> ExpectedCommands { get; } = expectedCommands ?? [];
        public IReadOnlyList<string> ExpectedOptions { get; } = expectedOptions ?? [];
        public IReadOnlyList<string> ExpectedArguments { get; } = expectedArguments ?? [];

        public override string ToString()
            => $"{ExpectedAnalysisMode}: {PackageId} {Version}";
    }
}
