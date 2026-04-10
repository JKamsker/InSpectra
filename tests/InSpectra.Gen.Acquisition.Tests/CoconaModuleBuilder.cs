namespace InSpectra.Gen.Acquisition.Tests;

using InSpectra.Gen.Acquisition.StaticAnalysis.Attributes.Cocona;
using InSpectra.Gen.Acquisition.StaticAnalysis.Inspection;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

internal sealed class CoconaModuleBuilder : IDisposable
{
    private readonly string _modulePath;
    private readonly ModuleDefUser _moduleBuilder;

    private CoconaModuleBuilder(
        string modulePath,
        ModuleDefUser moduleBuilder,
        TypeDefUser optionAttributeType,
        MethodDefUser optionAttributeConstructor,
        TypeDefUser argumentAttributeType,
        MethodDefUser argumentAttributeConstructor,
        TypeDefUser hasSubCommandsAttributeType,
        MethodDefUser hasSubCommandsAttributeConstructor,
        TypeDefUser fromServiceAttributeType,
        MethodDefUser fromServiceAttributeConstructor,
        TypeDefUser parameterSetInterfaceType)
    {
        _modulePath = modulePath;
        _moduleBuilder = moduleBuilder;
        OptionAttributeType = optionAttributeType;
        OptionAttributeConstructor = optionAttributeConstructor;
        ArgumentAttributeType = argumentAttributeType;
        ArgumentAttributeConstructor = argumentAttributeConstructor;
        HasSubCommandsAttributeType = hasSubCommandsAttributeType;
        HasSubCommandsAttributeConstructor = hasSubCommandsAttributeConstructor;
        FromServiceAttributeType = fromServiceAttributeType;
        FromServiceAttributeConstructor = fromServiceAttributeConstructor;
        ParameterSetInterfaceType = parameterSetInterfaceType;
    }

    public ModuleDefUser Module => _moduleBuilder;

    public TypeDefUser OptionAttributeType { get; }

    public MethodDefUser OptionAttributeConstructor { get; }

    public TypeDefUser ArgumentAttributeType { get; }

    public MethodDefUser ArgumentAttributeConstructor { get; }

    public TypeDefUser HasSubCommandsAttributeType { get; }

    public MethodDefUser HasSubCommandsAttributeConstructor { get; }

    public TypeDefUser FromServiceAttributeType { get; }

    public MethodDefUser FromServiceAttributeConstructor { get; }

    public TypeDefUser ParameterSetInterfaceType { get; }

    public static CoconaModuleBuilder Create()
    {
        var module = new ModuleDefUser("CoconaReaderTests")
        {
            Kind = ModuleKind.Dll,
        };

        var optionAttributeType = AddAttributeType(module, "OptionAttribute");
        var optionAttributeConstructor = AddInstanceMethod(optionAttributeType, ".ctor", module.CorLibTypes.Void, isConstructor: true);
        var argumentAttributeType = AddAttributeType(module, "ArgumentAttribute");
        var argumentAttributeConstructor = AddInstanceMethod(argumentAttributeType, ".ctor", module.CorLibTypes.Void, isConstructor: true);
        var hasSubCommandsAttributeType = AddAttributeType(module, "HasSubCommandsAttribute");
        var hasSubCommandsAttributeConstructor = AddInstanceMethod(
            hasSubCommandsAttributeType,
            ".ctor",
            module.CorLibTypes.Void,
            isConstructor: true,
            new ClassSig(module.CorLibTypes.GetTypeRef("System", "Type")));
        var fromServiceAttributeType = AddAttributeType(module, "FromServiceAttribute");
        var fromServiceAttributeConstructor = AddInstanceMethod(fromServiceAttributeType, ".ctor", module.CorLibTypes.Void, isConstructor: true);
        var parameterSetInterfaceType = new TypeDefUser("Cocona", "ICommandParameterSet", null);
        module.Types.Add(parameterSetInterfaceType);

        return new CoconaModuleBuilder(
            Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.dll"),
            module,
            optionAttributeType,
            optionAttributeConstructor,
            argumentAttributeType,
            argumentAttributeConstructor,
            hasSubCommandsAttributeType,
            hasSubCommandsAttributeConstructor,
            fromServiceAttributeType,
            fromServiceAttributeConstructor,
            parameterSetInterfaceType);
    }

    public TypeDefUser AddCommandType(string name)
        => AddPlainType(name);

    public TypeDefUser AddPlainType(string name)
    {
        var type = new TypeDefUser("Demo", name, _moduleBuilder.CorLibTypes.Object.TypeDefOrRef);
        _moduleBuilder.Types.Add(type);
        return type;
    }

    public TypeDefUser AddParameterSetType(string name)
    {
        var type = AddPlainType(name);
        type.Interfaces.Add(new InterfaceImplUser(ParameterSetInterfaceType));
        return type;
    }

    public void AddCommandMethod(TypeDefUser owner, string name, params (string Name, TypeSig Type, MethodDefUser? AttributeConstructor)[] parameters)
    {
        var method = AddInstanceMethod(
            owner,
            name,
            _moduleBuilder.CorLibTypes.Void,
            parameters: parameters.Select(static parameter => parameter.Type).ToArray());
        AddParameterDefinitions(method, parameters);
        method.Body!.Instructions.Add(Instruction.Create(OpCodes.Ret));
    }

    public PropertyDefUser AddOptionProperty(TypeDefUser owner, string name, TypeSig type)
    {
        var property = AddProperty(owner, name, type);
        property.CustomAttributes.Add(new CustomAttribute(OptionAttributeConstructor));
        return property;
    }

    public void AddArgumentProperty(TypeDefUser owner, string name, TypeSig type)
    {
        var property = AddProperty(owner, name, type);
        property.CustomAttributes.Add(new CustomAttribute(ArgumentAttributeConstructor));
    }

    public void AddHasSubCommands(TypeDefUser owner, TypeDefUser subcommandType)
    {
        owner.CustomAttributes.Add(new CustomAttribute(
            HasSubCommandsAttributeConstructor,
            [new CAArgument(new ClassSig(_moduleBuilder.CorLibTypes.GetTypeRef("System", "Type")), subcommandType.ToTypeSig())]));
    }

    public IReadOnlyDictionary<string, InSpectra.Gen.Acquisition.StaticAnalysis.Models.StaticCommandDefinition> ReadCommands()
    {
        var reader = new CoconaAttributeReader();
        _moduleBuilder.Write(_modulePath);
        using var loadedModule = ModuleDefMD.Load(_modulePath);
        return reader.Read([new ScannedModule(_modulePath, loadedModule)]);
    }

    public void Dispose()
    {
        if (File.Exists(_modulePath))
        {
            File.Delete(_modulePath);
        }
    }

    private static TypeDefUser AddAttributeType(ModuleDefUser module, string name)
    {
        var type = new TypeDefUser("Cocona", name, module.CorLibTypes.GetTypeRef("System", "Attribute"));
        module.Types.Add(type);
        return type;
    }

    private static MethodDefUser AddInstanceMethod(
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
        owner.Methods.Add(method);
        return method;
    }

    private static void AddParameterDefinitions(
        MethodDefUser method,
        IReadOnlyList<(string Name, TypeSig Type, MethodDefUser? AttributeConstructor)> parameters)
    {
        for (ushort index = 0; index < parameters.Count; index++)
        {
            var parameter = parameters[index];
            var paramDef = new ParamDefUser(parameter.Name, (ushort)(index + 1));
            if (parameter.AttributeConstructor is not null)
            {
                paramDef.CustomAttributes.Add(new CustomAttribute(parameter.AttributeConstructor));
            }

            method.ParamDefs.Add(paramDef);
        }
    }

    private PropertyDefUser AddProperty(TypeDefUser owner, string name, TypeSig type)
    {
        var getter = AddInstanceMethod(owner, $"get_{name}", type);
        if (type.ElementType == ElementType.String || type.ElementType == ElementType.Class)
        {
            getter.Body!.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
        }
        else
        {
            getter.Body!.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
        }

        getter.Body!.Instructions.Add(Instruction.Create(OpCodes.Ret));
        getter.Attributes |= MethodAttributes.SpecialName;

        var setter = AddInstanceMethod(owner, $"set_{name}", _moduleBuilder.CorLibTypes.Void, parameters: [type]);
        setter.Body!.Instructions.Add(Instruction.Create(OpCodes.Ret));
        setter.Attributes |= MethodAttributes.SpecialName;

        var property = new PropertyDefUser(name, PropertySig.CreateInstance(type));
        property.GetMethods.Add(getter);
        property.SetMethods.Add(setter);
        owner.Properties.Add(property);
        return property;
    }
}
