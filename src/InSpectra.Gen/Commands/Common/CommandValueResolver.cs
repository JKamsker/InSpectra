using InSpectra.Gen.Cli;
using InSpectra.Lib;
using InSpectra.Lib.Contracts;
using InSpectra.Lib.UseCases.Generate.Requests;
namespace InSpectra.Gen.Commands.Common;

internal static class CommandValueResolver
{
    public static ResolvedOutputMode ResolveOutputMode(bool json, string? output)
    {
        var explicitOutput = output?.Trim().ToLowerInvariant();
        var envOutput = Environment.GetEnvironmentVariable("INSPECTRA_GEN_OUTPUT")?.Trim().ToLowerInvariant();

        if (json && explicitOutput is "human")
        {
            throw new CliUsageException("`--json` cannot be combined with `--output human`.");
        }

        if (explicitOutput is not null and not ("human" or "json"))
        {
            throw new CliUsageException("`--output` must be `human` or `json`.");
        }

        if (explicitOutput is "json" || json)
        {
            return ResolvedOutputMode.Json;
        }

        if (explicitOutput is "human")
        {
            return ResolvedOutputMode.Human;
        }

        if (envOutput is not null and not ("human" or "json"))
        {
            throw new CliUsageException("`INSPECTRA_GEN_OUTPUT` must be `human` or `json`.");
        }

        return envOutput == "json"
            ? ResolvedOutputMode.Json
            : ResolvedOutputMode.Human;
    }

    public static bool ResolveFlag(bool flag, string environmentVariable)
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

    public static bool ResolveNoColor(bool noColor)
        => noColor || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("NO_COLOR"));

    public static OpenCliMode ResolveOpenCliMode(string? value, OpenCliMode defaultMode)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return defaultMode;
        }

        return normalized switch
        {
            AnalysisMode.Native => OpenCliMode.Native,
            AnalysisMode.Auto => OpenCliMode.Auto,
            AnalysisMode.Help => OpenCliMode.Help,
            AnalysisMode.CliFx => OpenCliMode.CliFx,
            AnalysisMode.Static => OpenCliMode.Static,
            AnalysisMode.Hook => OpenCliMode.Hook,
            _ => throw new CliUsageException("`--opencli-mode` must be `native`, `auto`, `help`, `clifx`, `static`, or `hook`."),
        };
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

    public static int ResolveTimeoutSeconds(int? timeoutSeconds, int defaultSeconds = 30)
    {
        if (timeoutSeconds.HasValue)
        {
            if (timeoutSeconds.Value <= 0)
            {
                throw new CliUsageException("`--timeout` must be a positive integer.");
            }

            return timeoutSeconds.Value;
        }

        var envValue = Environment.GetEnvironmentVariable("INSPECTRA_GEN_TIMEOUT");
        if (!string.IsNullOrWhiteSpace(envValue))
        {
            if (!int.TryParse(envValue, out var parsed) || parsed <= 0)
            {
                throw new CliUsageException("`INSPECTRA_GEN_TIMEOUT` must be a positive integer.");
            }

            return parsed;
        }

        return defaultSeconds;
    }

    public static string? NormalizePath(string? path)
    {
        return string.IsNullOrWhiteSpace(path)
            ? null
            : Path.GetFullPath(path);
    }

    public static string? NormalizeText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
