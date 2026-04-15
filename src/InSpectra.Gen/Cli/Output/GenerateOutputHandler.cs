using System.Text.Json;

using InSpectra.Gen.Cli.Output.Json;
using InSpectra.Lib;
using InSpectra.Lib.Rendering.Contracts;
using InSpectra.Lib.UseCases.Generate;

namespace InSpectra.Gen.Cli.Output;

public static class GenerateOutputHandler
{
    public static async Task<int> ExecuteAsync(
        ResolvedOutputMode outputMode,
        bool verbose,
        Func<Task<GenerateExecutionResult>> action)
    {
        try
        {
            var result = await action();
            await WriteSuccessAsync(outputMode, result);
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

    private static async Task WriteSuccessAsync(ResolvedOutputMode outputMode, GenerateExecutionResult result)
    {
        if (outputMode == ResolvedOutputMode.Json)
        {
            var envelope = new JsonEnvelope<object>
            {
                Ok = true,
                Data = new
                {
                    source = new
                    {
                        kind = result.Source.Kind,
                        openCli = result.Source.OpenCliOrigin,
                        xmlDoc = result.Source.XmlDocOrigin,
                        executablePath = result.Source.ExecutablePath,
                    },
                    acquisition = new
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
                            openCli = result.Acquisition.OpenCliOutputPath ?? result.OutputFile,
                            crawl = result.Acquisition.CrawlOutputPath,
                        },
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

        if (string.IsNullOrWhiteSpace(result.OutputFile))
        {
            await Console.Out.WriteAsync(result.OpenCliJson);
            if (!result.OpenCliJson.EndsWith(Environment.NewLine, StringComparison.Ordinal))
            {
                await Console.Out.WriteLineAsync();
            }

            return;
        }

        await Console.Out.WriteLineAsync($"Wrote OpenCLI JSON to `{result.OutputFile}`.");
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
}
