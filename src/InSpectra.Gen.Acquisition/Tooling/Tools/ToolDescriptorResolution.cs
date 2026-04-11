namespace InSpectra.Gen.Acquisition.Tooling.Tools;

using InSpectra.Gen.Acquisition.Tooling.Packages;

internal sealed record ToolDescriptorResolution(
    ToolDescriptor Descriptor,
    SpectrePackageInspection Inspection);
