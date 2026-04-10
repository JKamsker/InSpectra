namespace InSpectra.Gen.Acquisition.Analysis.Auto.Selection;

using InSpectra.Gen.Acquisition.Analysis.Auto.Results;

using InSpectra.Gen.Acquisition.Analysis.Tools;
using InSpectra.Gen.Acquisition.Analysis.Output;
using InSpectra.Gen.Acquisition.OpenCli.Documents;

using InSpectra.Discovery.Tool.Analysis;

using System.Text.Json.Nodes;

internal static class AutoSelectedAnalyzerSupport
{
    public static async Task<JsonObject> RunAsync(
        Func<CancellationToken, Task> runAnalyzerAsync,
        string packageId,
        string version,
        ToolDescriptor descriptor,
        string batchId,
        int attempt,
        string source,
        string resultPath,
        JsonObject? nativeResult,
        string selectedMode,
        string? selectedFramework,
        CancellationToken cancellationToken)
    {
        await runAnalyzerAsync(cancellationToken);

        var selectedResult = AutoResultSupport.LoadResult(resultPath)
            ?? AutoResultSupport.CreateFailureResult(
                packageId,
                version,
                batchId,
                attempt,
                source,
                "The selected analyzer did not write result.json.");
        AutoResultSupport.ApplyDescriptor(selectedResult, descriptor, selectedMode, nativeResult, selectedFramework);
        ValidateSuccessfulOpenCliArtifact(selectedResult, resultPath);
        return selectedResult;
    }

    private static void ValidateSuccessfulOpenCliArtifact(JsonObject selectedResult, string resultPath)
    {
        if (!string.Equals(selectedResult[ResultKey.Disposition]?.GetValue<string>(), AnalysisDisposition.Success, StringComparison.Ordinal))
        {
            return;
        }

        var openCliArtifact = selectedResult["artifacts"]?["opencliArtifact"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(openCliArtifact))
        {
            return;
        }

        var outputDirectory = Path.GetDirectoryName(resultPath);
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            return;
        }

        var openCliPath = Path.Combine(outputDirectory, openCliArtifact);
        if (OpenCliDocumentValidator.TryLoadValidDocument(openCliPath, out _, out var reason))
        {
            return;
        }

        var failureMessage = $"Selected analyzer produced an invalid OpenCLI artifact. {reason}";
        selectedResult[ResultKey.Disposition] = AnalysisDisposition.RetryableFailure;
        selectedResult["retryEligible"] = true;
        selectedResult["phase"] = "opencli-validation";
        selectedResult[ResultKey.Classification] = "invalid-success-artifact";
        selectedResult[ResultKey.FailureMessage] = failureMessage;
        selectedResult["failureSignature"] = ResultSupport.GetFailureSignature(
            "opencli-validation",
            "invalid-success-artifact",
            failureMessage);
        selectedResult["opencliSource"] = null;

        if (selectedResult["artifacts"] is JsonObject artifacts)
        {
            artifacts["opencliArtifact"] = null;
        }

        if (selectedResult["steps"]?["opencli"] is JsonObject openCliStep)
        {
            openCliStep["status"] = "invalid";
            openCliStep[ResultKey.Classification] = "invalid-success-artifact";
            openCliStep["message"] = reason;
            openCliStep.Remove("artifactSource");
        }

        TryDeleteInvalidArtifact(openCliPath);
    }

    private static void TryDeleteInvalidArtifact(string openCliPath)
    {
        try
        {
            if (File.Exists(openCliPath))
            {
                File.Delete(openCliPath);
            }
        }
        catch
        {
            // Best-effort cleanup only. Result classification already records the invalid artifact.
        }
    }
}

