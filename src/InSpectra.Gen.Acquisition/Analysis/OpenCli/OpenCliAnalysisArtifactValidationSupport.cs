namespace InSpectra.Gen.Acquisition.Analysis.OpenCli;

using InSpectra.Gen.Acquisition.Analysis.NonSpectre;
using InSpectra.Gen.Acquisition.Infrastructure.Paths;
using InSpectra.Gen.Acquisition.OpenCli.Documents;

using System.Text.Json.Nodes;

internal static class OpenCliAnalysisArtifactValidationSupport
{
    public static bool TryWriteValidatedArtifact(
        JsonObject result,
        string outputDirectory,
        JsonObject openCliDocument,
        string successClassification,
        string artifactSource)
    {
        if (!OpenCliDocumentValidator.TryValidateDocument(openCliDocument, out var validationError))
        {
            NonSpectreResultSupport.ApplyTerminalFailure(
                result,
                phase: "opencli",
                classification: "invalid-opencli-artifact",
                validationError ?? "Generated OpenCLI artifact is not publishable.");
            return false;
        }

        RepositoryPathResolver.WriteJsonFile(Path.Combine(outputDirectory, "opencli.json"), openCliDocument);
        result["artifacts"]!.AsObject()["opencliArtifact"] = "opencli.json";
        NonSpectreResultSupport.ApplySuccess(result, successClassification, artifactSource);
        return true;
    }
}
