using InSpectra.Gen.Composition;
using InSpectra.Gen.OpenCli.Acquisition;
using InSpectra.Gen.Targets;
using Microsoft.Extensions.DependencyInjection;

namespace InSpectra.Gen.Tests;

public class CompositionActivationTests
{
    [Fact]
    public void Service_collection_resolves_generate_services()
    {
        var services = new ServiceCollection();
        services.AddInSpectraGen();

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IOpenCliAcquisitionService>());
        Assert.NotNull(provider.GetRequiredService<IOpenCliGenerationService>());
        Assert.NotNull(provider.GetRequiredService<PackageCliTargetFactory>());
    }
}
