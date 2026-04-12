namespace InSpectra.Gen.Engine.Tests.TestSupport;

using dnlib.DotNet;

internal sealed record FactoryBuilderContext(
    ModuleDefUser Module,
    TypeDefUser CommandType,
    TypeDefUser RootCommandType,
    TypeDefUser OptionType,
    TypeDefUser BuilderType,
    MethodDefUser CommandConstructor,
    MethodDefUser RootCommandConstructor,
    MethodDefUser OptionConstructor,
    MethodDefUser AddCommandMethod,
    MethodDefUser AddOptionMethod);
