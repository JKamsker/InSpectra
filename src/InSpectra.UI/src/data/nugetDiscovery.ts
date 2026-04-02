const DISCOVERY_DATA_BASE_URL = "https://inspectra-data.kamsker.at/";
const SUMMARY_INDEX_URL = `${DISCOVERY_DATA_BASE_URL}index.json`;
const SUMMARY_INDEX_MIN_URL = `${DISCOVERY_DATA_BASE_URL}index.min.json`;
export const DEFAULT_PACKAGE_ICON_URL = "https://nuget.org/Content/gallery/img/default-package-icon-256x256.png";

export type DiscoveryStatus = "ok" | "partial";
export type DiscoveryCompleteness = "full" | "partial";

export interface DiscoverySummaryIndex {
  schemaVersion: number;
  generatedAt: string;
  packageCount: number;
  includedPackageCount?: number;
  packages: DiscoveryPackageSummary[];
}

export interface DiscoveryPackageSummary {
  packageId: string;
  commandName: string;
  versionCount: number;
  latestVersion: string;
  createdAt: string;
  updatedAt: string;
  completeness: DiscoveryCompleteness;
  packageIconUrl?: string;
  totalDownloads: number;
  commandCount: number;
  commandGroupCount: number;
  cliFramework?: string;
}

export interface DiscoveryPackageDetail {
  schemaVersion: number;
  packageId: string;
  trusted: boolean;
  totalDownloads: number;
  links?: DiscoveryPackageLinks;
  latestVersion: string;
  latestStatus: DiscoveryStatus;
  latestPaths: DiscoveryPaths;
  versions: DiscoveryVersion[];
}

export interface DiscoveryPackageLinks {
  nuget?: string;
  project?: string;
  source?: string;
}

export interface DiscoveryVersion {
  version: string;
  publishedAt: string;
  evaluatedAt: string;
  status: DiscoveryStatus;
  command: string;
  timings: {
    totalMs: number;
    installMs: number;
    opencliMs: number;
    xmldocMs: number;
  };
  paths: DiscoveryPaths & {
    opencliSource?: string;
  };
}

export interface DiscoveryPaths {
  metadataPath: string;
  opencliPath: string;
  xmldocPath: string;
}

let cachedIndex: DiscoverySummaryIndex | null = null;
let cachedIndexPreview: DiscoverySummaryIndex | null = null;
const cachedPackageDetails = new Map<string, DiscoveryPackageDetail>();

export async function fetchDiscoveryIndex(signal?: AbortSignal): Promise<DiscoverySummaryIndex> {
  if (cachedIndex) return cachedIndex;

  const response = await fetch(SUMMARY_INDEX_URL, { signal });
  if (!response.ok) {
    throw new Error(`Failed to load discovery index: ${response.status} ${response.statusText}`);
  }

  cachedIndex = (await response.json()) as DiscoverySummaryIndex;
  cachedIndexPreview = cachedIndex;
  return cachedIndex;
}

export async function fetchDiscoveryIndexPreview(signal?: AbortSignal): Promise<DiscoverySummaryIndex> {
  if (cachedIndex) return cachedIndex;
  if (cachedIndexPreview) return cachedIndexPreview;

  const response = await fetch(SUMMARY_INDEX_MIN_URL, { signal });
  if (!response.ok) {
    throw new Error(`Failed to load discovery index preview: ${response.status} ${response.statusText}`);
  }

  cachedIndexPreview = (await response.json()) as DiscoverySummaryIndex;
  return cachedIndexPreview;
}

export async function fetchDiscoveryPackage(packageId: string, signal?: AbortSignal): Promise<DiscoveryPackageDetail> {
  const cacheKey = packageId.toLowerCase();
  const cached = cachedPackageDetails.get(cacheKey);
  if (cached) return cached;

  const response = await fetch(buildPackageIndexUrl(packageId), { signal });
  if (!response.ok) {
    throw new Error(`Failed to load package index for ${packageId}: ${response.status} ${response.statusText}`);
  }

  const pkg = (await response.json()) as DiscoveryPackageDetail;
  cachedPackageDetails.set(cacheKey, pkg);
  return pkg;
}

export function searchPackages(index: DiscoverySummaryIndex, query: string): DiscoveryPackageSummary[] {
  const q = query.toLowerCase().trim();
  if (!q) return index.packages;

  return index.packages.filter((pkg) => {
    if (pkg.packageId.toLowerCase().includes(q)) return true;
    return pkg.commandName.toLowerCase().includes(q);
  });
}

export function findPackageSummaryById(
  index: DiscoverySummaryIndex,
  packageId: string,
): DiscoveryPackageSummary | undefined {
  return index.packages.find((pkg) => pkg.packageId.toLowerCase() === packageId.toLowerCase());
}

export function resolvePackageUrls(
  pkg: DiscoveryPackageDetail,
  version?: string,
): { opencliUrl: string; xmldocUrl: string } {
  const ver = version
    ? pkg.versions.find((candidate) => candidate.version === version)
    : undefined;

  if (ver) {
    return {
      opencliUrl: buildDiscoveryAssetUrl(ver.paths.opencliPath),
      xmldocUrl: buildDiscoveryAssetUrl(ver.paths.xmldocPath),
    };
  }

  return {
    opencliUrl: buildDiscoveryAssetUrl(pkg.latestPaths.opencliPath),
    xmldocUrl: buildDiscoveryAssetUrl(pkg.latestPaths.xmldocPath),
  };
}

export function getPackageStatus(pkg: DiscoveryPackageSummary): DiscoveryStatus {
  return pkg.completeness === "full" ? "ok" : "partial";
}

export function buildPackageIndexUrl(packageId: string): string {
  return `${DISCOVERY_DATA_BASE_URL}packages/${encodeURIComponent(packageId.toLowerCase())}/index.json`;
}

export function resetDiscoveryCacheForTests() {
  cachedIndex = null;
  cachedIndexPreview = null;
  cachedPackageDetails.clear();
}

function buildDiscoveryAssetUrl(path: string): string {
  if (/^https?:\/\//i.test(path)) {
    return path;
  }

  const normalizedPath = path
    .replace(/^\/+/, "")
    .replace(/^index\//i, "");

  return `${DISCOVERY_DATA_BASE_URL}${normalizedPath}`;
}
