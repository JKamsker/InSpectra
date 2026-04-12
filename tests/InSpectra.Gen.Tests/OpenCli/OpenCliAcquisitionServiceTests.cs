using System.Text.Json.Nodes;
using InSpectra.Gen.Core;
using InSpectra.Gen.Engine.Execution.Process;
using InSpectra.Gen.Engine.Targets.Sources;
using InSpectra.Gen.Engine.UseCases.Generate;
using InSpectra.Gen.Engine.UseCases.Generate.Requests;
using InSpectra.Gen.Tests.TestSupport;

namespace InSpectra.Gen.Tests.OpenCli;

public sealed class OpenCliAcquisitionServiceTests
{
    [Fact]
    public async Task Acquisition_service_writes_artifacts_for_successful_analysis_dispatch()
    {
        using var temp = new TempDirectory();
        var sourcePath = Path.Combine(temp.Path, "demo.cmd");
        await File.WriteAllTextAsync(sourcePath, "@echo off");
        var openCliPath = Path.Combine(temp.Path, "opencli.json");
        var crawlPath = Path.Combine(temp.Path, "crawl.json");
        var openCliJson = "{\"openCliVersion\":\"0.1-draft\",\"info\":{\"title\":\"demo\",\"version\":\"1.0.0\"}}";
        var crawlJson = "{\"commands\":[]}";
        var service = new OpenCliAcquisitionService(
            new ExecutableResolver(),
            new OpenCliNativeAcquisitionSupport(new FakeProcessRunner()),
            new LocalCliTargetFactory(new FakeLocalCliFrameworkDetector()),
            new PackageCliTargetFactory(new UnusedPackageInstaller()),
            new DotnetBuildOutputResolver(new FakeProcessRunner()),
            new EmptyCliFrameworkCatalog(),
            new SuccessfulDispatcher(openCliJson, crawlJson));

        var result = await service.AcquireFromExecAsync(
            new ExecAcquisitionRequest(
                sourcePath,
                [],
                temp.Path,
                new AcquisitionOptions(
                    OpenCliMode.Hook,
                    "demo",
                    null,
                    [],
                    false,
                    [],
                    30,
                    new OpenCliArtifactOptions(openCliPath, crawlPath))),
            CancellationToken.None);

        Assert.Equal(Path.GetFullPath(openCliPath), result.Metadata.OpenCliOutputPath);
        Assert.Equal(Path.GetFullPath(crawlPath), result.Metadata.CrawlOutputPath);
        Assert.Equal(
            JsonNode.Parse(openCliJson)?.ToJsonString(),
            JsonNode.Parse(await File.ReadAllTextAsync(openCliPath))?.ToJsonString());
        Assert.Equal(
            JsonNode.Parse(crawlJson)?.ToJsonString(),
            JsonNode.Parse(await File.ReadAllTextAsync(crawlPath))?.ToJsonString());
    }

    [Fact]
    public async Task Exec_acquisition_keeps_cleanup_root_null_for_local_targets()
    {
        using var temp = new TempDirectory();
        var sourcePath = Path.Combine(temp.Path, "demo.cmd");
        await File.WriteAllTextAsync(sourcePath, "@echo off");
        var openCliPath = Path.Combine(temp.Path, "opencli.json");
        var dispatcher = new CapturingDispatcher(
            "{\"openCliVersion\":\"0.1-draft\",\"info\":{\"title\":\"demo\",\"version\":\"1.0.0\"}}",
            crawlJson: null);
        var service = new OpenCliAcquisitionService(
            new ExecutableResolver(),
            new OpenCliNativeAcquisitionSupport(new FakeProcessRunner()),
            new LocalCliTargetFactory(new FakeLocalCliFrameworkDetector()),
            new PackageCliTargetFactory(new UnusedPackageInstaller()),
            new DotnetBuildOutputResolver(new FakeProcessRunner()),
            new EmptyCliFrameworkCatalog(),
            dispatcher);

        await service.AcquireFromExecAsync(
            new ExecAcquisitionRequest(
                sourcePath,
                [],
                temp.Path,
                new AcquisitionOptions(
                    OpenCliMode.Hook,
                    "demo",
                    null,
                    [],
                    false,
                    [],
                    30,
                    new OpenCliArtifactOptions(openCliPath, null))),
            CancellationToken.None);

        Assert.Null(dispatcher.LastCleanupRoot);
    }

    [Fact]
    public async Task Package_acquisition_forwards_sandbox_cleanup_root_to_internal_dispatcher()
    {
        using var temp = new TempDirectory();
        var openCliPath = Path.Combine(temp.Path, "opencli.json");
        var openCliJson = "{\"openCliVersion\":\"0.1-draft\",\"info\":{\"title\":\"demo\",\"version\":\"1.0.0\"}}";
        var packageInstaller = new RecordingPackageInstaller();
        var dispatcher = new CapturingDispatcher(openCliJson, crawlJson: null);
        var service = new OpenCliAcquisitionService(
            new ExecutableResolver(),
            new OpenCliNativeAcquisitionSupport(new FakeProcessRunner()),
            new LocalCliTargetFactory(new FakeLocalCliFrameworkDetector()),
            new PackageCliTargetFactory(packageInstaller),
            new DotnetBuildOutputResolver(new FakeProcessRunner()),
            new EmptyCliFrameworkCatalog(),
            dispatcher);

        var result = await service.AcquireFromPackageAsync(
            new PackageAcquisitionRequest(
                "Demo.Tool",
                "1.2.3",
                new AcquisitionOptions(
                    OpenCliMode.Hook,
                    "demo",
                    null,
                    [],
                    false,
                    [],
                    30,
                    new OpenCliArtifactOptions(openCliPath, null))),
            CancellationToken.None);

        Assert.NotNull(packageInstaller.LastTempRoot);
        Assert.Equal(Path.GetFullPath(packageInstaller.LastTempRoot), dispatcher.LastCleanupRoot);
        Assert.Equal(Path.GetFullPath(openCliPath), result.Metadata.OpenCliOutputPath);
    }

    [Fact]
    public async Task Package_native_acquisition_passes_cleanup_root_to_process_runner()
    {
        using var temp = new TempDirectory();
        var openCliPath = Path.Combine(temp.Path, "opencli.json");
        var packageInstaller = new RecordingPackageInstaller();
        var processRunner = new CapturingProcessRunner();
        var service = new OpenCliAcquisitionService(
            new ExecutableResolver(),
            new OpenCliNativeAcquisitionSupport(processRunner),
            new LocalCliTargetFactory(new FakeLocalCliFrameworkDetector()),
            new PackageCliTargetFactory(packageInstaller),
            new DotnetBuildOutputResolver(new FakeProcessRunner()),
            new EmptyCliFrameworkCatalog(),
            new SuccessfulDispatcher(
                "{\"openCliVersion\":\"0.1-draft\",\"info\":{\"title\":\"demo\",\"version\":\"1.0.0\"}}",
                crawlJson: null));

        await service.AcquireFromPackageAsync(
            new PackageAcquisitionRequest(
                "Demo.Tool",
                "1.2.3",
                new AcquisitionOptions(
                    OpenCliMode.Native,
                    "demo",
                    null,
                    [],
                    true,
                    ["xml"],
                    30,
                    new OpenCliArtifactOptions(openCliPath, null))),
            CancellationToken.None);

        Assert.NotNull(packageInstaller.LastTempRoot);
        Assert.Equal(
            [Path.GetFullPath(packageInstaller.LastTempRoot), Path.GetFullPath(packageInstaller.LastTempRoot)],
            processRunner.CleanupRoots);
    }

    [Fact]
    public async Task Package_xml_doc_after_analysis_passes_cleanup_root_to_process_runner()
    {
        using var temp = new TempDirectory();
        var openCliPath = Path.Combine(temp.Path, "opencli.json");
        var packageInstaller = new RecordingPackageInstaller();
        var processRunner = new CapturingProcessRunner();
        var dispatcher = new CapturingDispatcher(
            "{\"openCliVersion\":\"0.1-draft\",\"info\":{\"title\":\"demo\",\"version\":\"1.0.0\"}}",
            crawlJson: null);
        var service = new OpenCliAcquisitionService(
            new ExecutableResolver(),
            new OpenCliNativeAcquisitionSupport(processRunner),
            new LocalCliTargetFactory(new FakeLocalCliFrameworkDetector()),
            new PackageCliTargetFactory(packageInstaller),
            new DotnetBuildOutputResolver(new FakeProcessRunner()),
            new EmptyCliFrameworkCatalog(),
            dispatcher);

        await service.AcquireFromPackageAsync(
            new PackageAcquisitionRequest(
                "Demo.Tool",
                "1.2.3",
                new AcquisitionOptions(
                    OpenCliMode.Hook,
                    "demo",
                    null,
                    [],
                    true,
                    ["xml"],
                    30,
                    new OpenCliArtifactOptions(openCliPath, null))),
            CancellationToken.None);

        Assert.NotNull(packageInstaller.LastTempRoot);
        Assert.Equal([Path.GetFullPath(packageInstaller.LastTempRoot)], processRunner.CleanupRoots);
        Assert.Equal(Path.GetFullPath(packageInstaller.LastTempRoot), dispatcher.LastCleanupRoot);
    }

    [Fact]
    public async Task Auto_mode_final_failure_preserves_native_attempt_detail()
    {
        using var temp = new TempDirectory();
        var sourcePath = Path.Combine(temp.Path, "demo.cmd");
        await File.WriteAllTextAsync(sourcePath, "@echo off");
        var service = new OpenCliAcquisitionService(
            new ExecutableResolver(),
            new OpenCliNativeAcquisitionSupport(new ThrowingAcquisitionProcessRunner(
                new CliSourceExecutionException(
                    "Native analysis failed.",
                    details:
                    [
                        "Arguments: inspect --opencli",
                        "Standard output:\nusage details",
                    ]))),
            new LocalCliTargetFactory(new FakeLocalCliFrameworkDetector()),
            new PackageCliTargetFactory(new UnusedPackageInstaller()),
            new DotnetBuildOutputResolver(new FakeProcessRunner()),
            new EmptyCliFrameworkCatalog(),
            new AlwaysFailingDispatcher());

        var exception = await Assert.ThrowsAsync<CliSourceExecutionException>(() => service.AcquireFromExecAsync(
            new ExecAcquisitionRequest(
                sourcePath,
                [],
                temp.Path,
                new AcquisitionOptions(
                    OpenCliMode.Auto,
                    "demo",
                    null,
                    [],
                    false,
                    [],
                    30,
                    new OpenCliArtifactOptions(null, null))),
            CancellationToken.None));

        Assert.Contains(exception.Details, detail => detail.Contains("native: Native analysis failed.", StringComparison.Ordinal));
        Assert.Contains(exception.Details, detail => detail.Contains("Arguments: inspect --opencli", StringComparison.Ordinal));
        Assert.Contains(exception.Details, detail => detail.Contains("help: help mode failed", StringComparison.Ordinal));
    }
}
