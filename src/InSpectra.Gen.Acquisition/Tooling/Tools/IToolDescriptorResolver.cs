namespace InSpectra.Gen.Acquisition.Tooling.Tools;


internal interface IToolDescriptorResolver
{
    Task<ToolDescriptorResolution> ResolveAsync(
        string packageId,
        string version,
        string? commandName,
        CancellationToken cancellationToken);
}
