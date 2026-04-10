namespace InSpectra.Gen.Acquisition.Analysis.Auto.Results;

using InSpectra.Gen.Acquisition.Analysis.Output;

using InSpectra.Gen.Acquisition.Frameworks;

using InSpectra.Gen.Acquisition.Infrastructure.Json;

using InSpectra.Gen.Acquisition.Analysis.Tools;

using InSpectra.Discovery.Tool.Analysis;

using System.Text.Json.Nodes;

internal static class AutoResultSupport
{
    public static JsonObject? LoadResult(string resultPath)
        => JsonNodeFileLoader.TryLoadJsonObject(resultPath);

    public static void ApplyDescriptor(
        JsonObject result,
        ToolDescriptor descriptor,
        string analysisMode,
        JsonObject? fallbackResult,
        string? selectedFramework = null)
    {
        var analysisSelection = new JsonObject
        {
            ["preferredMode"] = descriptor.PreferredAnalysisMode,
            ["selectedMode"] = analysisMode,
            ["selectedFramework"] = selectedFramework,
            ["reason"] = descriptor.SelectionReason,
        };
        var candidateFrameworks = new JsonArray();
        foreach (var framework in CliFrameworkProviderRegistry.ResolveFrameworkNames(descriptor.CliFramework))
        {
            candidateFrameworks.Add(framework);
        }

        analysisSelection["candidateFrameworks"] = candidateFrameworks;
        result[ResultKey.AnalysisMode] = analysisMode;
        result["analysisSelection"] = analysisSelection;

        if (CliFrameworkProviderRegistry.ShouldReplace(result[ResultKey.CliFramework]?.GetValue<string>(), descriptor.CliFramework))
        {
            result[ResultKey.CliFramework] = descriptor.CliFramework;
        }
        else if (result[ResultKey.CliFramework] is null && !string.IsNullOrWhiteSpace(descriptor.CliFramework))
        {
            result[ResultKey.CliFramework] = descriptor.CliFramework;
        }

        if (result["command"] is null && !string.IsNullOrWhiteSpace(descriptor.CommandName))
        {
            result["command"] = descriptor.CommandName;
        }

        if (result["packageUrl"] is null)
        {
            result["packageUrl"] = descriptor.PackageUrl;
        }

        if (result["packageContentUrl"] is null && !string.IsNullOrWhiteSpace(descriptor.PackageContentUrl))
        {
            result["packageContentUrl"] = descriptor.PackageContentUrl;
        }

        if (result["catalogEntryUrl"] is null && !string.IsNullOrWhiteSpace(descriptor.CatalogEntryUrl))
        {
            result["catalogEntryUrl"] = descriptor.CatalogEntryUrl;
        }

        if (fallbackResult is null)
        {
            return;
        }

        ApplyFallback(result, AnalysisMode.Native, fallbackResult);
    }

    public static void ApplyAttemptLog(JsonObject result, JsonArray attemptLog)
    {
        if (result["analysisSelection"] is not JsonObject selection)
        {
            selection = new JsonObject();
            result["analysisSelection"] = selection;
        }

        selection["attempts"] = attemptLog;
    }

    public static void ApplyFallback(JsonObject result, string fallbackFrom, JsonObject fallbackResult)
    {
        result["fallback"] = new JsonObject
        {
            ["from"] = fallbackFrom,
            [ResultKey.Disposition] = fallbackResult[ResultKey.Disposition]?.GetValue<string>(),
            [ResultKey.Classification] = fallbackResult[ResultKey.Classification]?.GetValue<string>(),
            ["message"] = fallbackResult[ResultKey.FailureMessage]?.GetValue<string>(),
        };
    }

    public static JsonObject CreateFailureResult(string packageId, string version, string batchId, int attempt, string source, string message)
        => new()
        {
            ["schemaVersion"] = 1,
            ["packageId"] = packageId,
            ["version"] = version,
            [ResultKey.BatchId] = batchId,
            [ResultKey.Attempt] = attempt,
            [ResultKey.Source] = source,
            [ResultKey.AnalyzedAt] = DateTimeOffset.UtcNow.ToString("O"),
            [ResultKey.Disposition] = AnalysisDisposition.RetryableFailure,
            ["phase"] = "selection",
            [ResultKey.Classification] = "analysis-selection-failed",
            [ResultKey.FailureMessage] = message,
            ["timings"] = new JsonObject { ["totalMs"] = null },
            ["steps"] = new JsonObject { ["install"] = null, ["opencli"] = null, ["xmldoc"] = null },
            ["artifacts"] = new JsonObject { ["opencliArtifact"] = null, ["xmldocArtifact"] = null },
        };

    public static Task<int> WriteResultAsync(
        string packageId,
        string version,
        string resultPath,
        JsonObject result,
        bool json,
        bool suppressOutput,
        CancellationToken cancellationToken)
    {
        if (suppressOutput)
        {
            return Task.FromResult(0);
        }

        return CommandOutputSupport.WriteResultAsync(
            packageId,
            version,
            resultPath,
            result[ResultKey.Disposition]?.GetValue<string>(),
            json,
            cancellationToken,
            result[ResultKey.AnalysisMode]?.GetValue<string>(),
            result["analysisSelection"]?["reason"]?.GetValue<string>(),
            result["fallback"]?["from"]?.GetValue<string>());
    }
}

