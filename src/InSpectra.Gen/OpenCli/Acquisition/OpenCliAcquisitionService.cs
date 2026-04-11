using InSpectra.Gen.Acquisition.Contracts;
using InSpectra.Gen.Acquisition.Contracts.Exceptions;
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
            request.Options.CommandName,
            request.Options.CliFramework);
        return await AcquireFromTargetAsync(
            "exec",
            target.DisplayName,
            resolvedSource,
            target,
            request.Options,
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
            request.Options.CommandName,
            request.Options.CliFramework,
            workspace.RootPath,
            request.Options.TimeoutSeconds,
            cancellationToken);
        return await AcquireFromTargetAsync(
            "package",
            $"{request.PackageId}@{request.Version}",
            executablePath: null,
            target,
            request.Options,
            new List<string>(),
            cancellationToken);
    }

    public async Task<OpenCliAcquisitionResult> AcquireFromDotnetAsync(
        DotnetAcquisitionRequest request,
        CancellationToken cancellationToken)
    {
        var options = request.Options;
        var warnings = new List<string>();
        var nativeArgs = DotnetProjectArgsBuilder.Build(request.Build);
        var resultContext = new AcquisitionResultContext(
            "dotnet",
            request.Build.ProjectPath,
            null,
            options.CommandName,
            options.CliFramework,
            options.Artifacts);
        var processOptions = new NativeProcessOptions(
            "dotnet",
            nativeArgs,
            options.OpenCliArguments,
            options.IncludeXmlDoc,
            options.XmlDocArguments,
            request.WorkingDirectory,
            null,
            options.TimeoutSeconds);

        if (options.Mode == OpenCliMode.Native)
        {
            return await nativeAcquisitionSupport.AcquireAsync(
                resultContext,
                processOptions,
                warnings,
                cancellationToken);
        }

        List<OpenCliAcquisitionAttempt>? initialAttempts = null;
        if (options.Mode == OpenCliMode.Auto)
        {
            initialAttempts = [];
            var nativeOutcome = await nativeAcquisitionSupport.TryAcquireAsync(
                resultContext,
                processOptions,
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
            request.Build,
            request.WorkingDirectory,
            options.TimeoutSeconds,
            cancellationToken);
        warnings.AddRange(buildOutput.Warnings);
        var target = localTargetFactory.Create(
            buildOutput.TargetPath,
            [],
            request.WorkingDirectory,
            Path.Combine(workspace.RootPath, "shim"),
            options.CommandName,
            options.CliFramework);

        return await AcquireFromTargetAsync(
            "dotnet",
            request.Build.ProjectPath,
            buildOutput.TargetPath,
            target,
            options,
            warnings,
            cancellationToken,
            initialAttempts);
    }

    private async Task<OpenCliAcquisitionResult> AcquireFromTargetAsync(
        string kind,
        string sourceLabel,
        string? executablePath,
        MaterializedCliTarget target,
        AcquisitionOptions options,
        List<string> warnings,
        CancellationToken cancellationToken,
        List<OpenCliAcquisitionAttempt>? attempts = null)
    {
        attempts ??= [];
        var resultContext = new AcquisitionResultContext(
            kind,
            sourceLabel,
            executablePath,
            target.CommandName,
            target.CliFramework,
            options.Artifacts);
        var processOptions = new NativeProcessOptions(
            target.CommandPath,
            [],
            options.OpenCliArguments,
            options.IncludeXmlDoc,
            options.XmlDocArguments,
            target.WorkingDirectory,
            target.Environment,
            options.TimeoutSeconds);

        if (options.Mode == OpenCliMode.Native)
        {
            return await nativeAcquisitionSupport.AcquireAsync(
                resultContext,
                processOptions,
                warnings,
                cancellationToken);
        }

        if (options.Mode == OpenCliMode.Auto)
        {
            var nativeOutcome = await nativeAcquisitionSupport.TryAcquireAsync(
                resultContext,
                processOptions,
                attempts,
                warnings,
                cancellationToken);
            if (nativeOutcome is not null)
            {
                return nativeOutcome;
            }
        }

        var plannedAttempts = options.Mode == OpenCliMode.Auto
            ? OpenCliModePlanner.BuildAutoPlan(target.CliFramework, target.HookCliFramework)
            : [new OpenCliAcquisitionAttempt(
                OpenCliModePlanner.ToModeValue(options.Mode),
                options.Mode == OpenCliMode.Hook ? target.HookCliFramework ?? target.CliFramework : target.CliFramework,
                AnalysisDisposition.Planned)];
        var failureDetails = new List<string>();

        foreach (var plannedAttempt in plannedAttempts)
        {
            var analysisMode = ParseMode(plannedAttempt.Mode);
            var outcome = await acquisitionAnalyzerService.TryAnalyzeAsync(
                target,
                analysisMode,
                plannedAttempt.Framework,
                options.TimeoutSeconds,
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

            var xmlDocument = options.IncludeXmlDoc
                ? await nativeAcquisitionSupport.RunXmlDocAsync(
                    target.CommandPath,
                    options.XmlDocArguments,
                    target.WorkingDirectory,
                    target.Environment,
                    options.TimeoutSeconds,
                    cancellationToken)
                : null;
            return OpenCliAcquisitionResultFactory.Create(
                resultContext,
                outcome.Mode,
                outcome.OpenCliJson!,
                xmlDocument,
                outcome.CrawlJson,
                outcome.Framework ?? target.CliFramework,
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
