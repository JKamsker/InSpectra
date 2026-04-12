namespace InSpectra.Lib.Tests.Live;

using InSpectra.Lib.Modes.Help.Crawling;
using InSpectra.Lib.Modes.Help.Projection;
using InSpectra.Lib.Modes.Hook.Execution;
using InSpectra.Lib.Tooling;
using InSpectra.Lib.Tooling.Paths;
using InSpectra.Lib.Tooling.Process;
using InSpectra.Lib.Tooling.Results;

using System.Text.Json;
using System.Text.Json.Nodes;

/// <summary>
/// Adapts <see cref="HookInstalledToolAnalysisSupport"/> and
/// <see cref="InstalledToolAnalyzer"/> back to the end-to-end shape that the legacy
/// discovery-repo <c>HookService.RunAsync</c> / <c>HelpService.RunAsync</c> entry points
/// exposed: install a tool from NuGet, run the requested analyzer, and land
/// <c>result.json</c> and <c>opencli.json</c> artifacts under <paramref name="outputRoot"/>.
/// Preserves the classification/disposition assertions the ported live tests rely on.
/// </summary>
internal static class LiveAnalyzerHarness
{
    private const string HookAnalysisMode = "hook";
    private const string HookBatchId = "live-hook";
    private const string HookSource = "live-hook-test";
    private const string HelpAnalysisMode = "help";
    private const string HelpBatchId = "live-help";
    private const string HelpSource = "live-help-test";

    private static readonly JsonSerializerOptions ResultJsonOptions = new()
    {
        WriteIndented = true,
    };

    /// <summary>
    /// Installs the requested NuGet tool into a sandbox under <paramref name="outputRoot"/>,
    /// drives hook-based analysis, serializes the resulting <c>result.json</c> next to the
    /// <c>opencli.json</c> artifact, and returns 0 on success.
    /// </summary>
    public static async Task<int> RunHookAsync(
        string packageId,
        string version,
        string commandName,
        string cliFramework,
        string outputRoot,
        int installTimeoutSeconds,
        int commandTimeoutSeconds,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(outputRoot);
        var tempRoot = Path.Combine(outputRoot, "workspace");
        Directory.CreateDirectory(tempRoot);

        var result = NonSpectreResultSupport.CreateInitialResult(
            packageId,
            version,
            commandName,
            batchId: HookBatchId,
            attempt: 1,
            source: HookSource,
            cliFramework: cliFramework,
            analysisMode: HookAnalysisMode,
            analyzedAt: DateTimeOffset.UtcNow);

        // Mirror AcquisitionAnalysisDispatcher: fetch nugetTitle/nugetDescription from the
        // catalog leaf so HookInstalledToolAnalysisSupport.ApplyNuGetMetadata can overlay
        // the NuGet package title/description onto the OpenCLI document — which matches
        // the behaviour the frozen snapshot fixtures were captured under.
        await TryAttachNuGetMetadataAsync(result, packageId, version, cancellationToken);

        try
        {
            var runtime = new CommandRuntime();
            var analyzer = new HookInstalledToolAnalysisSupport(runtime);
            await analyzer.AnalyzeAsync(
                result,
                packageId,
                version,
                commandName,
                outputDirectory: outputRoot,
                tempRoot: tempRoot,
                installTimeoutSeconds: installTimeoutSeconds,
                commandTimeoutSeconds: commandTimeoutSeconds,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            NonSpectreResultSupport.ApplyUnexpectedRetryableFailure(result, ex.ToString());
        }
        finally
        {
            NonSpectreResultSupport.FinalizeFailureSignature(result);
            WriteResult(outputRoot, result);
        }

        var disposition = result["disposition"]?.GetValue<string>();
        return string.Equals(disposition, "success", StringComparison.Ordinal) ? 0 : 1;
    }

    /// <summary>
    /// Installs the requested NuGet tool into a sandbox under <paramref name="outputRoot"/>,
    /// drives help-mode crawling analysis, serializes the resulting <c>result.json</c>
    /// next to the <c>opencli.json</c> artifact, and returns 0 on success.
    /// </summary>
    public static async Task<int> RunHelpAsync(
        string packageId,
        string version,
        string commandName,
        string cliFramework,
        string outputRoot,
        int installTimeoutSeconds,
        int commandTimeoutSeconds,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(outputRoot);
        var tempRoot = Path.Combine(outputRoot, "workspace");
        Directory.CreateDirectory(tempRoot);

        var result = NonSpectreResultSupport.CreateInitialResult(
            packageId,
            version,
            commandName,
            batchId: HelpBatchId,
            attempt: 1,
            source: HelpSource,
            cliFramework: cliFramework,
            analysisMode: HelpAnalysisMode,
            analyzedAt: DateTimeOffset.UtcNow);

        await TryAttachNuGetMetadataAsync(result, packageId, version, cancellationToken);

        try
        {
            var runtime = new CommandRuntime();
            var analyzer = new InstalledToolAnalyzer(runtime, new OpenCliBuilder());
            await analyzer.AnalyzeAsync(
                result,
                packageId,
                version,
                commandName,
                outputDirectory: outputRoot,
                tempRoot: tempRoot,
                installTimeoutSeconds: installTimeoutSeconds,
                commandTimeoutSeconds: commandTimeoutSeconds,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            NonSpectreResultSupport.ApplyUnexpectedRetryableFailure(result, ex.ToString());
        }
        finally
        {
            NonSpectreResultSupport.FinalizeFailureSignature(result);
            WriteResult(outputRoot, result);
        }

        var disposition = result["disposition"]?.GetValue<string>();
        return string.Equals(disposition, "success", StringComparison.Ordinal) ? 0 : 1;
    }

    private static async Task TryAttachNuGetMetadataAsync(
        JsonObject result,
        string packageId,
        string version,
        CancellationToken cancellationToken)
    {
        try
        {
            using var scope = ApplicationLifetime.CreateNuGetApiClientScope();
            var (_, catalogLeaf) = await PackageVersionResolver.ResolveAsync(
                scope.Client,
                packageId,
                version,
                cancellationToken);
            result["nugetTitle"] = catalogLeaf.Title;
            result["nugetDescription"] = catalogLeaf.Description;
        }
        catch
        {
            // Best-effort: if metadata fetching fails, continue with the in-tool title/description.
        }
    }

    private static void WriteResult(string outputRoot, JsonObject result)
    {
        var resultPath = Path.Combine(outputRoot, "result.json");
        File.WriteAllText(resultPath, result.ToJsonString(ResultJsonOptions));
    }
}
