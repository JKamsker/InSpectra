namespace InSpectra.Gen.Engine.Modes.Hook.Execution;

using InSpectra.Gen.Engine.Modes.Hook.Capture;
using InSpectra.Gen.Engine.Modes.Hook.Projection;
using InSpectra.Gen.Engine.Tooling.FrameworkDetection;
using InSpectra.Gen.Engine.Tooling.Paths;

using InSpectra.Gen.Engine.Tooling.DocumentPipeline.Documents;

using InSpectra.Gen.Engine.Tooling.Results;

using InSpectra.Gen.Engine.Tooling.Process;

using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Nodes;

internal sealed class HookInstalledToolAnalysisSupport
{
    internal const string ExpectedCliFrameworkEnvironmentVariableName = "INSPECTRA_EXPECTED_CLI_FRAMEWORK";
    internal const string PreferredFrameworkDirectoryEnvironmentVariableName = "INSPECTRA_PREFERRED_FRAMEWORK_DIRECTORY";
    internal const string GlobalizationInvariantEnvironmentVariableName = DotnetRuntimeCompatibilitySupport.GlobalizationInvariantEnvironmentVariableName;
    internal const string DotnetRollForwardEnvironmentVariableName = DotnetRuntimeCompatibilitySupport.DotnetRollForwardEnvironmentVariableName;
    internal const string DotnetRollForwardMajorValue = DotnetRuntimeCompatibilitySupport.DotnetRollForwardMajorValue;
    private readonly CommandRuntime _runtime;
    private readonly Func<string?> _hookDllPathResolver;

    public HookInstalledToolAnalysisSupport(CommandRuntime runtime)
        : this(runtime, ResolveHookDllPath)
    {
    }

    internal HookInstalledToolAnalysisSupport(CommandRuntime runtime, Func<string?> hookDllPathResolver)
    {
        _runtime = runtime;
        _hookDllPathResolver = hookDllPathResolver;
    }

    public async Task AnalyzeAsync(
        JsonObject result,
        string packageId,
        string version,
        string commandName,
        string outputDirectory,
        string tempRoot,
        int installTimeoutSeconds,
        int commandTimeoutSeconds,
        CancellationToken cancellationToken)
    {
        var installedTool = await CommandInstallationSupport.InstallToolAsync(
            _runtime,
            result,
            packageId,
            version,
            commandName,
            tempRoot,
            installTimeoutSeconds,
            cancellationToken);
        if (installedTool is null)
            return;

        await AnalyzeInstalledAsync(
            new InstalledToolAnalysisRequest(result, version, commandName, outputDirectory, installedTool, tempRoot, commandTimeoutSeconds),
            cancellationToken);
    }

    internal async Task AnalyzeInstalledAsync(
        InstalledToolAnalysisRequest request,
        CancellationToken cancellationToken)
    {
        // Resolve hook DLL path (deployed alongside the main tool assembly).
        var hookDllPath = _hookDllPathResolver();
        if (hookDllPath is null)
        {
            NonSpectreResultSupport.ApplyTerminalFailure(
                request.Result,
                phase: "hook-setup",
                classification: "hook-dll-missing",
                "Could not locate InSpectra.Gen.StartupHook.dll.");
            return;
        }

        // Prepare capture path and hook environment.
        var capturePath = Path.Combine(request.OutputDirectory, $"inspectra-capture.{Guid.NewGuid():N}.json");
        var hookEnvironment = new Dictionary<string, string>(request.InstalledTool.Environment, StringComparer.OrdinalIgnoreCase)
        {
            ["DOTNET_STARTUP_HOOKS"] = hookDllPath,
            ["INSPECTRA_CAPTURE_PATH"] = capturePath,
        };
        var expectedHookCliFramework = CliFrameworkProviderRegistry.ResolveHookAnalysisFramework(
            request.Result["cliFramework"]?.GetValue<string>());
        if (!string.IsNullOrWhiteSpace(expectedHookCliFramework))
        {
            hookEnvironment[ExpectedCliFrameworkEnvironmentVariableName] = expectedHookCliFramework;
        }

        // Execute the tool with the startup hook attached. The hook observes the command tree
        // while the target processes `--help`, then writes a capture file for OpenCLI generation.
        var hookStopwatch = Stopwatch.StartNew();
        var invocationResolution = HookToolProcessInvocationResolver.Resolve(
            request.InstalledTool.InstallDirectory,
            request.CommandName,
            request.InstalledTool.CommandPath,
            request.InstalledTool.PreferredEntryPointPath);
        if (invocationResolution.TerminalFailureClassification is not null)
        {
            NonSpectreResultSupport.ApplyTerminalFailure(
                request.Result,
                phase: "hook-setup",
                classification: invocationResolution.TerminalFailureClassification,
                invocationResolution.TerminalFailureMessage);
            return;
        }

        var invocation = invocationResolution.Invocation!;
        var preferredFrameworkDirectory = HookToolProcessInvocationResolver.TryResolvePreferredAssemblyDirectory(invocation);
        if (!string.IsNullOrWhiteSpace(preferredFrameworkDirectory))
        {
            hookEnvironment[PreferredFrameworkDirectoryEnvironmentVariableName] = preferredFrameworkDirectory;
        }

        var retryResult = await HookProcessRetrySupport.InvokeWithHelpFallbackAsync(
            invocation,
            hookEnvironment,
            capturePath,
            (candidateInvocation, effectiveEnvironment, token) => InvokeHookProcessAsync(
                candidateInvocation,
                request.WorkingDirectory,
                effectiveEnvironment,
                request.CommandTimeoutSeconds,
                token),
            cancellationToken);
        var processResult = retryResult.ProcessResult;
        var resolvedCapturePath = retryResult.CapturePath;
        hookStopwatch.Stop();

        request.Result["timings"]!.AsObject()["crawlMs"] = (int)Math.Round(hookStopwatch.Elapsed.TotalMilliseconds);

        // Read and validate the capture file.
        if (string.IsNullOrWhiteSpace(resolvedCapturePath) || !File.Exists(resolvedCapturePath))
        {
            if (TryApplyMissingSharedRuntimeFailure(request.Result, request.CommandName, processResult))
            {
                return;
            }

            NonSpectreResultSupport.ApplyRetryableFailure(
                request.Result,
                phase: "hook-capture",
                classification: "hook-no-capture-file",
                HookFailureMessageSupport.BuildMissingCaptureMessage(processResult));
            return;
        }

        var capture = HookCaptureDeserializer.Deserialize(resolvedCapturePath);
        if (capture is null)
        {
            NonSpectreResultSupport.ApplyRetryableFailure(
                request.Result,
                phase: "hook-capture",
                classification: "hook-capture-invalid",
                "Capture file could not be deserialized.");
            return;
        }

        if (capture.Status != "ok" || capture.Root is null)
        {
            var classification = $"hook-{capture.Status}";
            var failureMessage = capture.Error ?? "Hook capture did not produce an ok result.";
            if (string.Equals(capture.Status, "capture-version-mismatch", StringComparison.Ordinal))
            {
                NonSpectreResultSupport.ApplyTerminalFailure(
                    request.Result,
                    phase: "hook-capture",
                    classification: classification,
                    failureMessage);
            }
            else
            {
                NonSpectreResultSupport.ApplyRetryableFailure(
                    request.Result,
                    phase: "hook-capture",
                    classification: classification,
                    failureMessage);
            }

            return;
        }

        // Build OpenCLI document from captured command tree.
        var openCliDocument = HookOpenCliBuilder.Build(request.CommandName, request.Version, capture);

        if (!string.IsNullOrWhiteSpace(request.Result["cliFramework"]?.GetValue<string>()))
            openCliDocument["x-inspectra"]!.AsObject()["cliFramework"] = request.Result["cliFramework"]!.GetValue<string>();

        OpenCliDocumentSanitizer.ApplyNuGetMetadata(
            openCliDocument,
            request.Result["nugetTitle"]?.GetValue<string>(),
            request.Result["nugetDescription"]?.GetValue<string>());

        HookOpenCliValidationSupport.TryWriteValidatedArtifact(request.Result, request.OutputDirectory, openCliDocument);
    }

    private static string? ResolveHookDllPath()
    {
        var toolAssemblyPath = Assembly.GetExecutingAssembly().Location;
        var toolDirectory = Path.GetDirectoryName(toolAssemblyPath);
        if (toolDirectory is null)
            return null;

        var hookPath = Path.Combine(toolDirectory, "hooks", "InSpectra.Gen.StartupHook.dll");
        return File.Exists(hookPath) ? hookPath : null;
    }

    private Task<CommandRuntime.ProcessResult> InvokeHookProcessAsync(
        HookToolProcessInvocation invocation,
        string workingDirectory,
        IReadOnlyDictionary<string, string> environment,
        int timeoutSeconds,
        CancellationToken cancellationToken)
        => _runtime.InvokeProcessCaptureAsync(
            invocation.FilePath,
            invocation.ArgumentList,
            workingDirectory,
            environment,
            timeoutSeconds,
            workingDirectory,
            cancellationToken);

    private static bool TryApplyMissingSharedRuntimeFailure(
        JsonObject result,
        string commandName,
        CommandRuntime.ProcessResult processResult)
    {
        if (!DotnetRuntimeCompatibilitySupport.LooksLikeMissingSharedRuntime(processResult))
        {
            return false;
        }

        var runtimeIssue = DotnetRuntimeCompatibilitySupport.DetectMissingFramework(
            commandName,
            processResult.Stdout,
            processResult.Stderr);
        var blockedCommands = runtimeIssue is null
            ? [DotnetRuntimeCompatibilitySupport.ToDisplayCommand(commandName)]
            : new[] { runtimeIssue.Command };
        var requiredFrameworks = runtimeIssue?.Requirement is null
            ? []
            : new[] { runtimeIssue.Requirement };

        NonSpectreResultSupport.ApplyTerminalFailure(
            result,
            phase: "hook-capture",
            classification: "hook-runtime-blocked",
            DotnetRuntimeCompatibilitySupport.BuildMissingFrameworkFailureMessage(
                blockedCommands,
                requiredFrameworks));
        return true;
    }
}
