using InSpectra.Gen.StartupHook.CommandLineParser;
using InSpectra.Gen.StartupHook.CommandLineUtils;
using InSpectra.Gen.StartupHook.FluentCommandLineParser;
using InSpectra.Gen.StartupHook.SystemCommandLine;
using System.Reflection;

namespace InSpectra.Gen.StartupHook.Frameworks;

internal static class HookCliFrameworkSupport
{
    public const string SystemCommandLine = "System.CommandLine";
    public const string McMasterExtensionsCommandLineUtils = "McMaster.Extensions.CommandLineUtils";
    public const string MicrosoftExtensionsCommandLineUtils = "Microsoft.Extensions.CommandLineUtils";
    public const string CommandLineParser = "CommandLineParser";
    public const string FluentCommandLineParser = "FluentCommandLineParser";

    public static string NormalizeExpectedFramework(string? cliFramework)
        => cliFramework switch
        {
            McMasterExtensionsCommandLineUtils => McMasterExtensionsCommandLineUtils,
            MicrosoftExtensionsCommandLineUtils => MicrosoftExtensionsCommandLineUtils,
            CommandLineParser => CommandLineParser,
            FluentCommandLineParser => FluentCommandLineParser,
            _ => SystemCommandLine,
        };

    public static string GetExpectedAssemblyName(string cliFramework)
        => cliFramework switch
        {
            CommandLineParser => "CommandLine",
            FluentCommandLineParser => "FluentCommandLineParser",
            _ => cliFramework,
        };

    public static bool MatchesExpectedAssembly(Assembly assembly, string cliFramework)
        => string.Equals(
            assembly.GetName().Name,
            GetExpectedAssemblyName(cliFramework),
            StringComparison.OrdinalIgnoreCase);

    public static void InstallPatches(Assembly assembly, string cliFramework, string capturePath)
    {
        switch (cliFramework)
        {
            case McMasterExtensionsCommandLineUtils:
            case MicrosoftExtensionsCommandLineUtils:
                CommandLineUtilsPatchInstaller.Install(assembly, cliFramework, capturePath);
                return;
            case CommandLineParser:
                CommandLineParserPatchInstaller.Install(assembly, capturePath);
                return;
            case FluentCommandLineParser:
                FluentCommandLineParserPatchInstaller.Install(assembly, capturePath);
                return;
            default:
                HarmonyPatchInstaller.Install(assembly, capturePath);
                return;
        }
    }
}
