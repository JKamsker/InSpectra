using System.Text.Json;

using InSpectra.Gen.Cli.Output.Json;
using InSpectra.Lib;
using InSpectra.Lib.Rendering.Contracts;

namespace InSpectra.Gen.Cli.Output;

public static class RenderOutputHandler
{
    public static async Task<int> ExecuteAsync(
        ResolvedOutputMode outputMode,
        bool quiet,
        bool verbose,
        Func<Task<RenderExecutionResult>> action)
    {
        try
        {
            var result = await action();
            await WriteSuccessAsync(outputMode, quiet, result);
            return 0;
        }
        catch (OperationCanceledException)
        {
            var cliException = new CliException("Operation cancelled.", "cancelled", 130);
            await WriteFailureAsync(outputMode, cliException);
            return cliException.ExitCode;
        }
        catch (CliException exception)
        {
            await WriteFailureAsync(outputMode, exception);
            return exception.ExitCode;
        }
        catch (Exception exception)
        {
            var cliException = new CliException(
                verbose ? exception.ToString() : "Unexpected failure.",
                "unexpected",
                1,
                verbose ? [exception.ToString()] : []);

            await WriteFailureAsync(outputMode, cliException);
            return cliException.ExitCode;
        }
    }

    private static async Task WriteSuccessAsync(ResolvedOutputMode outputMode, bool quiet, RenderExecutionResult result)
    {
        if (outputMode == ResolvedOutputMode.Json)
        {
            var envelope = new JsonEnvelope<object>
            {
                Ok = true,
                Data = new
                {
                    format = result.Format == DocumentFormat.Html ? "html" : "markdown",
                    layout = result.Layout switch
                    {
                        RenderLayout.Hybrid => "hybrid",
                        RenderLayout.Tree => "tree",
                        RenderLayout.App => "app",
                        _ => "single",
                    },
                    dryRun = result.IsDryRun,
                    source = new
                    {
                        kind = result.Source.Kind,
                        openCli = result.Source.OpenCliOrigin,
                        xmlDoc = result.Source.XmlDocOrigin,
                        executablePath = result.Source.ExecutablePath,
                    },
                    acquisition = result.Acquisition is null
                        ? null
                        : new
                        {
                            selectedMode = result.Acquisition.SelectedMode,
                            commandName = result.Acquisition.CommandName,
                            cliFramework = result.Acquisition.CliFramework,
                            attempts = result.Acquisition.Attempts.Select(attempt => new
                            {
                                mode = attempt.Mode,
                                framework = attempt.Framework,
                                outcome = attempt.Outcome,
                                detail = attempt.Detail,
                            }),
                            artifacts = new
                            {
                                openCli = result.Acquisition.OpenCliOutputPath,
                                crawl = result.Acquisition.CrawlOutputPath,
                            },
                        },
                    output = new
                    {
                        mode = result.StdoutDocument is not null ? "stdout" : "files",
                        files = result.Files.Select(file => new
                        {
                            path = file.FullPath,
                            relativePath = file.RelativePath,
                        }),
                    },
                    stats = new
                    {
                        commandCount = result.Stats.CommandCount,
                        optionCount = result.Stats.OptionCount,
                        argumentCount = result.Stats.ArgumentCount,
                        fileCount = result.Stats.FileCount,
                    },
                },
                Meta = new JsonMeta
                {
                    Warnings = result.Warnings,
                },
            };

            await Console.Out.WriteLineAsync(JsonSerializer.Serialize(envelope, JsonOutput.SerializerOptions));
            return;
        }

        await WriteWarningsAsync(result.Warnings);

        if (!string.IsNullOrWhiteSpace(result.StdoutDocument))
        {
            await Console.Out.WriteAsync(result.StdoutDocument);
            if (!result.StdoutDocument.EndsWith(Environment.NewLine, StringComparison.Ordinal))
            {
                await Console.Out.WriteLineAsync();
            }

            return;
        }

        if (quiet)
        {
            return;
        }

        var summary = BuildSummary(result);
        if (!string.IsNullOrWhiteSpace(summary))
        {
            await Console.Out.WriteLineAsync(summary);
        }
    }

    private static async Task WriteFailureAsync(ResolvedOutputMode outputMode, CliException exception)
    {
        if (outputMode == ResolvedOutputMode.Json)
        {
            var envelope = new JsonEnvelope<object>
            {
                Ok = false,
                Data = null,
                Error = new JsonError
                {
                    Kind = exception.ErrorKind,
                    Message = exception.Message,
                    Details = exception.Details,
                },
                Meta = new JsonMeta(),
            };

            await Console.Out.WriteLineAsync(JsonSerializer.Serialize(envelope, JsonOutput.SerializerOptions));
            return;
        }

        await Console.Error.WriteLineAsync(exception.Message);
        foreach (var detail in exception.Details)
        {
            await Console.Error.WriteLineAsync($"- {detail}");
        }
    }

    private static async Task WriteWarningsAsync(IReadOnlyList<string> warnings)
    {
        foreach (var warning in warnings)
        {
            await Console.Error.WriteLineAsync($"Warning: {warning}");
        }
    }

    private static string? BuildSummary(RenderExecutionResult result)
    {
        return result.Format switch
        {
            DocumentFormat.Markdown => BuildMarkdownSummary(result),
            DocumentFormat.Html => BuildHtmlSummary(result),
            _ => null,
        };
    }

    private static string? BuildMarkdownSummary(RenderExecutionResult result)
    {
        return result.Layout switch
        {
            RenderLayout.Single => BuildSingleMarkdownSummary(result),
            RenderLayout.Tree => BuildMultiFileMarkdownSummary(result, "Markdown", "a Markdown tree"),
            RenderLayout.Hybrid => BuildMultiFileMarkdownSummary(result, "hybrid Markdown", "hybrid Markdown"),
            _ => null,
        };
    }

    private static string? BuildSingleMarkdownSummary(RenderExecutionResult result)
    {
        if (result.IsDryRun)
        {
            return result.Files.Count == 0
                ? $"Dry run: render `{result.Source.OpenCliOrigin}` as single Markdown to stdout."
                : $"Dry run: render `{result.Source.OpenCliOrigin}` as single Markdown to `{result.Files[0].FullPath}`.";
        }

        return result.Files.Count == 0
            ? null
            : $"Wrote Markdown to `{result.Files[0].FullPath}`.";
    }

    private static string? BuildMultiFileMarkdownSummary(
        RenderExecutionResult result,
        string completedDescription,
        string plannedDescription)
    {
        var outputDirectory = ResolveOutputDirectory(result.Files);
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            return null;
        }

        return result.IsDryRun
            ? $"Dry run: render `{result.Source.OpenCliOrigin}` as {plannedDescription} in `{outputDirectory}` ({result.Files.Count} files planned)."
            : $"Wrote {result.Files.Count} {completedDescription} files to `{outputDirectory}`.";
    }

    private static string? BuildHtmlSummary(RenderExecutionResult result)
    {
        var outputDirectory = ResolveOutputDirectory(result.Files);
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            return null;
        }

        return result.IsDryRun
            ? $"Dry run: render `{result.Source.OpenCliOrigin}` as an HTML app bundle in `{outputDirectory}` ({result.Files.Count} files planned)."
            : $"Wrote HTML app bundle ({result.Files.Count} files) to `{outputDirectory}`.";
    }

    private static string? ResolveOutputDirectory(IReadOnlyList<RenderedFile> files)
    {
        if (files.Count == 0)
        {
            return null;
        }

        var root = Path.GetDirectoryName(Path.GetFullPath(files[0].FullPath));
        if (string.IsNullOrWhiteSpace(root))
        {
            return null;
        }

        while (!AllFilesShareDirectory(files, root))
        {
            root = Path.GetDirectoryName(root);
            if (string.IsNullOrWhiteSpace(root))
            {
                return null;
            }
        }

        return root;
    }

    private static bool AllFilesShareDirectory(IReadOnlyList<RenderedFile> files, string directoryPath)
    {
        var fullDirectory = Path.GetFullPath(directoryPath);
        var directoryPrefix = Path.EndsInDirectorySeparator(fullDirectory)
            ? fullDirectory
            : fullDirectory + Path.DirectorySeparatorChar;
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        return files.All(file =>
        {
            var fullPath = Path.GetFullPath(file.FullPath);
            return fullPath.StartsWith(directoryPrefix, comparison);
        });
    }
}
