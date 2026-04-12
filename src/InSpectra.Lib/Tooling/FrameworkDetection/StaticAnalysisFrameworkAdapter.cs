namespace InSpectra.Lib.Tooling.FrameworkDetection;

/// <summary>
/// Type-erased carrier for a Static-mode attribute reader. The Registry keeps the reader
/// as <see cref="object"/> on purpose so that <c>Tooling/</c> has no compile-time
/// dependency on <c>Modes.Static.Attributes</c>.
///
/// <para>
/// The type erasure is intentional. A strongly-typed <c>IStaticAttributeReader</c> here
/// would require either (a) a <c>Tooling → Modes</c> dependency (forbidden by the
/// architecture charter) or (b) promoting <c>IStaticAttributeReader</c> into
/// <c>Contracts/</c>, which would in turn force a <c>Contracts → Modes</c> dependency
/// because the interface signature references <c>StaticCommandDefinition</c> and
/// <c>ScannedModule</c> (the latter wraps <c>dnlib.DotNet.ModuleDefMD</c> and therefore
/// cannot be cleanly promoted). <c>Contracts/</c> is the foundational layer and must
/// stay free of any <c>Modes.*</c> reference — a <c>Contracts → Modes</c> leak is
/// strictly worse than the current <c>object</c> erasure.
/// </para>
///
/// <para>
/// Consumers in Static mode cast <see cref="Reader"/> back to
/// <c>IStaticAttributeReader</c> at the single use site in
/// <c>StaticAnalysisAssemblyInspectionSupport</c>.
/// </para>
/// </summary>
internal sealed record StaticAnalysisFrameworkAdapter(
    string FrameworkName,
    string AssemblyName,
    object Reader);
