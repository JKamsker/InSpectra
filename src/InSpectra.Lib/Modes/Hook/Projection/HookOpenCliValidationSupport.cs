namespace InSpectra.Lib.Modes.Hook.Projection;

using InSpectra.Lib.Tooling.Results;

using System.Text.Json.Nodes;

internal static class HookOpenCliValidationSupport
{
    public static bool TryWriteValidatedArtifact(JsonObject result, string outputDirectory, JsonObject openCliDocument)
        => OpenCliAnalysisArtifactValidationSupport.TryWriteValidatedArtifact(
            result,
            outputDirectory,
            openCliDocument,
            successClassification: "startup-hook",
            artifactSource: "startup-hook");
}
