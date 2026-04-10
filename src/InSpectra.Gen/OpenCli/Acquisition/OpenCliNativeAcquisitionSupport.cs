using InSpectra.Gen.Acquisition.Analysis;
using InSpectra.Gen.Acquisition.Runtime;
using InSpectra.Gen.Runtime.Acquisition;

namespace InSpectra.Gen.OpenCli.Acquisition;

internal sealed class OpenCliNativeAcquisitionSupport(IProcessRunner processRunner)
{
    public async Task<OpenCliAcquisitionResult?> TryAcquireAsync(
        string kind,
        string sourceLabel,
        string executablePath,
        string? reportedExecutablePath,
        IReadOnlyList<string> sourceArguments,
        IReadOnlyList<string> openCliArguments,
        bool includeXmlDoc,
        IReadOnlyList<string> xmlDocArguments,
        string workingDirectory,
        IReadOnlyDictionary<string, string>? environment,
        int timeoutSeconds,
        OpenCliArtifactOptions artifacts,
        string? commandName,
        string? cliFramework,
        List<OpenCliAcquisitionAttempt> attempts,
        IReadOnlyList<string> warnings,
        CancellationToken cancellationToken)
    {
        try
        {
            var nativeResult = await RunAsync(
                executablePath,
                sourceArguments,
                openCliArguments,
                includeXmlDoc,
                xmlDocArguments,
                workingDirectory,
                environment,
                timeoutSeconds,
                cancellationToken);
            var completedAttempts = attempts
                .Concat([new OpenCliAcquisitionAttempt(AnalysisMode.Native, cliFramework, AnalysisDisposition.Success)])
                .ToArray();
            return OpenCliAcquisitionResultFactory.Create(
                kind,
                sourceLabel,
                reportedExecutablePath,
                AnalysisMode.Native,
                commandName,
                cliFramework,
                nativeResult.OpenCliJson,
                nativeResult.XmlDocument,
                crawlJson: null,
                artifacts,
                completedAttempts,
                warnings);
        }
        catch (CliException exception)
        {
            attempts.Add(new OpenCliAcquisitionAttempt(AnalysisMode.Native, cliFramework, AnalysisDisposition.Failed, exception.Message));
            return null;
        }
    }

    public async Task<OpenCliAcquisitionResult> AcquireAsync(
        string kind,
        string sourceLabel,
        string executablePath,
        string? reportedExecutablePath,
        IReadOnlyList<string> sourceArguments,
        IReadOnlyList<string> openCliArguments,
        bool includeXmlDoc,
        IReadOnlyList<string> xmlDocArguments,
        string workingDirectory,
        IReadOnlyDictionary<string, string>? environment,
        int timeoutSeconds,
        OpenCliArtifactOptions artifacts,
        string? commandName,
        string? cliFramework,
        IReadOnlyList<string> warnings,
        CancellationToken cancellationToken)
    {
        var nativeResult = await RunAsync(
            executablePath,
            sourceArguments,
            openCliArguments,
            includeXmlDoc,
            xmlDocArguments,
            workingDirectory,
            environment,
            timeoutSeconds,
            cancellationToken);

        return OpenCliAcquisitionResultFactory.Create(
            kind,
            sourceLabel,
            reportedExecutablePath,
            AnalysisMode.Native,
            commandName,
            cliFramework,
            nativeResult.OpenCliJson,
            nativeResult.XmlDocument,
            crawlJson: null,
            artifacts,
            [new OpenCliAcquisitionAttempt(AnalysisMode.Native, cliFramework, AnalysisDisposition.Success)],
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
        string executablePath,
        IReadOnlyList<string> sourceArguments,
        IReadOnlyList<string> openCliArguments,
        bool includeXmlDoc,
        IReadOnlyList<string> xmlDocArguments,
        string workingDirectory,
        IReadOnlyDictionary<string, string>? environment,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        var openCliResult = await processRunner.RunAsync(
            executablePath,
            workingDirectory,
            sourceArguments.Concat(openCliArguments).ToArray(),
            timeoutSeconds,
            environment,
            cancellationToken);
        var xmlDocument = includeXmlDoc
            ? await RunXmlDocAsync(
                executablePath,
                sourceArguments.Concat(xmlDocArguments).ToArray(),
                workingDirectory,
                environment,
                timeoutSeconds,
                cancellationToken)
            : null;
        return (OpenCliJsonSanitizer.Sanitize(openCliResult.StandardOutput), xmlDocument);
    }
}
