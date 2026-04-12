namespace InSpectra.Gen.Engine.Tooling.Tools;

using InSpectra.Gen.Engine.Tooling.Packages;

internal sealed record ToolDescriptorResolution(
    ToolDescriptor Descriptor,
    SpectrePackageInspection Inspection);
