using InSpectra.Gen.Engine.Contracts;
using InSpectra.Gen.Core;
using InSpectra.Gen.Engine.Contracts.Providers;
using InSpectra.Gen.Engine.Execution.Process;
using InSpectra.Gen.Engine.Execution.Workspace;
using InSpectra.Gen.Engine.Orchestration;
using InSpectra.Gen.Engine.Targets.Inputs;
using InSpectra.Gen.Engine.Targets.Sources;
using InSpectra.Gen.Engine.UseCases.Generate.Requests;

namespace InSpectra.Gen.Engine.UseCases.Generate;

internal sealed class OpenCliAcquisitionService(
    ExecutableResolver executableResolver,
    OpenCliNativeAcquisitionSupport nativeAcquisitionSupport,
    LocalCliTargetFactory localTargetFactory,
    PackageCliTargetFactory packageCliTargetFactory,
    DotnetBuildOutputResolver dotnetBuildOutputResolver,
    ICliFrameworkCatalog cliFrameworkCatalog,
    IAcquisitionAnalysisDispatcherInternal acquisitionAnalysisDispatcher)
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
            target.CleanupRoot,
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
            ? OpenCliModePlanner.BuildAutoPlan(cliFrameworkCatalog, target.CliFramework, target.HookCliFramework)
            : [new OpenCliAcquisitionAttempt(
                OpenCliModePlanner.ToModeValue(options.Mode),
                options.Mode == OpenCliMode.Hook ? target.HookCliFramework ?? target.CliFramework : target.CliFramework,
                AnalysisDisposition.Planned)];
        var targetDescriptor = ToTargetDescriptor(target);

        foreach (var plannedAttempt in plannedAttempts)
        {
            var outcome = await acquisitionAnalysisDispatcher.TryAnalyzeAsync(
                targetDescriptor,
                target.CleanupRoot,
                plannedAttempt.Mode,
                plannedAttempt.Framework,
                options.TimeoutSeconds,
                cancellationToken);
            attempts.Add(new OpenCliAcquisitionAttempt(
                plannedAttempt.Mode,
                plannedAttempt.Framework,
                outcome.Success ? AnalysisDisposition.Success : AnalysisDisposition.Failed,
                outcome.FailureMessage ?? outcome.FailureClassification ?? AnalysisDisposition.Failed));
            if (!outcome.Success)
            {
                continue;
            }

            var xmlDocument = options.IncludeXmlDoc
                ? await nativeAcquisitionSupport.RunXmlDocAsync(
                    target.CommandPath,
                    options.XmlDocArguments,
                    target.WorkingDirectory,
                    target.Environment,
                    target.CleanupRoot,
                    options.TimeoutSeconds,
                    cancellationToken)
                : null;
            return await OpenCliAcquisitionResultFactory.CreateAsync(
                resultContext,
                outcome.Mode,
                outcome.OpenCliJson!,
                xmlDocument,
                outcome.CrawlJson,
                outcome.Framework ?? target.CliFramework,
                attempts,
                warnings,
                cancellationToken);
        }

        throw new CliSourceExecutionException(
            "No OpenCLI acquisition mode succeeded.",
            details: BuildFailureDetails(attempts));
    }

    private static IReadOnlyList<string> BuildFailureDetails(IEnumerable<OpenCliAcquisitionAttempt> attempts)
        => attempts
            .Where(attempt => attempt.Outcome == AnalysisDisposition.Failed)
            .Select(attempt => $"{attempt.Mode}: {attempt.Detail ?? AnalysisDisposition.Failed}")
            .ToArray();

    private static CliTargetDescriptor ToTargetDescriptor(MaterializedCliTarget target)
        => new(
            DisplayName: target.DisplayName,
            CommandPath: target.CommandPath,
            CommandName: target.CommandName,
            WorkingDirectory: target.WorkingDirectory,
            InstallDirectory: target.InstallDirectory,
            PreferredEntryPointPath: target.PreferredEntryPointPath,
            Version: target.Version,
            Environment: target.Environment,
            CliFramework: target.CliFramework,
            HookCliFramework: target.HookCliFramework,
            PackageTitle: target.PackageTitle,
            PackageDescription: target.PackageDescription);
}
