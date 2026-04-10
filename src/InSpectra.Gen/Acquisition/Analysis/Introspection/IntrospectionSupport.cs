namespace InSpectra.Gen.Acquisition.Analysis.Introspection;

using InSpectra.Gen.Acquisition.Infrastructure.Paths;
using InSpectra.Gen.Acquisition.Infrastructure.Commands;

using InSpectra.Gen.Acquisition.OpenCli.Documents;

using InSpectra.Gen.Acquisition.Analysis.Execution;

using InSpectra.Discovery.Tool.Analysis;

using System.Text.Json.Nodes;

internal static class IntrospectionSupport
{
    public static async Task<IntrospectionOutcome> InvokeIntrospectionCommandAsync(
        string commandPath,
        IReadOnlyList<string> argumentList,
        string expectedFormat,
        string workingDirectory,
        IReadOnlyDictionary<string, string> environment,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        var processResult = await RuntimeSupport.InvokeProcessCaptureAsync(
            commandPath,
            argumentList,
            workingDirectory,
            environment,
            timeoutSeconds,
            cancellationToken);
        var preferredMessage = RuntimeSupport.GetPreferredMessage(processResult.Stdout, processResult.Stderr);
        var classification = IntrospectionFailureClassifier.Classify(argumentList, preferredMessage);
        var parse = IntrospectionPayloadParser.TryParse(expectedFormat, processResult.Stdout);

        var status = AnalysisDisposition.Failed;
        var dispositionHint = AnalysisDisposition.RetryableFailure;
        var message = preferredMessage;
        JsonNode? artifactObject = null;
        string? artifactText = null;

        if (processResult.TimedOut)
        {
            status = "timed-out";
            if (classification is "requires-configuration" or "environment-missing-dependency" or "requires-interactive-authentication" or "requires-interactive-input" or "unsupported-platform")
            {
                dispositionHint = AnalysisDisposition.TerminalFailure;
            }
            else if (classification == "environment-missing-runtime")
            {
                dispositionHint = AnalysisDisposition.RetryableFailure;
            }
            else
            {
                classification = "timeout";
                dispositionHint = AnalysisDisposition.RetryableFailure;
            }

            message ??= "Command timed out.";
        }
        else if (processResult.OutputLimitExceeded)
        {
            status = "invalid-output";
            classification = "output-too-large";
            dispositionHint = AnalysisDisposition.TerminalFailure;
            message = ProcessOutputCaptureSupport.BuildOutputLimitExceededMessage();
        }
        else if (parse.Success)
        {
            status = "ok";
            classification = string.Equals(expectedFormat, "json", StringComparison.OrdinalIgnoreCase)
                ? processResult.ExitCode == 0 ? "json-ready" : "json-ready-with-nonzero-exit"
                : processResult.ExitCode == 0 ? "xml-ready" : "xml-ready-with-nonzero-exit";
            dispositionHint = AnalysisDisposition.Success;
            artifactObject = parse.Document;
            artifactText = parse.ArtifactText;
            message = processResult.ExitCode == 0 ? null : preferredMessage;
        }
        else if (!string.IsNullOrWhiteSpace(classification))
        {
            status = classification == "unsupported-command" ? "unsupported" : AnalysisDisposition.Failed;
            dispositionHint = classification == "environment-missing-runtime" ? AnalysisDisposition.RetryableFailure : AnalysisDisposition.TerminalFailure;
            message ??= parse.Error;
        }
        else if (processResult.ExitCode == 0)
        {
            status = "invalid-output";
            classification = string.Equals(expectedFormat, "json", StringComparison.OrdinalIgnoreCase) ? "invalid-json" : "invalid-xml";
            dispositionHint = AnalysisDisposition.TerminalFailure;
            message = parse.Error ?? "Command exited successfully but did not emit valid output.";
        }
        else
        {
            status = AnalysisDisposition.Failed;
            classification = "command-failed";
            dispositionHint = AnalysisDisposition.RetryableFailure;
            message ??= parse.Error;
        }

        return new IntrospectionOutcome(
            CommandName: argumentList[^1],
            ProcessResult: processResult,
            Status: status,
            Classification: classification ?? "command-failed",
            DispositionHint: dispositionHint,
            Message: message,
            ArtifactObject: artifactObject,
            ArtifactText: artifactText);
    }

    public static void ApplyOutputs(
        JsonObject result,
        string outputDirectory,
        ref IntrospectionOutcome openCliOutcome,
        IntrospectionOutcome xmlDocOutcome)
    {
        result["timings"]!.AsObject()["opencliMs"] = openCliOutcome.ProcessResult.DurationMs;
        result["timings"]!.AsObject()["xmldocMs"] = xmlDocOutcome.ProcessResult.DurationMs;

        openCliOutcome = NormalizeOpenCliOutcome(openCliOutcome);

        if (openCliOutcome.ArtifactObject is JsonObject openCliDocument)
        {
            RepositoryPathResolver.WriteJsonFile(Path.Combine(outputDirectory, "opencli.json"), openCliDocument);
            result["artifacts"]!.AsObject()["opencliArtifact"] = "opencli.json";
        }

        if (!string.IsNullOrWhiteSpace(xmlDocOutcome.ArtifactText))
        {
            RepositoryPathResolver.WriteTextFile(Path.Combine(outputDirectory, "xmldoc.xml"), xmlDocOutcome.ArtifactText);
            result["artifacts"]!.AsObject()["xmldocArtifact"] = "xmldoc.xml";
        }

        result["introspection"]!.AsObject()["opencli"] = new JsonObject
        {
            ["status"] = openCliOutcome.Status,
            [ResultKey.Classification] = openCliOutcome.Classification,
            ["message"] = openCliOutcome.Message,
        };
        result["introspection"]!.AsObject()["xmldoc"] = new JsonObject
        {
            ["status"] = xmlDocOutcome.Status,
            [ResultKey.Classification] = xmlDocOutcome.Classification,
            ["message"] = xmlDocOutcome.Message,
        };

        result["steps"]!.AsObject()["opencli"] = openCliOutcome.ToStepMetadata(result["artifacts"]?["opencliArtifact"]?.GetValue<string>());
        result["steps"]!.AsObject()["xmldoc"] = xmlDocOutcome.ToStepMetadata(result["artifacts"]?["xmldocArtifact"]?.GetValue<string>());
    }

    private static IntrospectionOutcome NormalizeOpenCliOutcome(IntrospectionOutcome openCliOutcome)
    {
        if (openCliOutcome.ArtifactObject is null)
        {
            return openCliOutcome;
        }

        if (openCliOutcome.ArtifactObject is not JsonObject openCliDocument)
        {
            return CreateInvalidOpenCliOutcome(openCliOutcome, "OpenCLI artifact is not a JSON object.");
        }

        OpenCliDocumentSanitizer.EnsureArtifactSource(openCliDocument, "tool-output");
        OpenCliDocumentSanitizer.Sanitize(openCliDocument);

        if (!OpenCliDocumentValidator.TryValidateDocument(openCliDocument, out var validationError))
        {
            return CreateInvalidOpenCliOutcome(
                openCliOutcome,
                validationError ?? "Generated OpenCLI artifact is not publishable.");
        }

        return openCliOutcome with
        {
            ArtifactObject = openCliDocument,
        };
    }

    private static IntrospectionOutcome CreateInvalidOpenCliOutcome(IntrospectionOutcome openCliOutcome, string message)
        => openCliOutcome with
        {
            Status = "invalid-output",
            Classification = "invalid-opencli-artifact",
            DispositionHint = "terminal-failure",
            Message = message,
            ArtifactObject = null,
            ArtifactText = null,
        };

    public static void ApplyClassification(JsonObject result, IntrospectionOutcome openCliOutcome, IntrospectionOutcome xmlDocOutcome)
    {
        var outcomes = new[] { openCliOutcome, xmlDocOutcome };
        var successfulOutcomes = outcomes.Where(outcome => outcome.Status == "ok").ToArray();
        var retryableOutcomes = outcomes.Where(outcome => outcome.Status != "ok" && outcome.DispositionHint == AnalysisDisposition.RetryableFailure).ToArray();
        var deterministicOutcomes = outcomes.Where(outcome => outcome.Status != "ok" && outcome.DispositionHint == AnalysisDisposition.TerminalFailure).ToArray();

        if (successfulOutcomes.Length == 2)
        {
            result[ResultKey.Disposition] = AnalysisDisposition.Success;
            result["retryEligible"] = false;
            result["phase"] = "complete";
            result[ResultKey.Classification] = "spectre-cli-confirmed";
            result[ResultKey.FailureMessage] = null;
            return;
        }

        if (successfulOutcomes.Length == 1 && retryableOutcomes.Length == 0)
        {
            result[ResultKey.Disposition] = AnalysisDisposition.Success;
            result["retryEligible"] = false;
            result["phase"] = "complete";
            result[ResultKey.Classification] = successfulOutcomes[0].CommandName == "opencli"
                ? "spectre-cli-opencli-only"
                : "spectre-cli-xmldoc-only";
            result[ResultKey.FailureMessage] = null;
            return;
        }

        if (retryableOutcomes.Length > 0)
        {
            var primaryFailure = retryableOutcomes[0];
            result[ResultKey.Disposition] = AnalysisDisposition.RetryableFailure;
            result["retryEligible"] = true;
            result["phase"] = primaryFailure.CommandName;
            result[ResultKey.Classification] = primaryFailure.Classification;
            result[ResultKey.FailureMessage] = primaryFailure.Message;
            return;
        }

        if (deterministicOutcomes.Length > 0)
        {
            var primaryFailure = deterministicOutcomes[0];
            result[ResultKey.Disposition] = AnalysisDisposition.TerminalFailure;
            result["retryEligible"] = false;
            result["phase"] = primaryFailure.CommandName;
            result[ResultKey.Classification] = primaryFailure.Classification;
            result[ResultKey.FailureMessage] = primaryFailure.Message;
            return;
        }

        result[ResultKey.Disposition] = AnalysisDisposition.RetryableFailure;
        result["retryEligible"] = true;
        result["phase"] = "introspection";
        result[ResultKey.Classification] = "introspection-unresolved";
        result[ResultKey.FailureMessage] = "The tool did not yield a usable introspection result.";
    }
}
