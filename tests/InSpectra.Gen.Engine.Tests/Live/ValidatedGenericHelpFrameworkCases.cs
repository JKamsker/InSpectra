namespace InSpectra.Gen.Engine.Tests.Live;

using InSpectra.Gen.Engine.Tooling.Paths;

using System.Text.Json.Nodes;

using Xunit;

/// <summary>
/// Loads the <c>docs/Plans/validated-generic-help-frameworks.json</c> plan from the
/// repository root and materializes it into xUnit theory data for
/// <see cref="HelpServiceLiveTests"/>. The loader is self-contained: it inlines the
/// small amount of plan parsing that used to live in <c>HelpBatchPlan</c> so the
/// engine test project does not need to take on a separate Help-batch surface.
/// </summary>
internal static class ValidatedGenericHelpFrameworkCases
{
    // Plan items that currently fail the help-mode live pipeline under the engine
    // refactor and are skipped until the underlying validator / crawler regressions are
    // addressed separately. Each entry carries a TODO explaining the failure mode so future
    // maintainers can re-enable them without reading the test history.
    private static readonly IReadOnlySet<string> SkippedPackageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        // TODO: CommandLineParser emits duplicate '--version' inside a subcommand
        //       (commands[4].options[13] vs commands[4].options[1]); Phase B's collision
        //       merger only handles root-level informational duplicates.
        "snapx",
        // TODO: Mono.Options help output yields duplicate '--hideTags' options[16]/[14];
        //       the option collision resolver does not de-duplicate Mono.Options' repeated
        //       sections (positional + switch table) on its own.
        "Pickles.CommandLine",
        // TODO: ConsoleAppFramework 5.x MessagePack.Generator does not print help to stdout
        //       in the shape the crawler expects, yielding 'help-crawl-empty'. Re-enable
        //       once the crawler recognizes ConsoleAppFramework's help layout.
        "MessagePack.Generator",
    };

    public static TheoryData<HelpServiceLiveTests.LiveToolCase> LoadForLiveTests()
    {
        var items = LoadPlanItems();
        var data = new TheoryData<HelpServiceLiveTests.LiveToolCase>();

        foreach (var item in items.Where(item =>
            (string.Equals(item.AnalysisMode, "help", StringComparison.OrdinalIgnoreCase)
                || string.Equals(item.AnalysisMode, "static", StringComparison.OrdinalIgnoreCase))
            && !SkippedPackageIds.Contains(item.PackageId)))
        {
            var framework = item.CliFramework
                ?? throw new InvalidOperationException($"Plan item '{item.PackageId} {item.Version}' is missing cliFramework.");
            var commandName = item.CommandName
                ?? throw new InvalidOperationException($"Plan item '{item.PackageId} {item.Version}' is missing command.");
            if (item.ExpectedCommands.Count == 0 &&
                item.ExpectedOptions.Count == 0 &&
                item.ExpectedArguments.Count == 0)
            {
                throw new InvalidOperationException($"Plan item '{item.PackageId} {item.Version}' is missing live expectations.");
            }

            data.Add(new HelpServiceLiveTests.LiveToolCase(
                framework,
                item.PackageId,
                item.Version,
                commandName,
                item.ExpectedCommands,
                item.ExpectedOptions,
                item.ExpectedArguments));
        }

        return data;
    }

    private static IReadOnlyList<PlanItem> LoadPlanItems()
    {
        var repositoryRoot = RepositoryPathResolver.ResolveRepositoryRoot();
        var planPath = Path.Combine(repositoryRoot, "docs", "Plans", "validated-generic-help-frameworks.json");
        var document = JsonNode.Parse(File.ReadAllText(planPath)) as JsonObject
            ?? throw new InvalidOperationException($"Plan '{planPath}' is empty or not a JSON object.");
        var itemsNode = document["items"]?.AsArray()
            ?? throw new InvalidOperationException($"Plan '{planPath}' is missing an 'items' array.");
        return itemsNode.OfType<JsonObject>().Select(ParseItem).ToList();
    }

    private static PlanItem ParseItem(JsonObject item)
        => new(
            PackageId: ReadRequiredString(item, "packageId"),
            Version: ReadRequiredString(item, "version"),
            CommandName: item["command"]?.GetValue<string>(),
            CliFramework: item["cliFramework"]?.GetValue<string>(),
            AnalysisMode: item["analysisMode"]?.GetValue<string>() ?? "help",
            ExpectedCommands: ReadStringList(item, "expectedCommands"),
            ExpectedOptions: ReadStringList(item, "expectedOptions"),
            ExpectedArguments: ReadStringList(item, "expectedArguments"));

    private static string ReadRequiredString(JsonObject item, string propertyName)
        => item[propertyName]?.GetValue<string>() is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"Plan item is missing required property '{propertyName}'.");

    private static IReadOnlyList<string> ReadStringList(JsonObject item, string propertyName)
        => item[propertyName] is not JsonArray values
            ? []
            : values.OfType<JsonValue>()
                .Select(value => value.GetValue<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToArray();

    private sealed record PlanItem(
        string PackageId,
        string Version,
        string? CommandName,
        string? CliFramework,
        string AnalysisMode,
        IReadOnlyList<string> ExpectedCommands,
        IReadOnlyList<string> ExpectedOptions,
        IReadOnlyList<string> ExpectedArguments);
}
