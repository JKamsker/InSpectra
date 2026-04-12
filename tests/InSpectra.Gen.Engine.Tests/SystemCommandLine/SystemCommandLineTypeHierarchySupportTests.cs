namespace InSpectra.Gen.Engine.Tests.SystemCommandLine;

using InSpectra.Gen.Engine.Modes.Static.Attributes.SystemCommandLine;

using dnlib.DotNet;

public sealed class SystemCommandLineTypeHierarchySupportTests
{
    [Fact]
    public void OptionResult_Is_Not_Treated_As_Option()
    {
        var module = new ModuleDefUser("TypeHierarchyTests")
        {
            Kind = ModuleKind.Dll,
        };
        var optionResultType = new TypeDefUser(
            "System.CommandLine.Parsing",
            "OptionResult",
            module.CorLibTypes.Object.TypeDefOrRef);

        Assert.False(SystemCommandLineTypeHierarchySupport.IsOptionType(optionResultType));
    }

    [Fact]
    public void ArgumentArity_Is_Not_Treated_As_Argument()
    {
        var module = new ModuleDefUser("TypeHierarchyTests")
        {
            Kind = ModuleKind.Dll,
        };
        var argumentArityType = new TypeDefUser(
            "System.CommandLine",
            "ArgumentArity",
            module.CorLibTypes.Object.TypeDefOrRef);

        Assert.False(SystemCommandLineTypeHierarchySupport.IsArgumentType(argumentArityType));
    }
}
