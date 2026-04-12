namespace InSpectra.Lib.Tooling.Tools;

using InSpectra.Lib.Tooling.Packages;

internal sealed record ToolDescriptorResolution(
    ToolDescriptor Descriptor,
    SpectrePackageInspection Inspection);
