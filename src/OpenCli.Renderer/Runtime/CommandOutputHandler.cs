using System.Text.Json;

namespace OpenCli.Renderer.Runtime;

public static class CommandOutputHandler
{
    public static async Task<int> ExecuteAsync(
        ResolvedOutputMode outputMode,
        bool verbose,
        Func<Task<RenderExecutionResult>> action)
    {
        try
        {
            var result = await action();
            await WriteSuccessAsync(outputMode, result);
            return 0;
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

    private static async Task WriteSuccessAsync(ResolvedOutputMode outputMode, RenderExecutionResult result)
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

        if (!string.IsNullOrWhiteSpace(result.StdoutDocument))
        {
            await Console.Out.WriteAsync(result.StdoutDocument);
            if (!result.StdoutDocument.EndsWith(Environment.NewLine, StringComparison.Ordinal))
            {
                await Console.Out.WriteLineAsync();
            }

            return;
        }

        if (!string.IsNullOrWhiteSpace(result.Summary))
        {
            await Console.Out.WriteLineAsync(result.Summary);
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
}
