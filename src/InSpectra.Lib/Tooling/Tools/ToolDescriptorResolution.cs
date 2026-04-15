namespace InSpectra.Lib.Tooling.Tools;

using InSpectra.Lib.Tooling.Packages;

public sealed record ToolDescriptorResolution(
    ToolDescriptor Descriptor,
    SpectrePackageInspection Inspection);
