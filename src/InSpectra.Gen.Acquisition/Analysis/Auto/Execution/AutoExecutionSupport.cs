namespace InSpectra.Gen.Acquisition.Analysis.Auto.Execution;

using InSpectra.Gen.Acquisition.Analysis.Auto.Selection;

using InSpectra.Gen.Acquisition.Analysis.Tools;

using InSpectra.Gen.Acquisition.Analysis.Auto.Runners;


using System.Text.Json.Nodes;

internal static class AutoExecutionSupport
{
    public static Task<NativeAnalysisOutcome> TryRunNativeAnalysisAsync(
        IAutoNativeRunner nativeRunner,
        string packageId,
        string version,
        ToolDescriptor descriptor,
        string outputDirectory,
        string batchId,
        int attempt,
        string source,
        int installTimeoutSeconds,
        int commandTimeoutSeconds,
        string resultPath,
        bool json,
        bool suppressOutput,
        CancellationToken cancellationToken)
        => AutoNativeExecutionSupport.TryRunAsync(
            nativeRunner,
            packageId,
            version,
            descriptor,
            outputDirectory,
            batchId,
            attempt,
            source,
            installTimeoutSeconds,
            commandTimeoutSeconds,
            resultPath,
            json,
            suppressOutput,
            cancellationToken);

    public static Task<JsonObject> RunSelectedAnalyzerAsync(
        AutoAnalysisAttempt selectedAttempt,
        IAutoHelpRunner helpRunner,
        IAutoCliFxRunner cliFxRunner,
        IAutoStaticRunner staticRunner,
        IAutoHookRunner hookRunner,
        string packageId,
        string version,
        ToolDescriptor descriptor,
        string outputDirectory,
        string batchId,
        int attempt,
        string source,
        int installTimeoutSeconds,
        int analysisTimeoutSeconds,
        int commandTimeoutSeconds,
        string resultPath,
        JsonObject? nativeResult,
        CancellationToken cancellationToken)
    {
        Func<CancellationToken, Task> runAnalyzer = selectedAttempt.Mode switch
        {
            "hook" => async token => await hookRunner.RunAsync(
                packageId, version, descriptor.CommandName,
                selectedAttempt.Framework ?? descriptor.CliFramework,
                outputDirectory, batchId, attempt, source,
                installTimeoutSeconds, analysisTimeoutSeconds, commandTimeoutSeconds, token),

            "clifx" => async token => await cliFxRunner.RunAsync(
                packageId, version, descriptor.CommandName,
                selectedAttempt.Framework ?? descriptor.CliFramework,
                outputDirectory, batchId, attempt, source,
                installTimeoutSeconds, analysisTimeoutSeconds, commandTimeoutSeconds, token),

            "static" => async token => await staticRunner.RunAsync(
                packageId, version, descriptor.CommandName,
                selectedAttempt.Framework ?? descriptor.CliFramework,
                outputDirectory, batchId, attempt, source,
                installTimeoutSeconds, analysisTimeoutSeconds, commandTimeoutSeconds, token),

            _ => async token => await helpRunner.RunAsync(
                packageId, version, descriptor.CommandName,
                outputDirectory, batchId, attempt, source,
                descriptor.CliFramework,
                installTimeoutSeconds, analysisTimeoutSeconds, commandTimeoutSeconds, token),
        };

        return AutoSelectedAnalyzerSupport.RunAsync(
            runAnalyzer,
            packageId,
            version,
            descriptor,
            batchId,
            attempt,
            source,
            resultPath,
            nativeResult,
            selectedMode: selectedAttempt.Mode,
            selectedFramework: selectedAttempt.Framework,
            cancellationToken);
    }
}

internal sealed record NativeAnalysisOutcome(bool ShouldReturnImmediately, int ExitCode, JsonObject? Result)
{
    public static NativeAnalysisOutcome Continue(JsonObject? result)
        => new(false, 0, result);

    public static NativeAnalysisOutcome Return(int exitCode)
        => new(true, exitCode, null);
}

