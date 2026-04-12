namespace InSpectra.Gen.Engine.Tests.SystemCommandLine;

using InSpectra.Gen.Engine.Tests.TestSupport;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

internal static class SystemCommandLineFactoryMethodTestModuleSupport
{
    public static FactoryBuilderContext CreateContext()
    {
        var module = new ModuleDefUser("FactoryReaderTests")
        {
            Kind = ModuleKind.Dll,
        };

        var commandType = new TypeDefUser(
            "System.CommandLine",
            "Command",
            module.CorLibTypes.Object.TypeDefOrRef);
        var rootCommandType = new TypeDefUser(
            "System.CommandLine",
            "RootCommand",
            commandType);
        var optionType = new TypeDefUser(
            "System.CommandLine",
            "Option",
            module.CorLibTypes.Object.TypeDefOrRef);
        var builderType = new TypeDefUser(
            "Demo",
            "Builders",
            module.CorLibTypes.Object.TypeDefOrRef);

        module.Types.Add(commandType);
        module.Types.Add(rootCommandType);
        module.Types.Add(optionType);
        module.Types.Add(builderType);

        var commandConstructor = AddInstanceMethod(
            commandType,
            ".ctor",
            module.CorLibTypes.Void,
            true,
            module.CorLibTypes.String,
            module.CorLibTypes.String);
        var rootCommandConstructor = AddInstanceMethod(
            rootCommandType,
            ".ctor",
            module.CorLibTypes.Void,
            true,
            module.CorLibTypes.String);
        var optionConstructor = AddInstanceMethod(
            optionType,
            ".ctor",
            module.CorLibTypes.Void,
            true,
            module.CorLibTypes.String,
            module.CorLibTypes.String);
        var addCommandMethod = AddInstanceMethod(
            commandType,
            "AddCommand",
            module.CorLibTypes.Void,
            false,
            commandType.ToTypeSig());
        var addOptionMethod = AddInstanceMethod(
            commandType,
            "AddOption",
            module.CorLibTypes.Void,
            false,
            optionType.ToTypeSig());

        return new FactoryBuilderContext(
            module,
            commandType,
            rootCommandType,
            optionType,
            builderType,
            commandConstructor,
            rootCommandConstructor,
            optionConstructor,
            addCommandMethod,
            addOptionMethod);
    }

    public static MethodDefUser CreateBuilderMethod(TypeDefUser owner, string name, TypeSig returnType)
    {
        var method = new MethodDefUser(
            name,
            MethodSig.CreateStatic(returnType),
            MethodImplAttributes.IL | MethodImplAttributes.Managed,
            MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig);
        method.Body = new CilBody { InitLocals = true };
        owner.Methods.Add(method);
        return method;
    }

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
            attributes)
        {
            Body = new CilBody(),
        };
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        owner.Methods.Add(method);
        return method;
    }

    public static TypeDefUser CreateDerivedCommandType(ModuleDefUser module, TypeDefUser baseType, string name)
    {
        var type = new TypeDefUser("Demo", name, baseType);
        module.Types.Add(type);
        return type;
    }

    public static void AddLocals(MethodDefUser method, params TypeSig[] locals)
    {
        foreach (var localType in locals)
        {
            method.Body!.Variables.Add(new Local(localType));
        }
    }

    public static void AddInstructions(MethodDefUser method, params Instruction[] instructions)
    {
        foreach (var instruction in instructions)
        {
            method.Body!.Instructions.Add(instruction);
        }
    }
}
