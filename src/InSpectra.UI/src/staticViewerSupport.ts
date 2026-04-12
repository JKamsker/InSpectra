import { FeatureFlags, ViewerOptions } from "./boot/contracts";
import { LoadedSource } from "./data/loadSource";
import { HashRoute } from "./data/navigation";
import { DiscoveryPackageDetail } from "./data/nugetDiscovery";
import { normalizeOpenCliDocument, NormalizedCliDocument } from "./data/normalize";

export interface PackageContext {
  packageId: string;
  version?: string;
  command?: string;
}

export interface ViewerToolbarRoutes {
  homeHref?: string;
  browseHref?: string;
  importHref?: string;
}

export interface ViewerState {
  document: NormalizedCliDocument;
  viewerOptions: ViewerOptions;
  warnings: string[];
  sourceLabel: string;
}

export function buildViewerState(source: LoadedSource): ViewerState {
  return {
    document: normalizeOpenCliDocument(source.document, source.options.includeHidden),
    viewerOptions: source.options,
    warnings: source.warnings,
    sourceLabel: source.label,
  };
}

export function buildToolbarRoutes(features: FeatureFlags): ViewerToolbarRoutes {
  return {
    homeHref: features.showHome ? "#/" : undefined,
    browseHref: supportsBrowseRoute(features) ? "#/browse" : undefined,
    importHref: supportsImportRoute(features) ? "#/import" : undefined,
  };
}

export function supportsBrowseRoute(features: FeatureFlags): boolean {
  return features.showHome && features.nugetBrowser;
}

export function supportsImportRoute(features: FeatureFlags): boolean {
  return features.showHome && features.packageUpload;
}

export function isSupportedStaticRoute(route: HashRoute, features: FeatureFlags): boolean {
  return (
    (route.kind === "browse" && supportsBrowseRoute(features)) ||
    (route.kind === "import" && supportsImportRoute(features)) ||
    (route.kind === "package" && supportsBrowseRoute(features))
  );
}

export function isUnsupportedStaticRoute(route: HashRoute, features: FeatureFlags): boolean {
  return (
    (route.kind === "browse" || route.kind === "import" || route.kind === "package") &&
    !isSupportedStaticRoute(route, features)
  );
}

export function isLoadedPackageRoute(route: HashRoute, packageContext: PackageContext | null): boolean {
  if (route.kind !== "package" || !packageContext) {
    return false;
  }

  return (
    packageContext.packageId.toLowerCase() === route.packageId.toLowerCase() &&
    normalizeVersion(packageContext.version) === normalizeVersion(route.version)
  );
}

export function resolvePackageCommand(pkg: DiscoveryPackageDetail, version: string): string | undefined {
  return pkg.versions.find((candidate) => candidate.version === version)?.command ?? pkg.versions[0]?.command;
}

function normalizeVersion(version: string | undefined): string {
  return (version ?? "latest").toLowerCase();
}
