namespace InSpectra.Gen.Acquisition.Modes.Hook;

using InSpectra.Gen.Acquisition.Modes.Hook.Models;
using InSpectra.Gen.Acquisition.Tooling.Process;

internal static class HookProcessRetrySupport
{
    public static async Task<CommandRuntime.ProcessResult> InvokeWithHelpFallbackAsync(
        HookToolProcessInvocation invocation,
        IReadOnlyDictionary<string, string> environment,
        string capturePath,
        Func<HookToolProcessInvocation, IReadOnlyDictionary<string, string>, CancellationToken, Task<CommandRuntime.ProcessResult>> invokeAsync,
        CancellationToken cancellationToken)
    {
        var attemptedHelpInvocations = new HashSet<string>(StringComparer.Ordinal);
        attemptedHelpInvocations.Add(BuildInvocationKey(invocation));
        var processResult = await InvokeWithCompatibilityRetriesAsync(
            invocation,
            environment,
            capturePath,
            attemptedHelpInvocations,
            invokeAsync,
            cancellationToken);
        if (!ShouldRetryWithAlternateHelp(processResult, capturePath))
        {
            return processResult;
        }

        foreach (var fallbackInvocation in HookToolProcessInvocationResolver.BuildHelpFallbackInvocations(invocation))
        {
            if (!attemptedHelpInvocations.Add(BuildInvocationKey(fallbackInvocation)))
            {
                continue;
            }

            TryDeleteCaptureFile(capturePath);
            processResult = await InvokeWithCompatibilityRetriesAsync(
                fallbackInvocation,
                environment,
                capturePath,
                attemptedHelpInvocations,
                invokeAsync,
                cancellationToken);
            if (!ShouldRetryWithAlternateHelp(processResult, capturePath))
            {
                return processResult;
            }
        }

        return processResult;
    }

    private static async Task<CommandRuntime.ProcessResult> InvokeWithCompatibilityRetriesAsync(
        HookToolProcessInvocation invocation,
        IReadOnlyDictionary<string, string> environment,
        string capturePath,
        ISet<string> attemptedHelpInvocations,
        Func<HookToolProcessInvocation, IReadOnlyDictionary<string, string>, CancellationToken, Task<CommandRuntime.ProcessResult>> invokeAsync,
        CancellationToken cancellationToken)
    {
        var effectiveEnvironment = environment;
        var processResult = await invokeAsync(invocation, effectiveEnvironment, cancellationToken);
        if (!File.Exists(capturePath)
            && DotnetRuntimeCompatibilitySupport.LooksLikeMissingIcu(processResult)
            && !effectiveEnvironment.ContainsKey(DotnetRuntimeCompatibilitySupport.GlobalizationInvariantEnvironmentVariableName))
        {
            effectiveEnvironment = new Dictionary<string, string>(effectiveEnvironment, StringComparer.OrdinalIgnoreCase)
            {
                [DotnetRuntimeCompatibilitySupport.GlobalizationInvariantEnvironmentVariableName] = "1",
            };

            TryDeleteCaptureFile(capturePath);
            processResult = await invokeAsync(invocation, effectiveEnvironment, cancellationToken);
            if (File.Exists(capturePath) || !HookRejectedHelpSupport.LooksLikeRejectedHelpInvocation(processResult))
            {
                return processResult;
            }

            foreach (var fallbackInvocation in HookToolProcessInvocationResolver.BuildHelpFallbackInvocations(invocation))
            {
                if (!attemptedHelpInvocations.Add(BuildInvocationKey(fallbackInvocation)))
                {
                    continue;
                }

                TryDeleteCaptureFile(capturePath);
                processResult = await invokeAsync(fallbackInvocation, effectiveEnvironment, cancellationToken);
                if (File.Exists(capturePath) || !HookRejectedHelpSupport.LooksLikeRejectedHelpInvocation(processResult))
                {
                    return processResult;
                }
            }
        }

        if (File.Exists(capturePath)
            || !DotnetRuntimeCompatibilitySupport.LooksLikeMissingSharedRuntime(processResult)
            || effectiveEnvironment.ContainsKey(DotnetRuntimeCompatibilitySupport.DotnetRollForwardEnvironmentVariableName))
        {
            return processResult;
        }

        effectiveEnvironment = new Dictionary<string, string>(effectiveEnvironment, StringComparer.OrdinalIgnoreCase)
        {
            [DotnetRuntimeCompatibilitySupport.DotnetRollForwardEnvironmentVariableName] = DotnetRuntimeCompatibilitySupport.DotnetRollForwardMajorValue,
        };

        TryDeleteCaptureFile(capturePath);
        processResult = await invokeAsync(invocation, effectiveEnvironment, cancellationToken);
        if (File.Exists(capturePath) || !HookRejectedHelpSupport.LooksLikeRejectedHelpInvocation(processResult))
        {
            return processResult;
        }

        foreach (var fallbackInvocation in HookToolProcessInvocationResolver.BuildHelpFallbackInvocations(invocation))
        {
            if (!attemptedHelpInvocations.Add(BuildInvocationKey(fallbackInvocation)))
            {
                continue;
            }

            TryDeleteCaptureFile(capturePath);
            processResult = await invokeAsync(fallbackInvocation, effectiveEnvironment, cancellationToken);
            if (File.Exists(capturePath) || !HookRejectedHelpSupport.LooksLikeRejectedHelpInvocation(processResult))
            {
                return processResult;
            }
        }

        return processResult;
    }

    private static bool ShouldRetryWithAlternateHelp(CommandRuntime.ProcessResult processResult, string capturePath)
    {
        if (!File.Exists(capturePath))
        {
            return HookRejectedHelpSupport.LooksLikeRejectedHelpInvocation(processResult);
        }

        var capture = HookCaptureDeserializer.Deserialize(capturePath);
        if (capture is null || (capture.Status == "ok" && capture.Root is not null))
        {
            return false;
        }

        return HookRejectedHelpSupport.LooksLikeRejectedHelpMessage(capture.Error);
    }

    private static void TryDeleteCaptureFile(string capturePath)
    {
        try
        {
            if (File.Exists(capturePath))
            {
                File.Delete(capturePath);
            }
        }
        catch
        {
        }
    }

    private static string BuildInvocationKey(HookToolProcessInvocation invocation)
        => string.Join(
            '\u001f',
            [invocation.FilePath, invocation.PreferredAssemblyPath ?? string.Empty, .. invocation.ArgumentList]);
}
