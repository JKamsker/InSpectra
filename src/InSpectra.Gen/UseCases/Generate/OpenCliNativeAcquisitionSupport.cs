using InSpectra.Gen.Acquisition.Contracts;
using InSpectra.Gen.Acquisition.Contracts.Exceptions;
using InSpectra.Gen.UseCases.Generate.Requests;

namespace InSpectra.Gen.UseCases.Generate;

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
            return OpenCliAcquisitionResultFactory.Create(
                context,
                AnalysisMode.Native,
                nativeResult.OpenCliJson,
                nativeResult.XmlDocument,
                crawlJson: null,
                context.CliFramework,
                completedAttempts,
                warnings);
        }
        catch (CliException exception)
        {
            attempts.Add(new OpenCliAcquisitionAttempt(AnalysisMode.Native, context.CliFramework, AnalysisDisposition.Failed, exception.Message));
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

        return OpenCliAcquisitionResultFactory.Create(
            context,
            AnalysisMode.Native,
            nativeResult.OpenCliJson,
            nativeResult.XmlDocument,
            crawlJson: null,
            context.CliFramework,
            [new OpenCliAcquisitionAttempt(AnalysisMode.Native, context.CliFramework, AnalysisDisposition.Success)],
            warnings);
    }

    public async Task<string> RunXmlDocAsync(
        string executablePath,
        IReadOnlyList<string> xmlDocArguments,
        string workingDirectory,
        IReadOnlyDictionary<string, string>? environment,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        var xmlResult = await processRunner.RunAsync(
            executablePath,
            workingDirectory,
            xmlDocArguments,
            timeoutSeconds,
            environment,
            cancellationToken);
        return xmlResult.StandardOutput;
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
            cancellationToken);
        var xmlDocument = process.IncludeXmlDoc
            ? await RunXmlDocAsync(
                process.ExecutablePath,
                process.SourceArguments.Concat(process.XmlDocArguments).ToArray(),
                process.WorkingDirectory,
                process.Environment,
                process.TimeoutSeconds,
                cancellationToken)
            : null;
        return (OpenCliJsonSanitizer.Sanitize(openCliResult.StandardOutput), xmlDocument);
    }
}
