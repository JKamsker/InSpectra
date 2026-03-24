namespace OpenCli.Renderer.Runtime;

public static class RenderRequestFactory
{
    public static RenderExecutionOptions CreateMarkdownOptions(
        CommonCommandSettings settings,
        string? layoutValue,
        string? outputFile,
        string? outputDirectory,
        int? timeoutSeconds,
        bool hasTimeoutSupport)
    {
        var outputMode = ResolveOutputMode(settings);
        var layout = ResolveMarkdownLayout(layoutValue);

        ValidateMarkdownLayoutOutputCombination(layout, outputMode, outputFile, outputDirectory);

        if (hasTimeoutSupport && timeoutSeconds is <= 0)
        {
            throw new CliUsageException("`--timeout` must be greater than zero.");
        }

        return new RenderExecutionOptions(
            layout,
            outputMode,
            settings.DryRun,
            ResolveFlag(settings.Quiet, "OPENCLI_RENDERER_QUIET"),
            ResolveFlag(settings.Verbose, "OPENCLI_RENDERER_VERBOSE"),
            settings.NoColor || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("NO_COLOR")),
            settings.IncludeHidden,
            settings.IncludeMetadata,
            settings.Overwrite,
            NormalizePath(outputFile),
            NormalizePath(outputDirectory));
    }

    public static RenderExecutionOptions CreateHtmlOptions(
        CommonCommandSettings settings,
        string? layoutValue,
        string? outputFile,
        string? outputDirectory,
        int? timeoutSeconds,
        bool hasTimeoutSupport)
    {
        var outputMode = ResolveOutputMode(settings);

        ValidateHtmlOutputCombination(layoutValue, outputFile, outputDirectory);

        if (hasTimeoutSupport && timeoutSeconds is <= 0)
        {
            throw new CliUsageException("`--timeout` must be greater than zero.");
        }

        return new RenderExecutionOptions(
            RenderLayout.App,
            outputMode,
            settings.DryRun,
            ResolveFlag(settings.Quiet, "OPENCLI_RENDERER_QUIET"),
            ResolveFlag(settings.Verbose, "OPENCLI_RENDERER_VERBOSE"),
            settings.NoColor || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("NO_COLOR")),
            settings.IncludeHidden,
            settings.IncludeMetadata,
            settings.Overwrite,
            OutputFile: null,
            NormalizePath(outputDirectory));
    }

    public static string ResolveWorkingDirectory(string? workingDirectory)
    {
        var resolved = string.IsNullOrWhiteSpace(workingDirectory)
            ? Directory.GetCurrentDirectory()
            : Path.GetFullPath(workingDirectory);

        if (!Directory.Exists(resolved))
        {
            throw new CliUsageException($"Working directory `{resolved}` does not exist.");
        }

        return resolved;
    }

    public static int ResolveTimeoutSeconds(int? timeoutSeconds)
    {
        if (timeoutSeconds is > 0)
        {
            return timeoutSeconds.Value;
        }

        var envValue = Environment.GetEnvironmentVariable("OPENCLI_RENDERER_TIMEOUT");
        if (!string.IsNullOrWhiteSpace(envValue))
        {
            if (!int.TryParse(envValue, out var parsed) || parsed <= 0)
            {
                throw new CliUsageException("`OPENCLI_RENDERER_TIMEOUT` must be a positive integer.");
            }

            return parsed;
        }

        return 30;
    }

    private static ResolvedOutputMode ResolveOutputMode(CommonCommandSettings settings)
    {
        var explicitOutput = settings.Output?.Trim().ToLowerInvariant();
        var envOutput = Environment.GetEnvironmentVariable("OPENCLI_RENDERER_OUTPUT")?.Trim().ToLowerInvariant();

        if (settings.Json && explicitOutput is "human")
        {
            throw new CliUsageException("`--json` cannot be combined with `--output human`.");
        }

        if (explicitOutput is not null and not ("human" or "json"))
        {
            throw new CliUsageException("`--output` must be `human` or `json`.");
        }

        if (envOutput is not null and not ("human" or "json"))
        {
            throw new CliUsageException("`OPENCLI_RENDERER_OUTPUT` must be `human` or `json`.");
        }

        if (explicitOutput is "json" || settings.Json)
        {
            return ResolvedOutputMode.Json;
        }

        if (explicitOutput is "human")
        {
            return ResolvedOutputMode.Human;
        }

        return envOutput == "json"
            ? ResolvedOutputMode.Json
            : ResolvedOutputMode.Human;
    }

    private static RenderLayout ResolveMarkdownLayout(string? layoutValue)
    {
        var normalized = layoutValue?.Trim().ToLowerInvariant();
        return normalized switch
        {
            null or "" or "single" => RenderLayout.Single,
            "tree" => RenderLayout.Tree,
            _ => throw new CliUsageException("`--layout` must be `single` or `tree`."),
        };
    }

    private static void ValidateMarkdownLayoutOutputCombination(
        RenderLayout layout,
        ResolvedOutputMode outputMode,
        string? outputFile,
        string? outputDirectory)
    {
        if (!string.IsNullOrWhiteSpace(outputFile) && !string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new CliUsageException("Use either `--out` or `--out-dir`, not both.");
        }

        if (layout == RenderLayout.Single && !string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new CliUsageException("`--out-dir` is only valid with `--layout tree`.");
        }

        if (layout == RenderLayout.Tree && !string.IsNullOrWhiteSpace(outputFile))
        {
            throw new CliUsageException("`--out` is only valid with `--layout single`.");
        }

        if (layout == RenderLayout.Tree && string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new CliUsageException("`--layout tree` requires `--out-dir`.");
        }

        if (outputMode == ResolvedOutputMode.Json && layout == RenderLayout.Single && string.IsNullOrWhiteSpace(outputFile))
        {
            throw new CliUsageException("Machine output requires `--out` for single-file rendering.");
        }
    }

    private static void ValidateHtmlOutputCombination(
        string? layoutValue,
        string? outputFile,
        string? outputDirectory)
    {
        if (!string.IsNullOrWhiteSpace(layoutValue))
        {
            throw new CliUsageException("`--layout` is not supported for HTML output. HTML always renders as an app bundle.");
        }

        if (!string.IsNullOrWhiteSpace(outputFile))
        {
            throw new CliUsageException("`--out` is not supported for HTML output. Use `--out-dir`.");
        }

        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new CliUsageException("HTML output requires `--out-dir`.");
        }
    }

    private static bool ResolveFlag(bool flag, string environmentVariable)
    {
        if (flag)
        {
            return true;
        }

        var value = Environment.GetEnvironmentVariable(environmentVariable);
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "1" or "true" or "yes" => true,
            "0" or "false" or "no" => false,
            _ => throw new CliUsageException($"`{environmentVariable}` must be a boolean value."),
        };
    }

    private static string? NormalizePath(string? path)
    {
        return string.IsNullOrWhiteSpace(path)
            ? null
            : Path.GetFullPath(path);
    }
}
