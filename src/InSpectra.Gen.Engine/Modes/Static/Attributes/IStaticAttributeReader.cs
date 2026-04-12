namespace InSpectra.Gen.Engine.Modes.Static.Attributes;

using InSpectra.Gen.Engine.Modes.Static.Inspection;
using InSpectra.Gen.Engine.Modes.Static.Metadata;

/// <summary>
/// Strongly typed contract for a Static-mode attribute reader.
///
/// <para>
/// The interface intentionally lives under <c>Modes/Static/Attributes/</c> rather than in
/// <c>Contracts/Providers/</c> because its signature references Static-mode-owned types
/// (<see cref="StaticCommandDefinition"/> and <see cref="ScannedModule"/>). Promoting
/// this interface into <c>Contracts/</c> would force <c>Contracts/</c> to depend on
/// <c>Modes/</c>, which is a worse charter violation than the Tooling type-erasure it
/// would replace: <c>Contracts/</c> is the foundational layer and must stay free of any
/// <c>Modes.*</c> reference.
/// </para>
///
/// <para>
/// Specifically, <see cref="ScannedModule"/> wraps <c>dnlib.DotNet.ModuleDefMD</c>, so it
/// cannot be promoted to <c>Contracts/</c> without dragging the dnlib dependency into
/// the foundational layer. Keeping the interface here preserves both layering
/// invariants: no <c>Contracts → Modes</c> dependency and no <c>Tooling → Modes</c>
/// dependency. <c>Tooling/FrameworkDetection</c> holds the reader through
/// <see cref="object"/> type-erasure on the framework adapter and Static mode casts it
/// back to <see cref="IStaticAttributeReader"/> at the single inspection use site.
/// </para>
/// </summary>
internal interface IStaticAttributeReader
{
    IReadOnlyDictionary<string, StaticCommandDefinition> Read(IReadOnlyList<ScannedModule> modules);
}
