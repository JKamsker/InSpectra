namespace InSpectra.Gen.Engine.Modes.Help.Crawling;

using InSpectra.Gen.Engine.Contracts.Documents;
using InSpectra.Gen.Engine.Modes.Help.Projection;
using InSpectra.Gen.Engine.Tooling.DocumentPipeline.Documents;
using InSpectra.Gen.Engine.Tooling.Process;

using System.Text.Json.Nodes;

internal static class CapturePayloadCandidateSupport
{
    public static IReadOnlyList<string> EnumeratePayloadCandidates(CommandRuntime.ProcessResult processResult)
        => EnumeratePayloadCandidates(
            storedPayload: null,
            stdout: CommandRuntime.NormalizeConsoleText(processResult.Stdout),
            stderr: CommandRuntime.NormalizeConsoleText(processResult.Stderr));

    public static IReadOnlyList<string> EnumeratePayloadCandidates(JsonObject capture)
        => EnumeratePayloadCandidates(
            storedPayload: CommandRuntime.NormalizeConsoleText(capture["payload"]?.GetValue<string>()),
            stdout: capture["result"] is JsonObject processResult
                ? CommandRuntime.NormalizeConsoleText(processResult["stdout"]?.GetValue<string>())
                : null,
            stderr: capture["result"] is JsonObject processResultValue
                ? CommandRuntime.NormalizeConsoleText(processResultValue["stderr"]?.GetValue<string>())
                : null);

    public static int GetPayloadSelectionPenalty(string payload, string? helpInvocation)
    {
        if (string.IsNullOrWhiteSpace(payload) || string.IsNullOrWhiteSpace(helpInvocation))
        {
            return 0;
        }

        var lines = payload
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .ToArray();
        var edgeLines = lines
            .Take(4)
            .Concat(lines.TakeLast(4))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return edgeLines.Any(line => string.Equals(line, helpInvocation, StringComparison.OrdinalIgnoreCase))
            ? 50
            : 0;
    }

    public static int ScorePayloadCandidate(
        string storedCommand,
        Document document,
        string? helpInvocation,
        string payload)
    {
        var score = DocumentInspector.Score(document) - GetPayloadSelectionPenalty(payload, helpInvocation);
        if (string.IsNullOrWhiteSpace(storedCommand))
        {
            return score;
        }

        var storedCommandLeaf = CommandPathSupport.SplitSegments(storedCommand).LastOrDefault();
        var hasLeafSurface = document.Options.Count > 0 || document.Arguments.Count > 0;
        if (!hasLeafSurface
            && document.Commands.Count > 0
            && (string.Equals(storedCommandLeaf, "help", StringComparison.OrdinalIgnoreCase)
                || string.Equals(storedCommandLeaf, "version", StringComparison.OrdinalIgnoreCase)))
        {
            return int.MinValue;
        }

        if (hasLeafSurface)
        {
            score += 20;
        }

        if (!hasLeafSurface && document.Commands.Count > 0)
        {
            score -= 10;
        }

        return score;
    }

    private static IReadOnlyList<string> EnumeratePayloadCandidates(string? storedPayload, string? stdout, string? stderr)
    {
        var payloads = new List<string>();
        if (!string.IsNullOrWhiteSpace(storedPayload))
        {
            payloads.Add(storedPayload);
        }

        if (!string.IsNullOrWhiteSpace(stdout))
        {
            payloads.Add(stdout);
        }

        if (!string.IsNullOrWhiteSpace(stderr))
        {
            payloads.Add(stderr);
        }

        if (!string.IsNullOrWhiteSpace(stdout) && !string.IsNullOrWhiteSpace(stderr))
        {
            payloads.Add($"{stdout}\n{stderr}");
            payloads.Add($"{stderr}\n{stdout}");
        }

        return payloads.Distinct(StringComparer.Ordinal).ToArray();
    }
}
