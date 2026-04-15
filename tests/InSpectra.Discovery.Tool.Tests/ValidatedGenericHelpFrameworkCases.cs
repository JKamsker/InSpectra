namespace InSpectra.Discovery.Tool.Tests;

using InSpectra.Lib.Tooling.Paths;

using InSpectra.Discovery.Tool.Analysis.Help.Models;

using Xunit;

internal static class ValidatedGenericHelpFrameworkCases
{
    public static TheoryData<HelpServiceLiveTests.LiveToolCase> LoadForLiveTests()
    {
        var data = new TheoryData<HelpServiceLiveTests.LiveToolCase>();
        foreach (var testCase in GetLiveTestCases())
        {
            data.Add(testCase);
        }

        return data;
    }

    public static TheoryData<AutoAnalysisServiceLiveTests.LiveAutoToolCase> LoadForAutoLiveTests()
    {
        var data = new TheoryData<AutoAnalysisServiceLiveTests.LiveAutoToolCase>();
        foreach (var testCase in GetAutoLiveTestCases())
        {
            data.Add(testCase);
        }

        return data;
    }

    public static IReadOnlyList<HelpServiceLiveTests.LiveToolCase> GetLiveTestCases()
    {
        var plan = LoadPlan();
        var cases = new List<HelpServiceLiveTests.LiveToolCase>();

        foreach (var item in plan.Items.Where(item =>
            string.Equals(item.AnalysisMode, "help", StringComparison.OrdinalIgnoreCase)
            || string.Equals(item.AnalysisMode, "static", StringComparison.OrdinalIgnoreCase)))
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

            cases.Add(new HelpServiceLiveTests.LiveToolCase(
                framework,
                item.PackageId,
                item.Version,
                commandName,
                item.ExpectedCommands,
                item.ExpectedOptions,
                item.ExpectedArguments));
        }

        return cases;
    }

    public static IReadOnlyList<AutoAnalysisServiceLiveTests.LiveAutoToolCase> GetAutoLiveTestCases()
    {
        var plan = LoadPlan();
        return
        [
            CreateAutoLiveTestCase(plan.Items.Single(item => string.Equals(item.PackageId, "Cake.Tool", StringComparison.OrdinalIgnoreCase))),
            CreateAutoLiveTestCase(plan.Items.Single(item => string.Equals(item.PackageId, "Husky", StringComparison.OrdinalIgnoreCase))),
        ];
    }

    private static HelpBatchPlan LoadPlan()
    {
        var repositoryRoot = RepositoryPathResolver.ResolveRepositoryRoot();
        var planPath = Path.Combine(repositoryRoot, "docs", "Plans", "validated-generic-help-frameworks.json");
        return HelpBatchPlan.Load(planPath);
    }

    private static AutoAnalysisServiceLiveTests.LiveAutoToolCase CreateAutoLiveTestCase(HelpBatchItem item)
    {
        var framework = item.CliFramework
            ?? throw new InvalidOperationException($"Plan item '{item.PackageId} {item.Version}' is missing cliFramework.");
        var commandName = item.CommandName
            ?? throw new InvalidOperationException($"Plan item '{item.PackageId} {item.Version}' is missing command.");

        return new AutoAnalysisServiceLiveTests.LiveAutoToolCase(
            framework,
            item.AnalysisMode,
            item.PackageId,
            item.Version,
            commandName,
            item.ExpectedCommands,
            item.ExpectedOptions,
            item.ExpectedArguments);
    }
}
