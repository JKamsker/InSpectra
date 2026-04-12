using InSpectra.Gen.Engine.Contracts;
using InSpectra.Gen.Core;
using InSpectra.Gen.Engine.Execution.Process;
using InSpectra.Gen.Engine.OpenCli.Validation;
using InSpectra.Gen.Engine.UseCases.Generate.Requests;

using System.Text.Json;

namespace InSpectra.Gen.Engine.UseCases.Generate;

internal sealed class OpenCliNativeAcquisitionSupport(IProcessRunner processRunner)
{
    public async Task<OpenCliAcquisitionResult?> TryAcquireAsync(
        AcquisitionResultContext context,
        NativeProcessOptions process,
        List<OpenCliAcquisitionAttempt> attempts,
        IReadOnlyList<string> warnings,
        CancellationToken cancellationToken)
    {
        try
        {
            var nativeResult = await RunAsync(process, cancellationToken);
            var completedAttempts = attempts
                .Concat([new OpenCliAcquisitionAttempt(AnalysisMode.Native, context.CliFramework, AnalysisDisposition.Success)])
                .ToArray();
            return await OpenCliAcquisitionResultFactory.CreateAsync(
                context,
                AnalysisMode.Native,
                nativeResult.OpenCliJson,
                nativeResult.XmlDocument,
                crawlJson: null,
                context.CliFramework,
                completedAttempts,
                warnings,
                cancellationToken);
        }
        catch (CliException exception)
        {
            attempts.Add(new OpenCliAcquisitionAttempt(
                AnalysisMode.Native,
                context.CliFramework,
                AnalysisDisposition.Failed,
                FormatAttemptDetail(exception)));
            return null;
        }
    }

    public async Task<OpenCliAcquisitionResult> AcquireAsync(
        AcquisitionResultContext context,
        NativeProcessOptions process,
        IReadOnlyList<string> warnings,
        CancellationToken cancellationToken)
    {
        var nativeResult = await RunAsync(process, cancellationToken);

        return await OpenCliAcquisitionResultFactory.CreateAsync(
            context,
            AnalysisMode.Native,
            nativeResult.OpenCliJson,
            nativeResult.XmlDocument,
            crawlJson: null,
            context.CliFramework,
            [new OpenCliAcquisitionAttempt(AnalysisMode.Native, context.CliFramework, AnalysisDisposition.Success)],
            warnings,
            cancellationToken);
    }

    public async Task<string> RunXmlDocAsync(
        string executablePath,
        IReadOnlyList<string> xmlDocArguments,
        string workingDirectory,
        IReadOnlyDictionary<string, string>? environment,
        string? cleanupRoot,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        var xmlResult = await processRunner.RunAsync(
            executablePath,
            workingDirectory,
            xmlDocArguments,
            timeoutSeconds,
            environment,
            cleanupRoot,
            cancellationToken);
        return xmlResult.StandardOutput;
    }

    private static string FormatAttemptDetail(CliException exception)
    {
        var detailLines = new List<string> { exception.Message };
        detailLines.AddRange(exception.Details.Where(detail => !string.IsNullOrWhiteSpace(detail)));
        return string.Join(Environment.NewLine, detailLines);
    }

    private async Task<(string OpenCliJson, string? XmlDocument)> RunAsync(
        NativeProcessOptions process,
        CancellationToken cancellationToken)
    {
        var openCliResult = await processRunner.RunAsync(
            process.ExecutablePath,
            process.WorkingDirectory,
            process.SourceArguments.Concat(process.OpenCliArguments).ToArray(),
            process.TimeoutSeconds,
            process.Environment,
            process.CleanupRoot,
            cancellationToken);
        string sanitizedOpenCliJson;
        try
        {
            sanitizedOpenCliJson = OpenCliJsonSanitizer.Sanitize(openCliResult.StandardOutput);
        }
        catch (JsonException exception)
        {
            throw new CliSourceExecutionException(
                "Native analysis produced an invalid OpenCLI artifact.",
                details:
                [
                    "Classification: invalid-success-artifact",
                    $"Parser error: {exception.Message}",
                    $"Standard output:{Environment.NewLine}{openCliResult.StandardOutput}",
                ],
                innerException: exception);
        }

        var xmlDocument = process.IncludeXmlDoc
            ? await RunXmlDocAsync(
                process.ExecutablePath,
                process.SourceArguments.Concat(process.XmlDocArguments).ToArray(),
                process.WorkingDirectory,
                process.Environment,
                process.CleanupRoot,
                process.TimeoutSeconds,
                cancellationToken)
            : null;
        return (sanitizedOpenCliJson, xmlDocument);
    }
}
