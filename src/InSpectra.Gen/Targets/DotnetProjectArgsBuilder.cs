namespace InSpectra.Gen.Targets;

/// <summary>
/// Builds the argument array passed to <c>dotnet</c> for the
/// <c>generate dotnet</c> commands. The final sequence is:
/// <code>run --project &lt;project&gt; [build flags] --</code>
/// so the renderer can append the CLI's export command (e.g. <c>cli opencli</c>)
/// without dotnet intercepting its flags.
/// </summary>
public static class DotnetProjectArgsBuilder
{
    public static string[] Build(
        string projectPath,
        string? configuration,
        string? framework,
        string? launchProfile,
        bool noBuild,
        bool noRestore)
    {
        var args = new List<string>
        {
            "run",
            "--project",
            projectPath,
        };

        if (!string.IsNullOrWhiteSpace(configuration))
        {
            args.Add("-c");
            args.Add(configuration);
        }

        if (!string.IsNullOrWhiteSpace(framework))
        {
            args.Add("-f");
            args.Add(framework);
        }

        if (!string.IsNullOrWhiteSpace(launchProfile))
        {
            args.Add("--launch-profile");
            args.Add(launchProfile);
        }

        if (noBuild)
        {
            args.Add("--no-build");
        }

        if (noRestore)
        {
            args.Add("--no-restore");
        }

        // Anything appended after this separator is forwarded to the user's CLI.
        args.Add("--");

        return args.ToArray();
    }
}
