namespace InSpectra.Gen.Acquisition.Analysis.NonSpectre;

using InSpectra.Gen.Acquisition.Analysis.Output;

using InSpectra.Gen.Acquisition.Analysis;

using System.Text.Json.Nodes;

internal static class NonSpectreResultSupport
{
    public static JsonObject CreateInitialResult(
        string packageId,
        string version,
        string? commandName,
        string batchId,
        int attempt,
        string source,
        string? cliFramework,
        string analysisMode,
        DateTimeOffset analyzedAt)
    {
        var result = ResultSupport.CreateInitialResult(packageId, version, batchId, attempt, source, analyzedAt);
        result["command"] = commandName;
        result[ResultKey.CliFramework] = cliFramework;
        result[ResultKey.AnalysisMode] = analysisMode;
        result["timings"]!.AsObject()["crawlMs"] = null;
        result["artifacts"]!.AsObject()["crawlArtifact"] = null;
        return result;
    }

    public static void ApplyRetryableFailure(JsonObject result, string phase, string classification, string? message)
    {
        result[ResultKey.Disposition] = AnalysisDisposition.RetryableFailure;
        result["retryEligible"] = true;
        result["phase"] = phase;
        result[ResultKey.Classification] = classification;
        result[ResultKey.FailureMessage] = message;
    }

    public static void ApplyUnexpectedRetryableFailure(JsonObject result, string? message)
    {
        var phase = result["phase"]?.GetValue<string>();
        var classification = result[ResultKey.Classification]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(phase))
        {
            phase = "bootstrap";
        }

        if (string.IsNullOrWhiteSpace(classification) || string.Equals(classification, "uninitialized", StringComparison.Ordinal))
        {
            classification = "unexpected-exception";
        }

        ApplyRetryableFailure(result, phase, classification, message);
    }

    public static void ApplyTerminalFailure(JsonObject result, string phase, string classification, string? message)
    {
        result[ResultKey.Disposition] = AnalysisDisposition.TerminalFailure;
        result["retryEligible"] = false;
        result["phase"] = phase;
        result[ResultKey.Classification] = classification;
        result[ResultKey.FailureMessage] = message;
    }

    public static void ApplySuccess(JsonObject result, string classification, string artifactSource)
    {
        result[ResultKey.Disposition] = AnalysisDisposition.Success;
        result["retryEligible"] = false;
        result["phase"] = "complete";
        result[ResultKey.Classification] = classification;
        result[ResultKey.FailureMessage] = null;
        result["failureSignature"] = null;
        result["opencliSource"] = artifactSource;

        var openCliStep = new JsonObject
        {
            ["status"] = "ok",
            [ResultKey.Classification] = classification,
            ["artifactSource"] = artifactSource,
        };
        result["steps"]!.AsObject()["opencli"] = openCliStep.DeepClone();
        result["introspection"]!.AsObject()["opencli"] = openCliStep.DeepClone();
    }

    public static void FinalizeFailureSignature(JsonObject result)
    {
        var disposition = result[ResultKey.Disposition]?.GetValue<string>();
        if (disposition is "retryable-failure" or "terminal-failure")
        {
            result["failureSignature"] = ResultSupport.GetFailureSignature(
                result["phase"]?.GetValue<string>() ?? "unknown",
                result[ResultKey.Classification]?.GetValue<string>() ?? "unknown",
                result[ResultKey.FailureMessage]?.GetValue<string>());
        }
        else
        {
            result["failureSignature"] = null;
        }
    }
}

