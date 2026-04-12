namespace InSpectra.Gen.Engine.Tooling.DocumentPipeline.Options.Collisions;

using System.Text.Json.Nodes;

internal static class OpenCliOptionStandaloneAliasCollisionSupport
{
    public static bool TryResolve(
        JsonObject leftOption,
        JsonObject rightOption,
        IReadOnlySet<string> rightTokens,
        out OpenCliOptionCollisionEntry resolvedEntry)
    {
        resolvedEntry = CreateEntry(leftOption);
        if (MatchesStandaloneAlias(leftOption, rightTokens))
        {
            resolvedEntry = CreateEntry(OpenCliOptionSupport.MergeOptions(rightOption, leftOption));
            return true;
        }

        var leftTokens = OpenCliOptionSupport.GetOptionTokens(leftOption);
        if (MatchesStandaloneAlias(rightOption, leftTokens))
        {
            resolvedEntry = CreateEntry(OpenCliOptionSupport.MergeOptions(leftOption, rightOption));
            return true;
        }

        return false;
    }

    private static bool MatchesStandaloneAlias(JsonObject standaloneCandidate, IReadOnlySet<string> richerTokens)
    {
        var standaloneName = standaloneCandidate["name"]?.GetValue<string>();
        return OpenCliOptionSupport.IsStandaloneAliasOption(standaloneCandidate)
            && !string.IsNullOrWhiteSpace(standaloneName)
            && richerTokens.Contains(standaloneName);
    }

    private static OpenCliOptionCollisionEntry CreateEntry(JsonObject option)
        => new(option, OpenCliOptionSupport.GetOptionTokens(option));
}
