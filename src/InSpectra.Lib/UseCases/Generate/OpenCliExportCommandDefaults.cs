namespace InSpectra.Lib.UseCases.Generate;

/// <summary>
/// Shared default export commands for CLIs that expose the InSpectra/OpenCLI surface.
/// Keep these in one place so the app shell and live regression harness use the same
/// native invocation shape.
/// </summary>
public static class OpenCliExportCommandDefaults
{
    public static IReadOnlyList<string> OpenCliArguments { get; } = ["cli", "opencli"];

    public static IReadOnlyList<string> XmlDocArguments { get; } = ["cli", "xmldoc"];
}
