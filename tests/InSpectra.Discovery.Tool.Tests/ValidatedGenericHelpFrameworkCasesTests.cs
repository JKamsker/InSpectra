namespace InSpectra.Discovery.Tool.Tests;

using InSpectra.Discovery.Tool.App.Host;

using Xunit;

public sealed class ValidatedGenericHelpFrameworkCasesTests
{
    [Fact]
    public void LoadForLiveTests_Excludes_CliFx_Items()
    {
        Runtime.Initialize();

        var cases = ValidatedGenericHelpFrameworkCases.GetLiveTestCases();

        Assert.DoesNotContain(cases, testCase =>
            string.Equals(testCase.PackageId, "Husky", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(cases, testCase =>
            string.Equals(testCase.PackageId, "Paket", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void LoadForAutoLiveTests_Uses_Current_Analysis_Mode_From_Plan()
    {
        Runtime.Initialize();

        var cases = ValidatedGenericHelpFrameworkCases.GetAutoLiveTestCases();

        Assert.Contains(cases, testCase =>
            string.Equals(testCase.PackageId, "Cake.Tool", StringComparison.OrdinalIgnoreCase)
            && string.Equals(testCase.ExpectedAnalysisMode, "native", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(cases, testCase =>
            string.Equals(testCase.PackageId, "Husky", StringComparison.OrdinalIgnoreCase)
            && string.Equals(testCase.ExpectedAnalysisMode, "clifx", StringComparison.OrdinalIgnoreCase));
    }
}
