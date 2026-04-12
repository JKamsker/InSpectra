using InSpectra.Gen.StartupHook.Capture;
using InSpectra.Gen.StartupHook.Hooking;

// No namespace - required by DOTNET_STARTUP_HOOKS contract.

internal class StartupHook
{
    public static void Initialize()
    {
        var capturePath = Environment.GetEnvironmentVariable("INSPECTRA_CAPTURE_PATH");
        var expectedCliFramework = Environment.GetEnvironmentVariable("INSPECTRA_EXPECTED_CLI_FRAMEWORK");
        var preferredFrameworkDirectory = Environment.GetEnvironmentVariable("INSPECTRA_PREFERRED_FRAMEWORK_DIRECTORY");
        if (string.IsNullOrEmpty(capturePath))
            return;

        try
        {
            HookDependencyLoader.LoadRequiredAssemblies();
            AssemblyLoadInterceptor.Start(capturePath, expectedCliFramework, preferredFrameworkDirectory);
        }
        catch (Exception ex)
        {
            CaptureFileWriter.WriteError(capturePath, "initialize-failed", ex.ToString());
        }
    }
}
