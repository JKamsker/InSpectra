namespace InSpectra.Gen.Engine.Tests.SystemCommandLine;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

internal static class SystemCommandLineFactoryMethodTestModuleBuilder
{
    public static ModuleDef CreateModuleWithPostAttachOption()
    {
        var context = SystemCommandLineFactoryMethodTestModuleSupport.CreateContext();
        var method = SystemCommandLineFactoryMethodTestModuleSupport.CreateBuilderMethod(
            context.BuilderType,
            "CreateRoot",
            context.RootCommandType.ToTypeSig());
        SystemCommandLineFactoryMethodTestModuleSupport.AddLocals(
            method,
            context.RootCommandType.ToTypeSig(),
            context.CommandType.ToTypeSig());
        SystemCommandLineFactoryMethodTestModuleSupport.AddInstructions(
            method,
            Instruction.Create(OpCodes.Ldstr, "Demo root"),
            Instruction.Create(OpCodes.Newobj, context.RootCommandConstructor),
            Instruction.Create(OpCodes.Stloc_0),
            Instruction.Create(OpCodes.Ldstr, "child"),
            Instruction.Create(OpCodes.Ldstr, "Child command"),
            Instruction.Create(OpCodes.Newobj, context.CommandConstructor),
            Instruction.Create(OpCodes.Stloc_1),
            Instruction.Create(OpCodes.Ldloc_0),
            Instruction.Create(OpCodes.Ldloc_1),
            Instruction.Create(OpCodes.Callvirt, context.AddCommandMethod),
            Instruction.Create(OpCodes.Ldloc_1),
            Instruction.Create(OpCodes.Ldstr, "--verbose"),
            Instruction.Create(OpCodes.Ldstr, "Verbose output."),
            Instruction.Create(OpCodes.Newobj, context.OptionConstructor),
            Instruction.Create(OpCodes.Callvirt, context.AddOptionMethod),
            Instruction.Create(OpCodes.Ldloc_0),
            Instruction.Create(OpCodes.Ret));
        return context.Module;
    }

    public static ModuleDef CreateModuleWithNestedReparenting()
    {
        var context = SystemCommandLineFactoryMethodTestModuleSupport.CreateContext();
        var method = SystemCommandLineFactoryMethodTestModuleSupport.CreateBuilderMethod(
            context.BuilderType,
            "CreateRoot",
            context.RootCommandType.ToTypeSig());
        SystemCommandLineFactoryMethodTestModuleSupport.AddLocals(
            method,
            context.RootCommandType.ToTypeSig(),
            context.CommandType.ToTypeSig(),
            context.CommandType.ToTypeSig(),
            context.CommandType.ToTypeSig());
        SystemCommandLineFactoryMethodTestModuleSupport.AddInstructions(
            method,
            Instruction.Create(OpCodes.Ldstr, "Demo root"),
            Instruction.Create(OpCodes.Newobj, context.RootCommandConstructor),
            Instruction.Create(OpCodes.Stloc_0),
            Instruction.Create(OpCodes.Ldstr, "group"),
            Instruction.Create(OpCodes.Ldstr, "Group command"),
            Instruction.Create(OpCodes.Newobj, context.CommandConstructor),
            Instruction.Create(OpCodes.Stloc_1),
            Instruction.Create(OpCodes.Ldstr, "parent"),
            Instruction.Create(OpCodes.Ldstr, "Parent command"),
            Instruction.Create(OpCodes.Newobj, context.CommandConstructor),
            Instruction.Create(OpCodes.Stloc_2),
            Instruction.Create(OpCodes.Ldstr, "child"),
            Instruction.Create(OpCodes.Ldstr, "Child command"),
            Instruction.Create(OpCodes.Newobj, context.CommandConstructor),
            Instruction.Create(OpCodes.Stloc_3),
            Instruction.Create(OpCodes.Ldloc_2),
            Instruction.Create(OpCodes.Ldloc_3),
            Instruction.Create(OpCodes.Callvirt, context.AddCommandMethod),
            Instruction.Create(OpCodes.Ldloc_1),
            Instruction.Create(OpCodes.Ldloc_2),
            Instruction.Create(OpCodes.Callvirt, context.AddCommandMethod),
            Instruction.Create(OpCodes.Ldloc_0),
            Instruction.Create(OpCodes.Ldloc_1),
            Instruction.Create(OpCodes.Callvirt, context.AddCommandMethod),
            Instruction.Create(OpCodes.Ldloc_0),
            Instruction.Create(OpCodes.Ret));
        return context.Module;
    }

    public static ModuleDef CreateModuleWithPostAttachOptionMetadata()
    {
        var context = SystemCommandLineFactoryMethodTestModuleSupport.CreateContext();
        var optionConstructor = SystemCommandLineFactoryMethodTestModuleSupport.AddInstanceMethod(
            context.OptionType,
            ".ctor",
            context.Module.CorLibTypes.Void,
            true,
            context.Module.CorLibTypes.String);
        var setDescriptionMethod = SystemCommandLineFactoryMethodTestModuleSupport.AddInstanceMethod(
            context.OptionType,
            "set_Description",
            context.Module.CorLibTypes.Void,
            parameters: [context.Module.CorLibTypes.String]);
        var method = SystemCommandLineFactoryMethodTestModuleSupport.CreateBuilderMethod(
            context.BuilderType,
            "CreateRoot",
            context.RootCommandType.ToTypeSig());
        SystemCommandLineFactoryMethodTestModuleSupport.AddLocals(
            method,
            context.RootCommandType.ToTypeSig(),
            context.OptionType.ToTypeSig());
        SystemCommandLineFactoryMethodTestModuleSupport.AddInstructions(
            method,
            Instruction.Create(OpCodes.Ldstr, "Demo root"),
            Instruction.Create(OpCodes.Newobj, context.RootCommandConstructor),
            Instruction.Create(OpCodes.Stloc_0),
            Instruction.Create(OpCodes.Ldstr, "--mode"),
            Instruction.Create(OpCodes.Newobj, optionConstructor),
            Instruction.Create(OpCodes.Stloc_1),
            Instruction.Create(OpCodes.Ldloc_0),
            Instruction.Create(OpCodes.Ldloc_1),
            Instruction.Create(OpCodes.Callvirt, context.AddOptionMethod),
            Instruction.Create(OpCodes.Ldloc_1),
            Instruction.Create(OpCodes.Ldstr, "Mode to use."),
            Instruction.Create(OpCodes.Callvirt, setDescriptionMethod),
            Instruction.Create(OpCodes.Ldloc_0),
            Instruction.Create(OpCodes.Ret));

        return context.Module;
    }

    public static ModuleDef CreateModuleWithFieldBackedOption()
    {
        var context = SystemCommandLineFactoryMethodTestModuleSupport.CreateContext();
        var optionField = new FieldDefUser(
            "_tokenOption",
            new FieldSig(context.OptionType.ToTypeSig()),
            FieldAttributes.Private);
        context.BuilderType.Fields.Add(optionField);
        var method = SystemCommandLineFactoryMethodTestModuleSupport.AddInstanceMethod(
            context.BuilderType,
            "Build",
            context.RootCommandType.ToTypeSig());
        SystemCommandLineFactoryMethodTestModuleSupport.AddLocals(method, context.RootCommandType.ToTypeSig());
        SystemCommandLineFactoryMethodTestModuleSupport.AddInstructions(
            method,
            Instruction.Create(OpCodes.Ldstr, "Demo root"),
            Instruction.Create(OpCodes.Newobj, context.RootCommandConstructor),
            Instruction.Create(OpCodes.Stloc_0),
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Ldstr, "--token"),
            Instruction.Create(OpCodes.Ldstr, "Authentication token."),
            Instruction.Create(OpCodes.Newobj, context.OptionConstructor),
            Instruction.Create(OpCodes.Stfld, optionField),
            Instruction.Create(OpCodes.Ldloc_0),
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Ldfld, optionField),
            Instruction.Create(OpCodes.Callvirt, context.AddOptionMethod),
            Instruction.Create(OpCodes.Ldloc_0),
            Instruction.Create(OpCodes.Ret));

        return context.Module;
    }

    public static ModuleDef CreateModuleWithTypeDerivedCommandName()
    {
        var context = SystemCommandLineFactoryMethodTestModuleSupport.CreateContext();
        var derivedCommandType = SystemCommandLineFactoryMethodTestModuleSupport.CreateDerivedCommandType(
            context.Module,
            context.CommandType,
            "SyncUsersCommand");
        var derivedCommandConstructor = SystemCommandLineFactoryMethodTestModuleSupport.AddInstanceMethod(
            derivedCommandType,
            ".ctor",
            context.Module.CorLibTypes.Void,
            isConstructor: true);

        var method = SystemCommandLineFactoryMethodTestModuleSupport.CreateBuilderMethod(
            context.BuilderType,
            "CreateRoot",
            context.RootCommandType.ToTypeSig());
        SystemCommandLineFactoryMethodTestModuleSupport.AddLocals(
            method,
            context.RootCommandType.ToTypeSig(),
            derivedCommandType.ToTypeSig());
        SystemCommandLineFactoryMethodTestModuleSupport.AddInstructions(
            method,
            Instruction.Create(OpCodes.Ldstr, "Demo root"),
            Instruction.Create(OpCodes.Newobj, context.RootCommandConstructor),
            Instruction.Create(OpCodes.Stloc_0),
            Instruction.Create(OpCodes.Newobj, derivedCommandConstructor),
            Instruction.Create(OpCodes.Stloc_1),
            Instruction.Create(OpCodes.Ldloc_0),
            Instruction.Create(OpCodes.Ldloc_1),
            Instruction.Create(OpCodes.Callvirt, context.AddCommandMethod),
            Instruction.Create(OpCodes.Ldloc_0),
            Instruction.Create(OpCodes.Ret));

        return context.Module;
    }
}
