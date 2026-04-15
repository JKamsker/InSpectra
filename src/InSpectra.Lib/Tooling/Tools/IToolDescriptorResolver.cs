namespace InSpectra.Lib.Tooling.Tools;


public interface IToolDescriptorResolver
{
    Task<ToolDescriptorResolution> ResolveAsync(
        string packageId,
        string version,
        string? commandName,
        CancellationToken cancellationToken);
}
