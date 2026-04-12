namespace InSpectra.Gen.Engine.Tests.Tooling.Process;

using System.Runtime.InteropServices;

internal static class InstalledDotnetToolCommandSupportTestSupport
{
    public static void WriteSharedFramework(string dotnetRoot, string frameworkName, string version)
        => Directory.CreateDirectory(Path.Combine(dotnetRoot, "shared", frameworkName, version));

    public static void WriteDotnetHost(string dotnetRoot)
    {
        Directory.CreateDirectory(dotnetRoot);
        File.WriteAllText(GetDotnetHostPath(dotnetRoot), string.Empty);
    }

    public static string GetDotnetHostPath(string dotnetRoot)
        => Path.Combine(dotnetRoot, OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet");

    public static void WriteCommandSettings(string installDirectory, string targetFrameworkMoniker, string entryPointFileName)
    {
        var settingsDirectory = Path.Combine(installDirectory, "tools", targetFrameworkMoniker, "any");
        Directory.CreateDirectory(settingsDirectory);
        File.WriteAllText(
            Path.Combine(settingsDirectory, "DotnetToolSettings.xml"),
            $$"""
              <DotNetCliTool>
                <Commands>
                  <Command Name="demo" EntryPoint="{{entryPointFileName}}" Runner="dotnet" />
                </Commands>
              </DotNetCliTool>
              """);
        File.WriteAllText(Path.Combine(settingsDirectory, entryPointFileName), string.Empty);
    }

    public static void WriteRuntimeConfig(string entryPointPath, string frameworkName, string frameworkVersion)
        => File.WriteAllText(
            Path.ChangeExtension(entryPointPath, ".runtimeconfig.json"),
            $$"""
              {
                "runtimeOptions": {
                  "framework": {
                    "name": "{{frameworkName}}",
                    "version": "{{frameworkVersion}}"
                  }
                }
              }
              """);

    public static void WriteRuntimeConfigFrameworks(string entryPointPath, params (string Name, string Version)[] frameworks)
        => File.WriteAllText(
            Path.ChangeExtension(entryPointPath, ".runtimeconfig.json"),
            $$"""
              {
                "runtimeOptions": {
                  "frameworks": [
              {{string.Join(
                  "," + Environment.NewLine,
                  frameworks.Select(framework =>
                      $$"""
                        {
                          "name": "{{framework.Name}}",
                          "version": "{{framework.Version}}"
                        }
                      """))}}
                  ]
                }
              }
              """);
}

internal sealed class DotnetEnvironmentScope : IDisposable
{
    private readonly string? _previousDotnetRoot;
    private readonly string? _previousArchitectureSpecificDotnetRoot;
    private readonly string? _previousLegacyX86DotnetRoot;
    private readonly string? _previousDotnetHostPath;
    private readonly string? _architectureSpecificVariableName;
    private readonly bool _useLegacyX86DotnetRoot;

    public DotnetEnvironmentScope(string? dotnetRoot = null, string? dotnetHostPath = null)
    {
        _previousDotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
        _architectureSpecificVariableName = ResolveArchitectureSpecificDotnetRootVariableName();
        _useLegacyX86DotnetRoot = OperatingSystem.IsWindows()
            && RuntimeInformation.ProcessArchitecture == Architecture.X86
            && Environment.Is64BitOperatingSystem;
        _previousArchitectureSpecificDotnetRoot = _architectureSpecificVariableName is null
            ? null
            : Environment.GetEnvironmentVariable(_architectureSpecificVariableName);
        _previousLegacyX86DotnetRoot = _useLegacyX86DotnetRoot
            ? Environment.GetEnvironmentVariable("DOTNET_ROOT(x86)")
            : null;
        _previousDotnetHostPath = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");

        Environment.SetEnvironmentVariable("DOTNET_ROOT", dotnetRoot);
        if (!string.IsNullOrWhiteSpace(_architectureSpecificVariableName))
        {
            Environment.SetEnvironmentVariable(_architectureSpecificVariableName, dotnetRoot);
        }

        if (_useLegacyX86DotnetRoot)
        {
            Environment.SetEnvironmentVariable("DOTNET_ROOT(x86)", dotnetRoot);
        }

        Environment.SetEnvironmentVariable("DOTNET_HOST_PATH", dotnetHostPath);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("DOTNET_ROOT", _previousDotnetRoot);
        if (!string.IsNullOrWhiteSpace(_architectureSpecificVariableName))
        {
            Environment.SetEnvironmentVariable(_architectureSpecificVariableName, _previousArchitectureSpecificDotnetRoot);
        }

        if (_useLegacyX86DotnetRoot)
        {
            Environment.SetEnvironmentVariable("DOTNET_ROOT(x86)", _previousLegacyX86DotnetRoot);
        }

        Environment.SetEnvironmentVariable("DOTNET_HOST_PATH", _previousDotnetHostPath);
    }

    private static string? ResolveArchitectureSpecificDotnetRootVariableName()
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        return RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "DOTNET_ROOT_X64",
            Architecture.X86 => "DOTNET_ROOT_X86",
            Architecture.Arm64 => "DOTNET_ROOT_ARM64",
            _ => null,
        };
    }
}
