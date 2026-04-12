namespace InSpectra.Gen.Engine.Tooling.Process;

internal static class CommandSandboxEnvironmentSupport
{
    public static CommandRuntime.SandboxEnvironment CreateSandboxEnvironment(string tempRoot)
    {
        var sandboxRoot = Path.GetFullPath(tempRoot);
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["HOME"] = Path.Combine(sandboxRoot, "home"),
            ["DOTNET_CLI_HOME"] = Path.Combine(sandboxRoot, "dotnet-home"),
            ["DOTNET_ADD_GLOBAL_TOOLS_TO_PATH"] = "0",
            ["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1",
            ["DOTNET_NOLOGO"] = "1",
            ["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1",
            ["DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE"] = "1",
            ["DOTNET_GENERATE_ASPNET_CERTIFICATE"] = "0",
            ["DOTNET_SYSTEM_CONSOLE_ALLOW_ANSI_COLOR_REDIRECTION"] = "0",
            ["NUGET_PACKAGES"] = Path.Combine(sandboxRoot, "nuget-packages"),
            ["NUGET_HTTP_CACHE_PATH"] = Path.Combine(sandboxRoot, "nuget-http-cache"),
            ["NO_COLOR"] = "1",
            ["FORCE_COLOR"] = "0",
            ["TERM"] = "dumb",
            ["XDG_CONFIG_HOME"] = Path.Combine(sandboxRoot, "xdg-config"),
            ["XDG_CACHE_HOME"] = Path.Combine(sandboxRoot, "xdg-cache"),
            ["XDG_DATA_HOME"] = Path.Combine(sandboxRoot, "xdg-data"),
            ["XDG_RUNTIME_DIR"] = Path.Combine(sandboxRoot, "xdg-runtime"),
            ["TMPDIR"] = Path.Combine(sandboxRoot, "tmp"),
            ["CI"] = "true",
            ["GCM_CREDENTIAL_STORE"] = "none",
            ["GCM_INTERACTIVE"] = "never",
            ["GIT_TERMINAL_PROMPT"] = "0",
        };

        values["TMP"] = values["TMPDIR"];
        values["TEMP"] = values["TMPDIR"];
        values["USERPROFILE"] = values["HOME"];
        values["APPDATA"] = values["XDG_CONFIG_HOME"];
        values["LOCALAPPDATA"] = values["XDG_DATA_HOME"];

        return new CommandRuntime.SandboxEnvironment(
            Values: values,
            Directories:
            [
                values["HOME"],
                values["DOTNET_CLI_HOME"],
                values["NUGET_PACKAGES"],
                values["NUGET_HTTP_CACHE_PATH"],
                values["XDG_CONFIG_HOME"],
                values["XDG_CACHE_HOME"],
                values["XDG_DATA_HOME"],
                values["XDG_RUNTIME_DIR"],
                values["TMPDIR"],
            ],
            CleanupRoot: sandboxRoot);
    }
}
