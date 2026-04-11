using System.Text.RegularExpressions;

namespace InSpectra.Gen.Tests.Architecture;

/// <summary>
/// Charter rules for acquisition modes (docs/architecture/ARCHITECTURE.md,
/// "Intra-module dependency rules" and "Placement rules"):
///
/// <list type="bullet">
///   <item>"One mode must not depend on another mode."</item>
///   <item>"In this repo, mode-specific conversion into OpenCLI lives under that mode as
///         <c>Projection/</c>, not as another <c>OpenCli</c> root."</item>
/// </list>
///
/// Both tests are skipped until <c>src/InSpectra.Gen.Acquisition/Modes/</c> exists
/// (introduced by Step 4) and until mode-specific <c>OpenCli</c> folders have been renamed
/// to <c>Projection</c> (Step 5).
/// </summary>
public sealed class ArchitectureModeTests
{
    /// <summary>Absolute path to <c>src/InSpectra.Gen.Acquisition/Modes</c>, may not exist yet.</summary>
    private static readonly string ModesRoot = Path.Combine(
        ArchitecturePolicyScanner.SrcRoot,
        ArchitecturePolicyScanner.AcquisitionProjectName,
        "Modes");

    /// <summary>
    /// Matches <c>using InSpectra.Gen.Acquisition.Modes.&lt;ModeName&gt;</c> at any depth.
    /// Captures the first segment after <c>Modes.</c> so the test can identify the target mode.
    /// </summary>
    private static readonly Regex ModeUsingDirective = new(
        @"^\s*using\s+InSpectra\.Gen\.Acquisition\.Modes\.(?<mode>[A-Za-z_][A-Za-z0-9_]*)",
        RegexOptions.Multiline | RegexOptions.Compiled);

    [Fact]
    public void No_cross_mode_dependencies()
    {
        Assert.True(Directory.Exists(ModesRoot), $"Expected Modes root at '{ModesRoot}' to exist.");

        var violations = new List<string>();

        foreach (var modeDirectory in Directory.EnumerateDirectories(ModesRoot))
        {
            var modeName = Path.GetFileName(modeDirectory);

            foreach (var filePath in Directory.EnumerateFiles(modeDirectory, "*.cs", SearchOption.AllDirectories))
            {
                var text = File.ReadAllText(filePath);
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

        var violations = new List<string>();

        foreach (var modeDirectory in Directory.EnumerateDirectories(ModesRoot))
        {
            foreach (var child in Directory.EnumerateDirectories(modeDirectory))
            {
                if (string.Equals(Path.GetFileName(child), "OpenCli", StringComparison.Ordinal))
                {
                    violations.Add($"- {ArchitecturePolicyScanner.GetRelativeRepoPath(child)}");
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            violations.Count == 0
                ? null
                : "Mode-specific OpenCli folders must be renamed to Projection, but found:"
                  + Environment.NewLine
                  + string.Join(Environment.NewLine, violations));
    }
}
