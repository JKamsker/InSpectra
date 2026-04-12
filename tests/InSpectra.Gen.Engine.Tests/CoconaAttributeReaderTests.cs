namespace InSpectra.Gen.Engine.Tests;

using InSpectra.Gen.Engine.Tests.TestSupport;

using dnlib.DotNet;

public sealed class CoconaAttributeReaderTests
{
    [Fact]
    public void Read_Does_Not_Promote_First_Unnamed_Method_To_Default_In_MultiCommand_Type()
    {
        using var context = CoconaModuleBuilder.Create();
        var program = context.AddCommandType("Program");
        context.AddCommandMethod(program, "Hello");
        context.AddCommandMethod(program, "Bye");

        var commands = context.ReadCommands();

        Assert.DoesNotContain(string.Empty, commands.Keys);
        Assert.Contains("hello", commands.Keys);
        Assert.Contains("bye", commands.Keys);
    }

    [Fact]
    public void Read_Nests_HasSubCommands_Children_Under_Their_Parent_Command()
    {
        using var context = CoconaModuleBuilder.Create();
        var program = context.AddCommandType("Program");
        var server = context.AddCommandType("Server");
        context.AddCommandMethod(program, "Info");
        context.AddCommandMethod(server, "Start");
        context.AddCommandMethod(server, "Stop");
        context.AddHasSubCommands(program, server);

        var commands = context.ReadCommands();

        Assert.DoesNotContain(string.Empty, commands.Keys);
        Assert.Contains("info", commands.Keys);
        Assert.Contains("server", commands.Keys);
        Assert.Contains("server start", commands.Keys);
        Assert.Contains("server stop", commands.Keys);
        Assert.DoesNotContain("start", commands.Keys);
        Assert.DoesNotContain("stop", commands.Keys);
    }

    [Fact]
    public void Read_Skips_FromService_Parameters_And_Expands_CommandParameterSet_Properties()
    {
        using var context = CoconaModuleBuilder.Create();
        var program = context.AddCommandType("Program");
        var services = context.AddPlainType("MyService");
        var parameterSet = context.AddParameterSetType("CommonParameters");
        context.AddOptionProperty(parameterSet, "Host", context.Module.CorLibTypes.String);
        context.AddArgumentProperty(parameterSet, "Path", context.Module.CorLibTypes.String);
        context.AddCommandMethod(
            program,
            "Sync",
            ("common", parameterSet.ToTypeSig(), null),
            ("service", services.ToTypeSig(), context.FromServiceAttributeConstructor));

        var commands = context.ReadCommands();
        var command = Assert.Single(commands);

        Assert.Equal(string.Empty, command.Key);
        Assert.Contains(command.Value.Options, option => option.LongName == "host");
        Assert.Contains(command.Value.Values, value => value.Name == "Path");
        Assert.DoesNotContain(command.Value.Options, option => option.PropertyName == "service");
    }

    [Fact]
    public void Read_Leaves_Explicit_Bool_Options_Optional()
    {
        using var context = CoconaModuleBuilder.Create();
        var program = context.AddCommandType("Program");
        context.AddCommandMethod(
            program,
            "Sync",
            ("force", context.Module.CorLibTypes.Boolean, context.OptionAttributeConstructor));

        var commands = context.ReadCommands();
        var command = Assert.Single(commands);
        var option = Assert.Single(command.Value.Options);

        Assert.Equal("force", option.LongName);
        Assert.False(option.IsRequired);
        Assert.True(option.IsBoolLike);
    }

    [Fact]
    public void Read_Skips_FromService_Properties_In_CommandParameterSets()
    {
        using var context = CoconaModuleBuilder.Create();
        var program = context.AddCommandType("Program");
        var parameterSet = context.AddParameterSetType("CommonParameters");
        context.AddOptionProperty(parameterSet, "Host", context.Module.CorLibTypes.String);
        var serviceProperty = context.AddOptionProperty(parameterSet, "Service", context.Module.CorLibTypes.String);
        serviceProperty.CustomAttributes.Add(new CustomAttribute(context.FromServiceAttributeConstructor));
        context.AddCommandMethod(program, "Sync", ("common", parameterSet.ToTypeSig(), null));

        var commands = context.ReadCommands();
        var command = Assert.Single(commands);

        Assert.Contains(command.Value.Options, option => option.LongName == "host");
        Assert.DoesNotContain(command.Value.Options, option => option.PropertyName == "Service");
    }
}
