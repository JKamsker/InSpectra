namespace InSpectra.Gen.Acquisition.Analysis.Auto.Execution;

using InSpectra.Gen.Acquisition.Infrastructure.Paths;

using InSpectra.Gen.Acquisition.Analysis.Auto.Results;

using InSpectra.Gen.Acquisition.Analysis.Tools;

using InSpectra.Gen.Acquisition.Analysis.Auto.Runners;

using InSpectra.Gen.Acquisition.Analysis;

using System.Text.Json.Nodes;

internal static class AutoNativeExecutionSupport
{
    public static async Task<NativeAnalysisOutcome> TryRunAsync(
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
    {
        if (!string.Equals(descriptor.PreferredAnalysisMode, AnalysisMode.Native, StringComparison.OrdinalIgnoreCase))
        {
            return NativeAnalysisOutcome.Continue(null);
        }

        await nativeRunner.RunAsync(
            packageId,
            version,
            outputDirectory,
            batchId,
            attempt,
            source,
            installTimeoutSeconds,
            commandTimeoutSeconds,
            cancellationToken);

        var nativeResult = AutoResultSupport.LoadResult(resultPath);
        if (nativeResult is null)
        {
            return NativeAnalysisOutcome.Continue(null);
        }

        AutoResultSupport.ApplyDescriptor(nativeResult, descriptor, AnalysisMode.Native, null, descriptor.CliFramework);
        RepositoryPathResolver.WriteJsonFile(resultPath, nativeResult);
        if (!AutoResultInspector.ShouldTryHelpFallback(nativeResult))
        {
            return NativeAnalysisOutcome.Return(
                await AutoResultSupport.WriteResultAsync(
                    packageId,
                    version,
                    resultPath,
                    nativeResult,
                    json,
                    suppressOutput,
                    cancellationToken));
        }

        return NativeAnalysisOutcome.Continue(nativeResult);
    }
}

