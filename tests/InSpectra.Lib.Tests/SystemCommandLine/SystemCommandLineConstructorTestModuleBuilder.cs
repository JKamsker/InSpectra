namespace InSpectra.Lib.Tests.SystemCommandLine;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

internal static class ConstructorReaderTestModuleBuilder
{
    public static ConstructorBuilderContext CreateContext()
    {
        var module = new ModuleDefUser("ConstructorReaderTests")
        {
            Kind = ModuleKind.Dll,
        };

        var commandType = new TypeDefUser(
            "System.CommandLine",
            "Command",
            module.CorLibTypes.Object.TypeDefOrRef);
        var optionType = new TypeDefUser(
            "System.CommandLine",
            "Option",
            module.CorLibTypes.Object.TypeDefOrRef);
        var argumentType = new TypeDefUser(
            "System.CommandLine",
            "Argument",
            module.CorLibTypes.Object.TypeDefOrRef);

        module.Types.Add(commandType);
        module.Types.Add(optionType);
        module.Types.Add(argumentType);

        var optionConstructor = AddInstanceMethod(
            optionType,
            ".ctor",
            module.CorLibTypes.Void,
            isConstructor: true,
            module.CorLibTypes.String,
            module.CorLibTypes.String);
        var addOptionMethod = AddInstanceMethod(
            commandType,
            "AddOption",
            module.CorLibTypes.Void,
            parameters: [optionType.ToTypeSig()]);

        return new ConstructorBuilderContext(module, commandType, optionType, argumentType, optionConstructor, addOptionMethod);
    }

    public static TypeDefUser CreateCommandType(ModuleDefUser module, TypeDefUser commandType, string name)
    {
        var type = new TypeDefUser("Demo", name, commandType);
        module.Types.Add(type);
        return type;
    }

    public static TypeDefUser CreateDerivedSurfaceType(ModuleDefUser module, TypeDefUser baseType, string name)
    {
        var type = new TypeDefUser("Demo", name, baseType);
        module.Types.Add(type);
        return type;
    }

    public static FieldDefUser AddField(TypeDefUser owner, string name, TypeSig type)
    {
        var field = new FieldDefUser(name, new FieldSig(type), FieldAttributes.Private);
        owner.Fields.Add(field);
        return field;
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
}

internal sealed record ConstructorBuilderContext(
    ModuleDefUser Module,
    TypeDefUser CommandType,
    TypeDefUser OptionType,
    TypeDefUser ArgumentType,
    MethodDefUser OptionConstructor,
    MethodDefUser AddOptionMethod);
