namespace InSpectra.Gen.Acquisition.Tests.SystemCommandLine;

using InSpectra.Gen.Acquisition.StaticAnalysis.Attributes.SystemCommandLine;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

public sealed class SystemCommandLineAttributeReaderTests
{
    [Fact]
    public void ReadCommandType_Uses_Explicit_Command_Name_From_Base_Constructor()
    {
        var context = AttributeReaderTestModuleBuilder.CreateContext();
        var commandType = AttributeReaderTestModuleBuilder.CreateCommandType(context.Module, context.CommandBaseType, "ResetAllCommand");
        var constructor = AttributeReaderTestModuleBuilder.AddInstanceMethod(
            commandType,
            ".ctor",
            context.Module.CorLibTypes.Void,
            isConstructor: true);
        AttributeReaderTestModuleBuilder.AddInstructions(
            constructor,
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Ldstr, "reset-all"),
            Instruction.Create(OpCodes.Ldstr, "Reset everything."),
            Instruction.Create(OpCodes.Call, context.CommandBaseConstructor),
            Instruction.Create(OpCodes.Ret));

        var definition = SystemCommandLineAttributeReader.ReadCommandType(commandType);

        Assert.NotNull(definition);
        Assert.Equal("reset-all", definition.Name);
        Assert.Equal("Reset everything.", definition.Description);
    }

    [Fact]
    public void ReadCommandType_Prefers_Richer_Constructor_Metadata_Over_Field_Stubs()
    {
        var context = AttributeReaderTestModuleBuilder.CreateContext();
        var commandType = AttributeReaderTestModuleBuilder.CreateCommandType(context.Module, context.CommandBaseType, "SaveCommand");
        AttributeReaderTestModuleBuilder.AddField(commandType, "SaveOption", context.OptionBaseType.ToTypeSig());
        var constructor = AttributeReaderTestModuleBuilder.AddInstanceMethod(
            commandType,
            ".ctor",
            context.Module.CorLibTypes.Void,
            isConstructor: true);
        AttributeReaderTestModuleBuilder.AddInstructions(
            constructor,
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Ldstr, "--save"),
            Instruction.Create(OpCodes.Ldstr, "Save state."),
            Instruction.Create(OpCodes.Newobj, context.OptionConstructor),
            Instruction.Create(OpCodes.Callvirt, context.AddOptionMethod),
            Instruction.Create(OpCodes.Ret));

        var definition = SystemCommandLineAttributeReader.ReadCommandType(commandType);
        Assert.NotNull(definition);

        var option = Assert.Single(definition.Options);
        Assert.Equal("save", option.LongName);
        Assert.Equal("Save state.", option.Description);
    }

    [Fact]
    public void ReadCommandType_Merges_Short_Only_Constructor_Options_With_Member_Stubs()
    {
        var context = AttributeReaderTestModuleBuilder.CreateContext();
        var commandType = AttributeReaderTestModuleBuilder.CreateCommandType(context.Module, context.CommandBaseType, "SyncCommand");
        var modeField = new FieldDefUser("ModeOption", new FieldSig(context.OptionBaseType.ToTypeSig()), FieldAttributes.Public);
        commandType.Fields.Add(modeField);
        var shortOnlyConstructor = AttributeReaderTestModuleBuilder.AddInstanceMethod(
            context.OptionBaseType,
            ".ctor",
            context.Module.CorLibTypes.Void,
            isConstructor: true,
            context.Module.CorLibTypes.String);
        var constructor = AttributeReaderTestModuleBuilder.AddInstanceMethod(
            commandType,
            ".ctor",
            context.Module.CorLibTypes.Void,
            isConstructor: true);
        AttributeReaderTestModuleBuilder.AddInstructions(
            constructor,
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Ldstr, "-m"),
            Instruction.Create(OpCodes.Newobj, shortOnlyConstructor),
            Instruction.Create(OpCodes.Stfld, modeField),
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Ldfld, modeField),
            Instruction.Create(OpCodes.Callvirt, context.AddOptionMethod),
            Instruction.Create(OpCodes.Ret));

        var definition = SystemCommandLineAttributeReader.ReadCommandType(commandType);
        Assert.NotNull(definition);

        var option = Assert.Single(definition.Options);
        Assert.Null(option.LongName);
        Assert.Equal('m', option.ShortName);
        Assert.Equal("ModeOption", option.PropertyName);
    }

    [Fact]
    public void ReadCommandType_Recognizes_Derived_Option_And_Argument_Members_And_Reindexes_Values()
    {
        var context = AttributeReaderTestModuleBuilder.CreateContext();
        var derivedOptionType = AttributeReaderTestModuleBuilder.CreateDerivedSurfaceType(context.Module, context.OptionBaseType, "DemoOption");
        var derivedArgumentType = AttributeReaderTestModuleBuilder.CreateDerivedSurfaceType(context.Module, context.ArgumentBaseType, "DemoArgument");
        var commandType = AttributeReaderTestModuleBuilder.CreateCommandType(context.Module, context.CommandBaseType, "SyncCommand");
        AttributeReaderTestModuleBuilder.AddField(commandType, "ModeOption", derivedOptionType.ToTypeSig());
        AttributeReaderTestModuleBuilder.AddField(commandType, "SourceArgument", derivedArgumentType.ToTypeSig());
        AttributeReaderTestModuleBuilder.AddField(commandType, "TargetArgument", derivedArgumentType.ToTypeSig());

        var definition = SystemCommandLineAttributeReader.ReadCommandType(commandType);

        Assert.NotNull(definition);
        Assert.Contains(definition.Options, option => option.LongName == "mode");
        Assert.Equal(2, definition.Values.Count);
        Assert.Equal("source", definition.Values[0].Name);
        Assert.Equal(0, definition.Values[0].Index);
        Assert.Equal("target", definition.Values[1].Name);
        Assert.Equal(1, definition.Values[1].Index);
    }

    [Fact]
    public void ReadCommandType_Prefers_Richer_Command_Metadata_From_Later_Constructors()
    {
        var context = AttributeReaderTestModuleBuilder.CreateContext();
        var commandType = AttributeReaderTestModuleBuilder.CreateCommandType(context.Module, context.CommandBaseType, "SyncUsersCommand");
        var basicConstructor = AttributeReaderTestModuleBuilder.AddInstanceMethod(
            commandType,
            ".ctor",
            context.Module.CorLibTypes.Void,
            isConstructor: true);
        AttributeReaderTestModuleBuilder.AddInstructions(
            basicConstructor,
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Ldstr, "sync-users"),
            Instruction.Create(OpCodes.Call, context.CommandNameOnlyConstructor),
            Instruction.Create(OpCodes.Ret));
        var richConstructor = AttributeReaderTestModuleBuilder.AddInstanceMethod(
            commandType,
            ".ctor",
            context.Module.CorLibTypes.Void,
            isConstructor: true,
            context.Module.CorLibTypes.String);
        AttributeReaderTestModuleBuilder.AddInstructions(
            richConstructor,
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Ldstr, "sync-users"),
            Instruction.Create(OpCodes.Ldstr, "Synchronize remote users."),
            Instruction.Create(OpCodes.Call, context.CommandBaseConstructor),
            Instruction.Create(OpCodes.Ret));

        var definition = SystemCommandLineAttributeReader.ReadCommandType(commandType);

        Assert.NotNull(definition);
        Assert.Equal("sync-users", definition.Name);
        Assert.Equal("Synchronize remote users.", definition.Description);
    }

    private static class AttributeReaderTestModuleBuilder
    {
        public static BuilderContext CreateContext()
        {
            var module = new ModuleDefUser("AttributeReaderTests")
            {
                Kind = ModuleKind.Dll,
            };

            var commandBaseType = new TypeDefUser(
                "System.CommandLine",
                "Command",
                module.CorLibTypes.Object.TypeDefOrRef);
            var optionBaseType = new TypeDefUser(
                "System.CommandLine",
                "Option",
                module.CorLibTypes.Object.TypeDefOrRef);
            var argumentBaseType = new TypeDefUser(
                "System.CommandLine",
                "Argument",
                module.CorLibTypes.Object.TypeDefOrRef);

            module.Types.Add(commandBaseType);
            module.Types.Add(optionBaseType);
            module.Types.Add(argumentBaseType);

            var commandBaseConstructor = AddInstanceMethod(
                commandBaseType,
                ".ctor",
                module.CorLibTypes.Void,
                isConstructor: true,
                module.CorLibTypes.String,
                module.CorLibTypes.String);
            var commandNameOnlyConstructor = AddInstanceMethod(
                commandBaseType,
                ".ctor",
                module.CorLibTypes.Void,
                isConstructor: true,
                module.CorLibTypes.String);
            var optionConstructor = AddInstanceMethod(
                optionBaseType,
                ".ctor",
                module.CorLibTypes.Void,
                isConstructor: true,
                module.CorLibTypes.String,
                module.CorLibTypes.String);
            var addOptionMethod = AddInstanceMethod(
                commandBaseType,
                "AddOption",
                module.CorLibTypes.Void,
                parameters: [optionBaseType.ToTypeSig()]);

            return new BuilderContext(
                module,
                commandBaseType,
                optionBaseType,
                argumentBaseType,
                commandBaseConstructor,
                commandNameOnlyConstructor,
                optionConstructor,
                addOptionMethod);
        }

        public static TypeDefUser CreateCommandType(ModuleDefUser module, TypeDefUser baseType, string name)
        {
            var type = new TypeDefUser("Demo", name, baseType);
            module.Types.Add(type);
            return type;
        }

        public static TypeDefUser CreateDerivedSurfaceType(ModuleDefUser module, TypeDefUser baseType, string name)
        {
            var type = new TypeDefUser("Demo", name, baseType);
            module.Types.Add(type);
            return type;
        }

        public static void AddField(TypeDefUser owner, string name, TypeSig type)
            => owner.Fields.Add(new FieldDefUser(
                name,
                new FieldSig(type),
                FieldAttributes.Public));

        public static MethodDefUser AddInstanceMethod(
            TypeDefUser owner,
            string name,
            TypeSig returnType,
            bool isConstructor = false,
            params TypeSig[] parameters)
        {
            var attributes = MethodAttributes.Public | MethodAttributes.HideBySig;
            if (isConstructor)
            {
                attributes |= MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
            }

            var method = new MethodDefUser(
                name,
                MethodSig.CreateInstance(returnType, parameters),
                MethodImplAttributes.IL | MethodImplAttributes.Managed,
                attributes);
            method.Body = new CilBody();
            owner.Methods.Add(method);
            return method;
        }

        public static void AddInstructions(MethodDefUser method, params Instruction[] instructions)
        {
            foreach (var instruction in instructions)
            {
                method.Body!.Instructions.Add(instruction);
            }
        }

        public sealed record BuilderContext(
            ModuleDefUser Module,
            TypeDefUser CommandBaseType,
            TypeDefUser OptionBaseType,
            TypeDefUser ArgumentBaseType,
            MethodDefUser CommandBaseConstructor,
            MethodDefUser CommandNameOnlyConstructor,
            MethodDefUser OptionConstructor,
            MethodDefUser AddOptionMethod);
    }
}
