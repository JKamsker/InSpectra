using InSpectra.Gen.UseCases.Generate.Requests;

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
    public static string[] Build(DotnetBuildSettings settings)
    {
        var args = new List<string>
        {
            "run",
            "--project",
            settings.ProjectPath,
        };

        if (!string.IsNullOrWhiteSpace(settings.Configuration))
        {
            args.Add("-c");
            args.Add(settings.Configuration);
        }

        if (!string.IsNullOrWhiteSpace(settings.Framework))
        {
            args.Add("-f");
            args.Add(settings.Framework);
        }

        if (!string.IsNullOrWhiteSpace(settings.LaunchProfile))
        {
            args.Add("--launch-profile");
            args.Add(settings.LaunchProfile);
        }

        if (settings.NoBuild)
        {
            args.Add("--no-build");
        }

        if (settings.NoRestore)
        {
            args.Add("--no-restore");
        }

        // Anything appended after this separator is forwarded to the user's CLI.
        args.Add("--");

        return args.ToArray();
    }
}
