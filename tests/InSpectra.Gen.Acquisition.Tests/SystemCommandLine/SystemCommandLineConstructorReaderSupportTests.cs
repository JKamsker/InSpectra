namespace InSpectra.Gen.Acquisition.Tests.SystemCommandLine;

using InSpectra.Gen.Acquisition.StaticAnalysis.Attributes.SystemCommandLine;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

public sealed class SystemCommandLineConstructorReaderSupportTests
{
    [Fact]
    public void ReadSurface_Keeps_Option_When_Description_Comes_From_A_Constructor_Parameter()
    {
        var context = ConstructorReaderTestModuleBuilder.CreateContext();
        var commandType = ConstructorReaderTestModuleBuilder.CreateCommandType(context.Module, context.CommandType, "ParameterizedOptionCommand");
        var constructor = ConstructorReaderTestModuleBuilder.AddInstanceMethod(
            commandType,
            ".ctor",
            context.Module.CorLibTypes.Void,
            isConstructor: true,
            context.Module.CorLibTypes.String);
        ConstructorReaderTestModuleBuilder.AddInstructions(
            constructor,
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Ldstr, "--file"),
            Instruction.Create(OpCodes.Ldarg_1),
            Instruction.Create(OpCodes.Newobj, context.OptionConstructor),
            Instruction.Create(OpCodes.Callvirt, context.AddOptionMethod),
            Instruction.Create(OpCodes.Ret));

        var surface = SystemCommandLineConstructorReaderSupport.ReadSurface(commandType);

        var option = Assert.Single(surface.Options);
        Assert.Equal("file", option.LongName);
    }

    [Fact]
    public void ReadSurface_Recognizes_Derived_Option_And_Argument_Types()
    {
        var context = ConstructorReaderTestModuleBuilder.CreateContext();
        var derivedOptionType = ConstructorReaderTestModuleBuilder.CreateDerivedSurfaceType(context.Module, context.OptionType, "DemoOption");
        var derivedArgumentType = ConstructorReaderTestModuleBuilder.CreateDerivedSurfaceType(context.Module, context.ArgumentType, "DemoArgument");
        var derivedOptionConstructor = ConstructorReaderTestModuleBuilder.AddInstanceMethod(
            derivedOptionType,
            ".ctor",
            context.Module.CorLibTypes.Void,
            isConstructor: true,
            context.Module.CorLibTypes.String,
            context.Module.CorLibTypes.String);
        var derivedArgumentConstructor = ConstructorReaderTestModuleBuilder.AddInstanceMethod(
            derivedArgumentType,
            ".ctor",
            context.Module.CorLibTypes.Void,
            isConstructor: true,
            context.Module.CorLibTypes.String,
            context.Module.CorLibTypes.String);
        var addDerivedOptionMethod = ConstructorReaderTestModuleBuilder.AddInstanceMethod(
            context.CommandType,
            "AddOption",
            context.Module.CorLibTypes.Void,
            parameters: [derivedOptionType.ToTypeSig()]);
        var addDerivedArgumentMethod = ConstructorReaderTestModuleBuilder.AddInstanceMethod(
            context.CommandType,
            "AddArgument",
            context.Module.CorLibTypes.Void,
            parameters: [derivedArgumentType.ToTypeSig()]);
        var commandType = ConstructorReaderTestModuleBuilder.CreateCommandType(context.Module, context.CommandType, "DerivedSurfaceCommand");
        var constructor = ConstructorReaderTestModuleBuilder.AddInstanceMethod(
            commandType,
            ".ctor",
            context.Module.CorLibTypes.Void,
            isConstructor: true);
        ConstructorReaderTestModuleBuilder.AddInstructions(
            constructor,
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Ldstr, "--token"),
            Instruction.Create(OpCodes.Ldstr, "Authentication token."),
            Instruction.Create(OpCodes.Newobj, derivedOptionConstructor),
            Instruction.Create(OpCodes.Callvirt, addDerivedOptionMethod),
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Ldstr, "<PATH>"),
            Instruction.Create(OpCodes.Ldstr, "Input path."),
            Instruction.Create(OpCodes.Newobj, derivedArgumentConstructor),
            Instruction.Create(OpCodes.Callvirt, addDerivedArgumentMethod),
            Instruction.Create(OpCodes.Ret));

        var surface = SystemCommandLineConstructorReaderSupport.ReadSurface(commandType);

        var option = Assert.Single(surface.Options);
        Assert.Equal("token", option.LongName);
        var argument = Assert.Single(surface.Values);
        Assert.Equal("PATH", argument.Name);
    }

    [Fact]
    public void ReadSurface_Tracks_Option_Values_Stored_In_Fields()
    {
        var context = ConstructorReaderTestModuleBuilder.CreateContext();
        var commandType = ConstructorReaderTestModuleBuilder.CreateCommandType(context.Module, context.CommandType, "FieldBackedCommand");
        var optionField = ConstructorReaderTestModuleBuilder.AddField(commandType, "_fileOption", context.OptionType.ToTypeSig());
        var constructor = ConstructorReaderTestModuleBuilder.AddInstanceMethod(
            commandType,
            ".ctor",
            context.Module.CorLibTypes.Void,
            isConstructor: true);
        ConstructorReaderTestModuleBuilder.AddInstructions(
            constructor,
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Ldstr, "--file"),
            Instruction.Create(OpCodes.Ldstr, "Input file."),
            Instruction.Create(OpCodes.Newobj, context.OptionConstructor),
            Instruction.Create(OpCodes.Stfld, optionField),
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Ldfld, optionField),
            Instruction.Create(OpCodes.Callvirt, context.AddOptionMethod),
            Instruction.Create(OpCodes.Ret));

        var surface = SystemCommandLineConstructorReaderSupport.ReadSurface(commandType);

        var option = Assert.Single(surface.Options);
        Assert.Equal("file", option.LongName);
        Assert.Equal("Input file.", option.Description);
    }

    [Fact]
    public void ReadSurface_Applies_Option_Setter_And_Fluent_Metadata()
    {
        var context = ConstructorReaderTestModuleBuilder.CreateContext();
        var optionConstructor = ConstructorReaderTestModuleBuilder.AddInstanceMethod(
            context.OptionType,
            ".ctor",
            context.Module.CorLibTypes.Void,
            isConstructor: true,
            context.Module.CorLibTypes.String);
        var setDescriptionMethod = ConstructorReaderTestModuleBuilder.AddInstanceMethod(
            context.OptionType,
            "set_Description",
            context.Module.CorLibTypes.Void,
            parameters: [context.Module.CorLibTypes.String]);
        var setRequiredMethod = ConstructorReaderTestModuleBuilder.AddInstanceMethod(
            context.OptionType,
            "set_IsRequired",
            context.Module.CorLibTypes.Void,
            parameters: [context.Module.CorLibTypes.Boolean]);
        var fromAmongMethod = ConstructorReaderTestModuleBuilder.AddInstanceMethod(
            context.OptionType,
            "FromAmong",
            context.OptionType.ToTypeSig(),
            parameters: [new SZArraySig(context.Module.CorLibTypes.String)]);
        var commandType = ConstructorReaderTestModuleBuilder.CreateCommandType(context.Module, context.CommandType, "ConfiguredOptionCommand");
        var constructor = ConstructorReaderTestModuleBuilder.AddInstanceMethod(
            commandType,
            ".ctor",
            context.Module.CorLibTypes.Void,
            isConstructor: true);
        ConstructorReaderTestModuleBuilder.AddInstructions(
            constructor,
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Ldstr, "-m"),
            Instruction.Create(OpCodes.Newobj, optionConstructor),
            Instruction.Create(OpCodes.Dup),
            Instruction.Create(OpCodes.Ldstr, "Mode to use."),
            Instruction.Create(OpCodes.Callvirt, setDescriptionMethod),
            Instruction.Create(OpCodes.Dup),
            Instruction.Create(OpCodes.Ldc_I4_1),
            Instruction.Create(OpCodes.Callvirt, setRequiredMethod),
            Instruction.Create(OpCodes.Ldc_I4_2),
            Instruction.Create(OpCodes.Newarr, context.Module.CorLibTypes.String.TypeDefOrRef),
            Instruction.Create(OpCodes.Dup),
            Instruction.Create(OpCodes.Ldc_I4_0),
            Instruction.Create(OpCodes.Ldstr, "safe"),
            Instruction.Create(OpCodes.Stelem_Ref),
            Instruction.Create(OpCodes.Dup),
            Instruction.Create(OpCodes.Ldc_I4_1),
            Instruction.Create(OpCodes.Ldstr, "fast"),
            Instruction.Create(OpCodes.Stelem_Ref),
            Instruction.Create(OpCodes.Callvirt, fromAmongMethod),
            Instruction.Create(OpCodes.Callvirt, context.AddOptionMethod),
            Instruction.Create(OpCodes.Ret));

        var surface = SystemCommandLineConstructorReaderSupport.ReadSurface(commandType);

        var option = Assert.Single(surface.Options);
        Assert.Null(option.LongName);
        Assert.Equal('m', option.ShortName);
        Assert.True(option.IsRequired);
        Assert.Equal("Mode to use.", option.Description);
        Assert.Equal(["safe", "fast"], option.AcceptedValues);
    }

    [Fact]
    public void ReadSurface_Preserves_Option_Metadata_Updated_After_Attach()
    {
        var context = ConstructorReaderTestModuleBuilder.CreateContext();
        var optionConstructor = ConstructorReaderTestModuleBuilder.AddInstanceMethod(
            context.OptionType,
            ".ctor",
            context.Module.CorLibTypes.Void,
            isConstructor: true,
            context.Module.CorLibTypes.String);
        var setDescriptionMethod = ConstructorReaderTestModuleBuilder.AddInstanceMethod(
            context.OptionType,
            "set_Description",
            context.Module.CorLibTypes.Void,
            parameters: [context.Module.CorLibTypes.String]);
        var commandType = ConstructorReaderTestModuleBuilder.CreateCommandType(context.Module, context.CommandType, "PostAttachMetadataCommand");
        var constructor = ConstructorReaderTestModuleBuilder.AddInstanceMethod(
            commandType,
            ".ctor",
            context.Module.CorLibTypes.Void,
            isConstructor: true);
        constructor.Body!.InitLocals = true;
        constructor.Body.Variables.Add(new Local(context.OptionType.ToTypeSig()));
        ConstructorReaderTestModuleBuilder.AddInstructions(
            constructor,
            Instruction.Create(OpCodes.Ldstr, "--mode"),
            Instruction.Create(OpCodes.Newobj, optionConstructor),
            Instruction.Create(OpCodes.Stloc_0),
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Ldloc_0),
            Instruction.Create(OpCodes.Callvirt, context.AddOptionMethod),
            Instruction.Create(OpCodes.Ldloc_0),
            Instruction.Create(OpCodes.Ldstr, "Mode to use."),
            Instruction.Create(OpCodes.Callvirt, setDescriptionMethod),
            Instruction.Create(OpCodes.Ret));

        var surface = SystemCommandLineConstructorReaderSupport.ReadSurface(commandType);

        var option = Assert.Single(surface.Options);
        Assert.Equal("mode", option.LongName);
        Assert.Equal("Mode to use.", option.Description);
    }
}
