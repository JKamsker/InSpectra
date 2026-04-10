using InSpectra.Gen.Acquisition.Analysis;
using InSpectra.Gen.Acquisition.Runtime;
using InSpectra.Gen.Runtime.Acquisition;

namespace InSpectra.Gen.OpenCli.Acquisition;

internal sealed class OpenCliAcquisitionService(
    ExecutableResolver executableResolver,
    OpenCliNativeAcquisitionSupport nativeAcquisitionSupport,
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
            resolvedSource,
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
            executablePath: null,
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
            return await nativeAcquisitionSupport.AcquireAsync(
                "dotnet",
                request.ProjectPath,
                "dotnet",
                reportedExecutablePath: null,
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
                warnings,
                cancellationToken);
        }

        List<OpenCliAcquisitionAttempt>? initialAttempts = null;
        if (request.Mode == OpenCliMode.Auto)
        {
            initialAttempts = [];
            var nativeOutcome = await nativeAcquisitionSupport.TryAcquireAsync(
                "dotnet",
                request.ProjectPath,
                "dotnet",
                reportedExecutablePath: null,
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
            buildOutput.TargetPath,
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
        string? executablePath,
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
            return await nativeAcquisitionSupport.AcquireAsync(
                kind,
                sourceLabel,
                target.CommandPath,
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
            var nativeOutcome = await nativeAcquisitionSupport.TryAcquireAsync(
                kind,
                sourceLabel,
                target.CommandPath,
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
                ? await nativeAcquisitionSupport.RunXmlDocAsync(
                    target.CommandPath,
                    xmlDocArguments,
                    target.WorkingDirectory,
                    target.Environment,
                    timeoutSeconds,
                    cancellationToken)
                : null;
            return OpenCliAcquisitionResultFactory.Create(
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
