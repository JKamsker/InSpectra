namespace InSpectra.Gen.Acquisition.Tests.SystemCommandLine;

using InSpectra.Gen.Acquisition.StaticAnalysis.Attributes.SystemCommandLine;

public sealed class SystemCommandLineFactoryMethodReaderSupportTests
{
    [Fact]
    public void Read_Registers_Child_Options_Added_After_Attach()
    {
        var module = SystemCommandLineFactoryMethodTestModuleBuilder.CreateModuleWithPostAttachOption();

        var commands = SystemCommandLineFactoryMethodReaderSupport.Read(module);

        Assert.True(commands.TryGetValue("child", out var child));
        Assert.Contains(child.Options, option => option.LongName == "verbose");
    }

    [Fact]
    public void Read_Preserves_Option_Metadata_Updated_After_Attach()
    {
        var module = SystemCommandLineFactoryMethodTestModuleBuilder.CreateModuleWithPostAttachOptionMetadata();

        var commands = SystemCommandLineFactoryMethodReaderSupport.Read(module);

        var root = Assert.Single(commands.Values);
        var option = Assert.Single(root.Options);
        Assert.Equal("mode", option.LongName);
        Assert.Equal("Mode to use.", option.Description);
    }

    [Fact]
    public void Read_Tracks_Field_Backed_Option_In_Instance_Builder()
    {
        var module = SystemCommandLineFactoryMethodTestModuleBuilder.CreateModuleWithFieldBackedOption();

        var commands = SystemCommandLineFactoryMethodReaderSupport.Read(module);

        var root = Assert.Single(commands.Values);
        var option = Assert.Single(root.Options);
        Assert.Equal("token", option.LongName);
        Assert.Equal("Authentication token.", option.Description);
    }

    [Fact]
    public void Read_Reparents_Descendant_Keys_When_Ancestors_Attach_Later()
    {
        var module = SystemCommandLineFactoryMethodTestModuleBuilder.CreateModuleWithNestedReparenting();

        var commands = SystemCommandLineFactoryMethodReaderSupport.Read(module);

        Assert.Contains("group", commands.Keys);
        Assert.Contains("group parent", commands.Keys);
        Assert.Contains("group parent child", commands.Keys);
        Assert.DoesNotContain("parent child", commands.Keys);
    }

    [Fact]
    public void Read_Normalizes_Type_Derived_Command_Names()
    {
        var module = SystemCommandLineFactoryMethodTestModuleBuilder.CreateModuleWithTypeDerivedCommandName();

        var commands = SystemCommandLineFactoryMethodReaderSupport.Read(module);

        Assert.Contains("sync-users", commands.Keys);
        Assert.DoesNotContain("SyncUsers", commands.Keys);
    }
}
