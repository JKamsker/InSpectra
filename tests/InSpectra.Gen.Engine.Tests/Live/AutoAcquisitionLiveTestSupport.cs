namespace InSpectra.Gen.Engine.Tests.Live;

using InSpectra.Gen.Engine.Composition;
using InSpectra.Gen.Engine.Contracts;
using InSpectra.Gen.Engine.Contracts.Providers;
using InSpectra.Gen.Engine.Execution.Workspace;
using InSpectra.Gen.Engine.Targets.Sources;
using InSpectra.Gen.Engine.Tooling;
using InSpectra.Gen.Engine.Tooling.Tools;
using InSpectra.Gen.Engine.UseCases.Generate;
using InSpectra.Gen.Engine.UseCases.Generate.Requests;

using Microsoft.Extensions.DependencyInjection;

using System.Text.Json.Nodes;

internal static class AutoAcquisitionLiveTestSupport
{
    private static readonly Lazy<ServiceProvider> ServiceProvider = new(CreateServiceProvider);

    public static async Task<AutoAcquisitionReport> RunAsync(
        string packageId,
        string version,
        string commandName,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        ApplicationLifetime.Initialize();

        using var dotnetRootOverride = HookLiveTestSupport.UseOptionalDotnetRootOverride();
        using var workspace = new TemporaryWorkspace("inspectra-live-auto");

        var provider = ServiceProvider.Value;
        var resolver = provider.GetRequiredService<IToolDescriptorResolver>();
        var catalog = provider.GetRequiredService<ICliFrameworkCatalog>();
        var targetFactory = provider.GetRequiredService<PackageCliTargetFactory>();
        var nativeSupport = provider.GetRequiredService<OpenCliNativeAcquisitionSupport>();
        var dispatcher = provider.GetRequiredService<IAcquisitionAnalysisDispatcherInternal>();

        var descriptorResolution = await resolver.ResolveAsync(packageId, version, commandName, cancellationToken);
        var target = await targetFactory.CreateAsync(
            packageId,
            version,
            commandName,
            cliFramework: null,
            workspace.RootPath,
            timeoutSeconds,
            cancellationToken);

        var attempts = new List<AutoAcquisitionAttemptReport>();
        var context = new AcquisitionResultContext(
            "package",
            $"{packageId}@{version}",
            null,
            target.CommandName,
            target.CliFramework,
            new OpenCliArtifactOptions(null, null, Overwrite: false));
        var processOptions = new NativeProcessOptions(
            target.CommandPath,
            [],
            OpenCliExportCommandDefaults.OpenCliArguments,
            IncludeXmlDoc: false,
            OpenCliExportCommandDefaults.XmlDocArguments,
            target.WorkingDirectory,
            target.Environment,
            target.CleanupRoot,
            timeoutSeconds);

        var nativeAttempts = new List<OpenCliAcquisitionAttempt>();
        var nativeOutcome = await nativeSupport.TryAcquireAsync(
            context,
            processOptions,
            nativeAttempts,
            warnings: [],
            cancellationToken);
        attempts.AddRange(nativeAttempts.Select(MapNativeAttempt));

        if (nativeOutcome is not null)
        {
            return AutoAcquisitionReport.CreateSuccess(
                descriptorResolution.Descriptor,
                attempts,
                nativeOutcome.Metadata.SelectedMode,
                nativeOutcome.Metadata.CliFramework,
                JsonNode.Parse(nativeOutcome.OpenCliJson),
                nativeOutcome.CrawlJson);
        }

        var plannedAttempts = OpenCliModePlanner.BuildAutoPlan(
            catalog,
            target.CliFramework,
            target.HookCliFramework);
        var targetDescriptor = ToTargetDescriptor(target);

        foreach (var plannedAttempt in plannedAttempts)
        {
            var outcome = await dispatcher.TryAnalyzeAsync(
                targetDescriptor,
                target.CleanupRoot,
                plannedAttempt.Mode,
                plannedAttempt.Framework,
                timeoutSeconds,
                cancellationToken);
            if (!outcome.Success)
            {
                attempts.Add(new AutoAcquisitionAttemptReport(
                    plannedAttempt.Mode,
                    plannedAttempt.Framework,
                    AnalysisDisposition.Failed,
                    outcome.FailureClassification,
                    outcome.FailureMessage,
                    ArtifactSource: null));
                continue;
            }

            attempts.Add(new AutoAcquisitionAttemptReport(
                outcome.Mode,
                outcome.Framework,
                AnalysisDisposition.Success,
                Classification: GetSuccessClassification(outcome.Mode),
                Message: null,
                ArtifactSource: GetArtifactSource(outcome.Mode)));
            return AutoAcquisitionReport.CreateSuccess(
                descriptorResolution.Descriptor,
                attempts,
                outcome.Mode,
                outcome.Framework,
                JsonNode.Parse(outcome.OpenCliJson!),
                outcome.CrawlJson);
        }

        return AutoAcquisitionReport.CreateFailure(descriptorResolution.Descriptor, attempts);
    }

    public static void AssertExpectedCommands(JsonNode? node, IReadOnlyList<string> expectedCommands)
    {
        foreach (var expectedCommand in expectedCommands)
        {
            Assert.True(ContainsCommand(node, expectedCommand), $"Expected command '{expectedCommand}'.");
        }
    }

    public static void AssertExpectedOptions(JsonNode? node, IReadOnlyList<string> expectedOptions)
    {
        foreach (var expectedOption in expectedOptions)
        {
            Assert.True(ContainsOption(node, expectedOption), $"Expected option '{expectedOption}'.");
        }
    }

    public static void AssertExpectedArguments(JsonNode? node, IReadOnlyList<string> expectedArguments)
    {
        foreach (var expectedArgument in expectedArguments)
        {
            Assert.True(ContainsArgument(node, expectedArgument), $"Expected argument '{expectedArgument}'.");
        }
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddInSpectraEngine();
        return services.BuildServiceProvider();
    }

    private static AutoAcquisitionAttemptReport MapNativeAttempt(OpenCliAcquisitionAttempt attempt)
        => new(
            attempt.Mode,
            attempt.Framework,
            attempt.Outcome,
            Classification: attempt.Outcome == AnalysisDisposition.Success ? "native" : null,
            Message: attempt.Detail,
            ArtifactSource: attempt.Outcome == AnalysisDisposition.Success ? "tool-output" : null);

    private static string GetSuccessClassification(string mode)
        => mode switch
        {
            AnalysisMode.CliFx => "clifx-crawl",
            AnalysisMode.Help => "help-crawl",
            AnalysisMode.Hook => "startup-hook",
            AnalysisMode.Static => "static-crawl",
            _ => "native",
        };

    private static string? GetArtifactSource(string mode)
        => mode switch
        {
            AnalysisMode.CliFx => "crawled-from-clifx-help",
            AnalysisMode.Help => "crawled-from-help",
            AnalysisMode.Hook => "startup-hook",
            AnalysisMode.Static => "static-analysis",
            _ => "tool-output",
        };

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

    private static bool ContainsCommand(JsonNode? node, string expectedName)
        => node?["commands"]?.AsArray().Any(command =>
            string.Equals(command?["name"]?.GetValue<string>(), expectedName, StringComparison.OrdinalIgnoreCase)
            || ContainsCommand(command, expectedName)) ?? false;

    private static bool ContainsOption(JsonNode? node, string expectedName)
        => (node?["options"]?.AsArray().Any(option =>
            string.Equals(option?["name"]?.GetValue<string>(), expectedName, StringComparison.OrdinalIgnoreCase)
            || (option?["aliases"]?.AsArray().Any(alias =>
                string.Equals(alias?.GetValue<string>(), expectedName, StringComparison.OrdinalIgnoreCase)) ?? false)) ?? false)
            || (node?["commands"]?.AsArray().Any(command => ContainsOption(command, expectedName)) ?? false);

    private static bool ContainsArgument(JsonNode? node, string expectedName)
        => (node?["arguments"]?.AsArray().Any(argument =>
            string.Equals(argument?["name"]?.GetValue<string>(), expectedName, StringComparison.OrdinalIgnoreCase)) ?? false)
            || (node?["commands"]?.AsArray().Any(command => ContainsArgument(command, expectedName)) ?? false);
}

internal sealed record AutoAcquisitionReport(
    ToolDescriptor Descriptor,
    IReadOnlyList<AutoAcquisitionAttemptReport> Attempts,
    bool Success,
    string? SelectedMode,
    string? SelectedFramework,
    JsonNode? OpenCliDocument,
    string? CrawlJson)
{
    public static AutoAcquisitionReport CreateSuccess(
        ToolDescriptor descriptor,
        IReadOnlyList<AutoAcquisitionAttemptReport> attempts,
        string selectedMode,
        string? selectedFramework,
        JsonNode? openCliDocument,
        string? crawlJson)
        => new(descriptor, attempts, true, selectedMode, selectedFramework, openCliDocument, crawlJson);

    public static AutoAcquisitionReport CreateFailure(
        ToolDescriptor descriptor,
        IReadOnlyList<AutoAcquisitionAttemptReport> attempts)
        => new(descriptor, attempts, false, null, null, null, null);
}

internal sealed record AutoAcquisitionAttemptReport(
    string Mode,
    string? Framework,
    string Outcome,
    string? Classification,
    string? Message,
    string? ArtifactSource);
