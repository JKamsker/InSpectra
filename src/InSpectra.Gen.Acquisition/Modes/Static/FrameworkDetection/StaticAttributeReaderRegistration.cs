namespace InSpectra.Gen.Acquisition.Modes.Static.FrameworkDetection;

using System.Runtime.CompilerServices;

using InSpectra.Gen.Acquisition.Modes.Static.Attributes;
using InSpectra.Gen.Acquisition.Modes.Static.Attributes.Cocona;
using InSpectra.Gen.Acquisition.Modes.Static.Attributes.SystemCommandLine;
using InSpectra.Gen.Acquisition.Tooling.FrameworkDetection;

/// <summary>
/// Bootstraps Static-mode attribute readers into the Tooling-level
/// <see cref="CliFrameworkProviderRegistry"/> at module load.
///
/// <para>
/// This type exists to keep the <c>Tooling/</c> layer free of any direct reference to
/// <c>Modes.Static.Attributes.*</c> concrete reader types. The Registry exposes a typed
/// registration API and this module initializer wires up every Static-mode reader
/// before any consumer calls <see cref="CliFrameworkProviderRegistry.Detect"/> or the
/// other query methods. Module initializers are guaranteed to run before any type in
/// this assembly is first accessed, so the ordering is race-free.
/// </para>
/// </summary>
internal static class StaticAttributeReaderRegistration
{
#pragma warning disable CA2255 // ModuleInitializer is the cleanest way to keep Tooling free of Modes imports here.
    [ModuleInitializer]
    internal static void Register()
#pragma warning restore CA2255
    {
        CliFrameworkProviderRegistry.RegisterStaticAnalysisProvider(
            name: "System.CommandLine",
            dependencyIds: ["System.CommandLine"],
            packageAssemblyNames: ["System.CommandLine.dll"],
            staticAssemblyName: "System.CommandLine",
            reader: new SystemCommandLineAttributeReader());

        CliFrameworkProviderRegistry.RegisterStaticAnalysisProvider(
            name: "McMaster.Extensions.CommandLineUtils",
            dependencyIds: ["McMaster.Extensions.CommandLineUtils"],
            packageAssemblyNames: ["McMaster.Extensions.CommandLineUtils.dll"],
            staticAssemblyName: "McMaster.Extensions.CommandLineUtils",
            reader: new McMasterAttributeReader());

        CliFrameworkProviderRegistry.RegisterStaticAnalysisProvider(
            name: "Microsoft.Extensions.CommandLineUtils",
            dependencyIds: ["Microsoft.Extensions.CommandLineUtils"],
            packageAssemblyNames: ["Microsoft.Extensions.CommandLineUtils.dll"],
            staticAssemblyName: "Microsoft.Extensions.CommandLineUtils",
            reader: new McMasterAttributeReader());

        CliFrameworkProviderRegistry.RegisterStaticAnalysisProvider(
            name: "Argu",
            dependencyIds: ["Argu"],
            packageAssemblyNames: ["Argu.dll"],
            staticAssemblyName: "Argu",
            reader: new ArguAttributeReader());

        CliFrameworkProviderRegistry.RegisterStaticAnalysisProvider(
            name: "Cocona",
            dependencyIds: ["Cocona"],
            packageAssemblyNames: ["Cocona.dll"],
            staticAssemblyName: "Cocona",
            reader: new CoconaAttributeReader());

        CliFrameworkProviderRegistry.RegisterStaticAnalysisProvider(
            name: "CommandDotNet",
            dependencyIds: ["CommandDotNet"],
            packageAssemblyNames: ["CommandDotNet.dll"],
            staticAssemblyName: "CommandDotNet",
            reader: new CommandDotNetAttributeReader());

        CliFrameworkProviderRegistry.RegisterStaticAnalysisProvider(
            name: "PowerArgs",
            dependencyIds: ["PowerArgs"],
            packageAssemblyNames: ["PowerArgs.dll"],
            staticAssemblyName: "PowerArgs",
            reader: new PowerArgsAttributeReader());

        CliFrameworkProviderRegistry.RegisterStaticAnalysisProvider(
            name: "CommandLineParser",
            dependencyIds: ["CommandLineParser"],
            packageAssemblyNames: ["CommandLine.dll"],
            staticAssemblyName: "CommandLine",
            reader: new CmdParserAttributeReader());
    }
}
