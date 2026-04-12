namespace InSpectra.Lib.Modes.Static.Inspection;

using dnlib.DotNet;

internal sealed record ScannedModule(string Path, ModuleDefMD Module) : IDisposable
{
    public void Dispose() => Module.Dispose();
}
