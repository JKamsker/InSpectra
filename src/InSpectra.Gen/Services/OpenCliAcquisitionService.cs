using InSpectra.Gen.Runtime;

using InSpectra.Gen.Acquisition.Analysis;

namespace InSpectra.Gen.Services;

public sealed class OpenCliAcquisitionService(
    ExecutableResolver executableResolver,
    IProcessRunner processRunner,
    LocalCliTargetFactory localTargetFactory,
    PackageCliTargetFactory packageCliTargetFactory,
    DotnetBuildOutputResolver dotnetBuildOutputResolver,
    AcquisitionAnalyzerService acquisitionAnalyzerService)
    : IOpenCliAcquisitionService
{
    public async Task<OpenCliAcquisitionResult> AcquireFromExecAsync(
        ExecAcquisitionRequest request,
        CancellationToken cancellationToken)
    {
        using var workspace = new TemporaryWorkspace("inspectra-exec");
        var resolvedSource = executableResolver.Resolve(request.Source, request.WorkingDirectory);
        var target = localTargetFactory.Create(
            resolvedSource,
            request.SourceArguments,
            request.WorkingDirectory,
            Path.Combine(workspace.RootPath, "shim"),
            request.CommandName,
            request.CliFramework);

        return await AcquireFromTargetAsync(
            "exec",
            target.DisplayName,
            target.CommandPath,
            target,
            request.Mode,
            request.OpenCliArguments,
            request.IncludeXmlDoc,
            request.XmlDocArguments,
            request.TimeoutSeconds,
            request.Artifacts,
            new List<string>(),
            cancellationToken);
    }

    public async Task<OpenCliAcquisitionResult> AcquireFromPackageAsync(
        PackageAcquisitionRequest request,
        CancellationToken cancellationToken)
    {
        using var workspace = new TemporaryWorkspace("inspectra-package");
        var target = await packageCliTargetFactory.CreateAsync(
            request.PackageId,
            request.Version,
            request.CommandName,
            request.CliFramework,
            workspace.RootPath,
            request.TimeoutSeconds,
            cancellationToken);

        return await AcquireFromTargetAsync(
            "package",
            $"{request.PackageId}@{request.Version}",
            target.CommandPath,
            target,
            request.Mode,
            request.OpenCliArguments,
            request.IncludeXmlDoc,
            request.XmlDocArguments,
            request.TimeoutSeconds,
            request.Artifacts,
            new List<string>(),
            cancellationToken);
    }

    public async Task<OpenCliAcquisitionResult> AcquireFromDotnetAsync(
        DotnetAcquisitionRequest request,
        CancellationToken cancellationToken)
    {
        var warnings = new List<string>();
        var nativeArgs = DotnetProjectArgsBuilder.Build(
            request.ProjectPath,
            request.Configuration,
            request.Framework,
            request.LaunchProfile,
            request.NoBuild,
            request.NoRestore);

        if (request.Mode == OpenCliMode.Native)
        {
            var nativeResult = await RunNativeAsync(
                "dotnet",
                nativeArgs,
                request.OpenCliArguments,
                request.IncludeXmlDoc,
                request.XmlDocArguments,
                request.WorkingDirectory,
                environment: null,
                request.TimeoutSeconds,
                cancellationToken);
            return CreateResult(
                "dotnet",
                request.ProjectPath,
                "dotnet",
                AnalysisMode.Native,
                request.CommandName,
                request.CliFramework,
                nativeResult.OpenCliJson,
                nativeResult.XmlDocument,
                crawlJson: null,
                request.Artifacts,
                [new OpenCliAcquisitionAttempt(AnalysisMode.Native, request.CliFramework, AnalysisDisposition.Success)],
                warnings);
        }

        List<OpenCliAcquisitionAttempt>? initialAttempts = null;
        if (request.Mode == OpenCliMode.Auto)
        {
            initialAttempts = [];
            var nativeOutcome = await TryAcquireNativeAsync(
                "dotnet",
                request.ProjectPath,
                "dotnet",
                nativeArgs,
                request.OpenCliArguments,
                request.IncludeXmlDoc,
                request.XmlDocArguments,
                request.WorkingDirectory,
                environment: null,
                request.TimeoutSeconds,
                request.Artifacts,
                request.CommandName,
                request.CliFramework,
                initialAttempts,
                warnings,
                cancellationToken);
            if (nativeOutcome is not null)
            {
                return nativeOutcome;
            }
        }

        using var workspace = new TemporaryWorkspace("inspectra-dotnet");
        var buildOutput = await dotnetBuildOutputResolver.ResolveAsync(
            request.ProjectPath,
            request.Configuration,
            request.Framework,
            request.LaunchProfile,
            request.NoBuild,
            request.NoRestore,
            request.WorkingDirectory,
            request.TimeoutSeconds,
            cancellationToken);
        warnings.AddRange(buildOutput.Warnings);

        var target = localTargetFactory.Create(
            buildOutput.TargetPath,
            [],
            request.WorkingDirectory,
            Path.Combine(workspace.RootPath, "shim"),
            request.CommandName,
            request.CliFramework);

        return await AcquireFromTargetAsync(
            "dotnet",
            request.ProjectPath,
            target.CommandPath,
            target,
            request.Mode,
            request.OpenCliArguments,
            request.IncludeXmlDoc,
            request.XmlDocArguments,
            request.TimeoutSeconds,
            request.Artifacts,
            warnings,
            cancellationToken,
            initialAttempts);
    }

    private async Task<OpenCliAcquisitionResult> AcquireFromTargetAsync(
        string kind,
        string sourceLabel,
        string executablePath,
        MaterializedCliTarget target,
        OpenCliMode mode,
        IReadOnlyList<string> openCliArguments,
        bool includeXmlDoc,
        IReadOnlyList<string> xmlDocArguments,
        int timeoutSeconds,
        OpenCliArtifactOptions artifacts,
        List<string> warnings,
        CancellationToken cancellationToken,
        List<OpenCliAcquisitionAttempt>? attempts = null)
    {
        attempts ??= [];
        if (mode == OpenCliMode.Native)
        {
            return await AcquireNativeAsync(
                kind,
                sourceLabel,
                executablePath,
                [],
                openCliArguments,
                includeXmlDoc,
                xmlDocArguments,
                target.WorkingDirectory,
                target.Environment,
                timeoutSeconds,
                artifacts,
                target.CommandName,
                target.CliFramework,
                warnings,
                cancellationToken);
        }

        if (mode == OpenCliMode.Auto)
        {
            var nativeOutcome = await TryAcquireNativeAsync(
                kind,
                sourceLabel,
                executablePath,
                [],
                openCliArguments,
                includeXmlDoc,
                xmlDocArguments,
                target.WorkingDirectory,
                target.Environment,
                timeoutSeconds,
                artifacts,
                target.CommandName,
                target.CliFramework,
                attempts,
                warnings,
                cancellationToken);
            if (nativeOutcome is not null)
            {
                return nativeOutcome;
            }
        }

        var plannedAttempts = mode == OpenCliMode.Auto
            ? OpenCliModePlanner.BuildAutoPlan(target.CliFramework, target.HookCliFramework)
            : [new OpenCliAcquisitionAttempt(
                OpenCliModePlanner.ToModeValue(mode),
                mode == OpenCliMode.Hook ? target.HookCliFramework ?? target.CliFramework : target.CliFramework,
                AnalysisDisposition.Planned)];
        var failureDetails = new List<string>();

        foreach (var plannedAttempt in plannedAttempts)
        {
            var analysisMode = ParseMode(plannedAttempt.Mode);
            var outcome = await acquisitionAnalyzerService.TryAnalyzeAsync(
                target,
                analysisMode,
                plannedAttempt.Framework,
                timeoutSeconds,
                cancellationToken);
            attempts.Add(new OpenCliAcquisitionAttempt(
                plannedAttempt.Mode,
                plannedAttempt.Framework,
                outcome.Success ? AnalysisDisposition.Success : AnalysisDisposition.Failed,
                outcome.FailureMessage));
            if (!outcome.Success)
            {
                failureDetails.Add($"{plannedAttempt.Mode}: {outcome.FailureMessage ?? outcome.FailureClassification ?? AnalysisDisposition.Failed}");
                continue;
            }

            var xmlDocument = includeXmlDoc
                ? await RunXmlDocAsync(target.CommandPath, xmlDocArguments, target.WorkingDirectory, target.Environment, timeoutSeconds, cancellationToken)
                : null;
            return CreateResult(
                kind,
                sourceLabel,
                executablePath,
                outcome.Mode,
                target.CommandName,
                outcome.Framework ?? target.CliFramework,
                outcome.OpenCliJson!,
                xmlDocument,
                outcome.CrawlJson,
                artifacts,
                attempts,
                warnings);
        }

        throw new CliSourceExecutionException(
            "No OpenCLI acquisition mode succeeded.",
            details: failureDetails);
    }

    private async Task<OpenCliAcquisitionResult?> TryAcquireNativeAsync(
        string kind,
        string sourceLabel,
        string executablePath,
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
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        try
        {
            var nativeResult = await RunNativeAsync(
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
            return CreateResult(
                kind,
                sourceLabel,
                executablePath,
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

    private async Task<OpenCliAcquisitionResult> AcquireNativeAsync(
        string kind,
        string sourceLabel,
        string executablePath,
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
        var nativeResult = await RunNativeAsync(
            executablePath,
            sourceArguments,
            openCliArguments,
            includeXmlDoc,
            xmlDocArguments,
            workingDirectory,
            environment,
            timeoutSeconds,
            cancellationToken);

        return CreateResult(
            kind,
            sourceLabel,
            executablePath,
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

    private async Task<(string OpenCliJson, string? XmlDocument)> RunNativeAsync(
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
            ? await RunXmlDocAsync(executablePath, sourceArguments.Concat(xmlDocArguments).ToArray(), workingDirectory, environment, timeoutSeconds, cancellationToken)
            : null;
        return (OpenCliJsonSanitizer.Sanitize(openCliResult.StandardOutput), xmlDocument);
    }

    private async Task<string> RunXmlDocAsync(
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

    private static OpenCliAcquisitionResult CreateResult(
        string kind,
        string sourceLabel,
        string executablePath,
        string selectedMode,
        string? commandName,
        string? cliFramework,
        string openCliJson,
        string? xmlDocument,
        string? crawlJson,
        OpenCliArtifactOptions requestedArtifacts,
        IReadOnlyList<OpenCliAcquisitionAttempt> attempts,
        IReadOnlyList<string> warnings)
    {
        var allWarnings = warnings.ToList();
        if (!string.IsNullOrWhiteSpace(requestedArtifacts.CrawlOutputPath) && string.IsNullOrWhiteSpace(crawlJson))
        {
            allWarnings.Add("`--crawl-out` was requested, but the selected acquisition mode did not produce crawl data.");
        }

        var writtenArtifacts = OpenCliArtifactWriter.WriteArtifacts(requestedArtifacts, openCliJson, crawlJson);
        return new OpenCliAcquisitionResult(
            openCliJson,
            xmlDocument,
            crawlJson,
            new RenderSourceInfo(kind, sourceLabel, xmlDocument is null ? null : sourceLabel, executablePath),
            new OpenCliAcquisitionMetadata(selectedMode, commandName, cliFramework, attempts, writtenArtifacts.OpenCliOutputPath, writtenArtifacts.CrawlOutputPath),
            allWarnings);
    }

    private static OpenCliMode ParseMode(string value)
        => value switch
        {
            AnalysisMode.Help => OpenCliMode.Help,
            AnalysisMode.CliFx => OpenCliMode.CliFx,
            AnalysisMode.Static => OpenCliMode.Static,
            AnalysisMode.Hook => OpenCliMode.Hook,
            _ => throw new InvalidOperationException($"Unsupported acquisition mode `{value}`."),
        };
}
