namespace InSpectra.Gen.Engine.Modes.Hook.Execution;

using InSpectra.Gen.Engine.Modes.Hook.Capture;
using InSpectra.Gen.Engine.Tooling.Process;

internal static class HookProcessRetrySupport
{
    private const string CapturePathEnvironmentVariableName = "INSPECTRA_CAPTURE_PATH";

    public static async Task<PublishedRetryResult> InvokeWithHelpFallbackAsync(
        HookToolProcessInvocation invocation,
        IReadOnlyDictionary<string, string> environment,
        string capturePath,
        Func<HookToolProcessInvocation, IReadOnlyDictionary<string, string>, CancellationToken, Task<CommandRuntime.ProcessResult>> invokeAsync,
        CancellationToken cancellationToken)
    {
        var capturePublisher = new RetryCapturePublisher(capturePath);
        var attemptedHelpInvocations = new HashSet<string>(StringComparer.Ordinal);
        attemptedHelpInvocations.Add(BuildInvocationKey(invocation));
        var retryResult = await InvokeWithCompatibilityRetriesAsync(
            invocation,
            environment,
            capturePublisher,
            attemptedHelpInvocations,
            invokeAsync,
            cancellationToken);
        if (!ShouldRetryWithAlternateHelp(retryResult))
        {
            return capturePublisher.Publish(retryResult);
        }

        foreach (var fallbackInvocation in HookToolProcessInvocationResolver.BuildHelpFallbackInvocations(invocation))
        {
            if (!attemptedHelpInvocations.Add(BuildInvocationKey(fallbackInvocation)))
            {
                continue;
            }

            capturePublisher.DeleteAttemptCapture(retryResult);
            retryResult = await InvokeWithCompatibilityRetriesAsync(
                fallbackInvocation,
                environment,
                capturePublisher,
                attemptedHelpInvocations,
                invokeAsync,
                cancellationToken);
            if (!ShouldRetryWithAlternateHelp(retryResult))
            {
                return capturePublisher.Publish(retryResult);
            }
        }

        return capturePublisher.Publish(retryResult);
    }

    private static async Task<RetryInvocationResult> InvokeWithCompatibilityRetriesAsync(
        HookToolProcessInvocation invocation,
        IReadOnlyDictionary<string, string> environment,
        RetryCapturePublisher capturePublisher,
        ISet<string> attemptedHelpInvocations,
        Func<HookToolProcessInvocation, IReadOnlyDictionary<string, string>, CancellationToken, Task<CommandRuntime.ProcessResult>> invokeAsync,
        CancellationToken cancellationToken)
    {
        var effectiveEnvironment = environment;
        var retryResult = await InvokeAttemptAsync(
            invocation,
            effectiveEnvironment,
            capturePublisher,
            invokeAsync,
            cancellationToken);
        if (!retryResult.HasCapture
            && DotnetRuntimeCompatibilitySupport.LooksLikeMissingIcu(retryResult.ProcessResult)
            && !effectiveEnvironment.ContainsKey(DotnetRuntimeCompatibilitySupport.GlobalizationInvariantEnvironmentVariableName))
        {
            effectiveEnvironment = new Dictionary<string, string>(effectiveEnvironment, StringComparer.OrdinalIgnoreCase)
            {
                [DotnetRuntimeCompatibilitySupport.GlobalizationInvariantEnvironmentVariableName] = "1",
            };

            retryResult = await InvokeAttemptAsync(
                invocation,
                effectiveEnvironment,
                capturePublisher,
                invokeAsync,
                cancellationToken);
            if (retryResult.HasCapture || !HookRejectedHelpSupport.LooksLikeRejectedHelpInvocation(retryResult.ProcessResult))
            {
                return retryResult;
            }

            foreach (var fallbackInvocation in HookToolProcessInvocationResolver.BuildHelpFallbackInvocations(invocation))
            {
                if (!attemptedHelpInvocations.Add(BuildInvocationKey(fallbackInvocation)))
                {
                    continue;
                }

                retryResult = await InvokeAttemptAsync(
                    fallbackInvocation,
                    effectiveEnvironment,
                    capturePublisher,
                    invokeAsync,
                    cancellationToken);
                if (retryResult.HasCapture || !HookRejectedHelpSupport.LooksLikeRejectedHelpInvocation(retryResult.ProcessResult))
                {
                    return retryResult;
                }
            }
        }

        if (retryResult.HasCapture
            || !DotnetRuntimeCompatibilitySupport.LooksLikeMissingSharedRuntime(retryResult.ProcessResult)
            || effectiveEnvironment.ContainsKey(DotnetRuntimeCompatibilitySupport.DotnetRollForwardEnvironmentVariableName))
        {
            return retryResult;
        }

        effectiveEnvironment = new Dictionary<string, string>(effectiveEnvironment, StringComparer.OrdinalIgnoreCase)
        {
            [DotnetRuntimeCompatibilitySupport.DotnetRollForwardEnvironmentVariableName] = DotnetRuntimeCompatibilitySupport.DotnetRollForwardMajorValue,
        };

        retryResult = await InvokeAttemptAsync(
            invocation,
            effectiveEnvironment,
            capturePublisher,
            invokeAsync,
            cancellationToken);
        if (retryResult.HasCapture || !HookRejectedHelpSupport.LooksLikeRejectedHelpInvocation(retryResult.ProcessResult))
        {
            return retryResult;
        }

        foreach (var fallbackInvocation in HookToolProcessInvocationResolver.BuildHelpFallbackInvocations(invocation))
        {
            if (!attemptedHelpInvocations.Add(BuildInvocationKey(fallbackInvocation)))
            {
                continue;
            }

            retryResult = await InvokeAttemptAsync(
                fallbackInvocation,
                effectiveEnvironment,
                capturePublisher,
                invokeAsync,
                cancellationToken);
            if (retryResult.HasCapture || !HookRejectedHelpSupport.LooksLikeRejectedHelpInvocation(retryResult.ProcessResult))
            {
                return retryResult;
            }
        }

        return retryResult;
    }

    private static async Task<RetryInvocationResult> InvokeAttemptAsync(
        HookToolProcessInvocation invocation,
        IReadOnlyDictionary<string, string> environment,
        RetryCapturePublisher capturePublisher,
        Func<HookToolProcessInvocation, IReadOnlyDictionary<string, string>, CancellationToken, Task<CommandRuntime.ProcessResult>> invokeAsync,
        CancellationToken cancellationToken)
    {
        var attemptCapturePath = capturePublisher.CreateAttemptCapturePath();
        var effectiveEnvironment = new Dictionary<string, string>(environment, StringComparer.OrdinalIgnoreCase)
        {
            [CapturePathEnvironmentVariableName] = attemptCapturePath,
        };
        var processResult = await invokeAsync(invocation, effectiveEnvironment, cancellationToken);
        return new RetryInvocationResult(processResult, attemptCapturePath);
    }

    private static bool ShouldRetryWithAlternateHelp(RetryInvocationResult retryResult)
    {
        if (!retryResult.HasCapture)
        {
            return HookRejectedHelpSupport.LooksLikeRejectedHelpInvocation(retryResult.ProcessResult);
        }

        var capture = HookCaptureDeserializer.Deserialize(retryResult.CapturePath);
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

    private sealed record RetryInvocationResult(CommandRuntime.ProcessResult ProcessResult, string CapturePath)
    {
        public bool HasCapture => File.Exists(CapturePath);
    }

    internal sealed record PublishedRetryResult(CommandRuntime.ProcessResult ProcessResult, string? CapturePath);

    private sealed class RetryCapturePublisher(string requestedCapturePath)
    {
        private readonly string _requestedCapturePath = requestedCapturePath;
        private int _attemptNumber;

        public string CreateAttemptCapturePath()
        {
            _attemptNumber++;
            var directory = Path.GetDirectoryName(_requestedCapturePath) ?? string.Empty;
            var fileName = Path.GetFileNameWithoutExtension(_requestedCapturePath);
            var extension = Path.GetExtension(_requestedCapturePath);
            return Path.Combine(directory, $"{fileName}.attempt-{_attemptNumber:D3}{extension}");
        }

        public PublishedRetryResult Publish(RetryInvocationResult retryResult)
        {
            TryDeleteCaptureFile(_requestedCapturePath);
            if (!retryResult.HasCapture)
            {
                DeleteAttemptCapture(retryResult);
                return new PublishedRetryResult(retryResult.ProcessResult, CapturePath: null);
            }

            try
            {
                var directory = Path.GetDirectoryName(_requestedCapturePath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.Copy(retryResult.CapturePath, _requestedCapturePath, overwrite: true);
            }
            catch
            {
                TryDeleteCaptureFile(_requestedCapturePath);
                return new PublishedRetryResult(
                    retryResult.ProcessResult,
                    File.Exists(retryResult.CapturePath) ? retryResult.CapturePath : null);
            }

            DeleteAttemptCapture(retryResult);
            return new PublishedRetryResult(retryResult.ProcessResult, _requestedCapturePath);
        }

        public void DeleteAttemptCapture(RetryInvocationResult retryResult)
            => TryDeleteCaptureFile(retryResult.CapturePath);
    }
}
