using System.Text.RegularExpressions;

namespace InSpectra.Gen.Tests.Architecture;

/// <summary>
/// Charter rules for engine modes (docs/architecture/ARCHITECTURE.md,
/// "Intra-module dependency rules" and "Placement rules"):
///
/// <list type="bullet">
///   <item>"One mode must not depend on another mode."</item>
///   <item>"In this repo, mode-specific conversion into OpenCLI lives under that mode as
///         <c>Projection/</c>, not as another <c>OpenCli</c> root."</item>
/// </list>
/// </summary>
public sealed class ArchitectureModeTests
{
    /// <summary>Absolute path to <c>src/InSpectra.Gen.Engine/Modes</c>.</summary>
    private static readonly string ModesRoot = Path.Combine(
        ArchitecturePolicyScanner.SrcRoot,
        ArchitecturePolicyScanner.EngineProjectName,
        "Modes");

    /// <summary>
    /// Matches <c>using InSpectra.Gen.Engine.Modes.&lt;ModeName&gt;</c> at any depth.
    /// Captures the first segment after <c>Modes.</c> so the test can identify the target mode.
    /// </summary>
    private static readonly Regex ModeUsingDirective = new(
        @"^\s*using\s+InSpectra\.Gen\.Engine\.Modes\.(?<mode>[A-Za-z_][A-Za-z0-9_]*)",
        RegexOptions.Multiline | RegexOptions.Compiled);

    /// <summary>
    /// Positive anchor for the modes scan: the scanned <c>Modes/</c> surface should contain
    /// at least one matching <c>InSpectra.Gen.Engine.Modes.&lt;Mode&gt;...</c>
    /// namespace declaration. Without that anchor, the test could scan the wrong surface
    /// and still pass on zero meaningful matches.
    /// </summary>
    private static readonly Regex ModeNamespaceDeclaration = new(
        @"^\s*namespace\s+InSpectra\.Gen\.Engine\.Modes\.(?<mode>[A-Za-z_][A-Za-z0-9_]*)(?:\.[A-Za-z_][A-Za-z0-9_]*)*\s*[;{]",
        RegexOptions.Multiline | RegexOptions.Compiled);

    [Fact]
    public void No_cross_mode_dependencies()
    {
        Assert.True(Directory.Exists(ModesRoot), $"Expected Modes root at '{ModesRoot}' to exist.");
        var modeDirectories = Directory.EnumerateDirectories(ModesRoot).OrderBy(path => path, StringComparer.Ordinal).ToArray();
        Assert.NotEmpty(modeDirectories);

        var violations = new List<string>();
        var filesScanned = 0;
        var matchingNamespacesSeen = 0;

        foreach (var modeDirectory in modeDirectories)
        {
            var modeName = Path.GetFileName(modeDirectory);

            foreach (var filePath in Directory.EnumerateFiles(modeDirectory, "*.cs", SearchOption.AllDirectories))
            {
                if (IsIgnoredPath(filePath))
                {
                    continue;
                }

                filesScanned++;
                var text = File.ReadAllText(filePath);
                foreach (Match namespaceMatch in ModeNamespaceDeclaration.Matches(text))
                {
                    if (!string.IsNullOrWhiteSpace(namespaceMatch.Groups["mode"].Value))
                    {
                        matchingNamespacesSeen++;
                    }
                }

                foreach (Match match in ModeUsingDirective.Matches(text))
                {
                    var referencedMode = match.Groups["mode"].Value;
                    if (!string.Equals(referencedMode, modeName, StringComparison.Ordinal))
                    {
                        violations.Add(
                            $"- {ArchitecturePolicyScanner.GetRelativeRepoPath(filePath)} (mode '{modeName}')"
                            + $" references mode '{referencedMode}'");
                    }
                }
            }
        }

        Assert.True(
            filesScanned > 0,
            $"Expected Modes root at '{ModesRoot}' to contain at least one tracked .cs file but found none.");

        Assert.True(
            matchingNamespacesSeen > 0,
            $"Expected Modes scan under '{ModesRoot}' to encounter at least one InSpectra.Gen.Engine.Modes.<Mode> namespace declaration but found none.");

        Assert.True(
            violations.Count == 0,
            violations.Count == 0
                ? null
                : "Modes must not depend on other modes, but found:"
                  + Environment.NewLine
                  + string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void Mode_specific_OpenCli_folders_are_renamed_to_Projection()
    {
        Assert.True(Directory.Exists(ModesRoot), $"Expected Modes root at '{ModesRoot}' to exist.");
        var modeDirectories = Directory.EnumerateDirectories(ModesRoot).OrderBy(path => path, StringComparer.Ordinal).ToArray();
        Assert.NotEmpty(modeDirectories);

        var violations = new List<string>();
        var projectionDirectoriesSeen = modeDirectories.Count(modeDirectory =>
            Directory.Exists(Path.Combine(modeDirectory, "Projection")));

        foreach (var modeDirectory in modeDirectories)
        {
            foreach (var child in Directory.EnumerateDirectories(modeDirectory, "*", SearchOption.AllDirectories))
            {
                if (IsIgnoredPath(child))
                {
                    continue;
                }

                var directoryName = Path.GetFileName(child);
                if (string.Equals(directoryName, "OpenCli", StringComparison.Ordinal))
                {
                    violations.Add($"- {ArchitecturePolicyScanner.GetRelativeRepoPath(child)}");
                }
            }
        }

        Assert.True(
            projectionDirectoriesSeen > 0,
            $"Expected at least one mode subtree under '{ModesRoot}' to contain a Projection directory, but found none.");

        Assert.True(
            violations.Count == 0,
            violations.Count == 0
                ? null
                : "Mode-specific OpenCli folders must be renamed to Projection, but found:"
                  + Environment.NewLine
                  + string.Join(Environment.NewLine, violations));
    }

    private static bool IsIgnoredPath(string path)
    {
        var segments = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return segments.Any(segment =>
            string.Equals(segment, "bin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(segment, "obj", StringComparison.OrdinalIgnoreCase));
    }
}
