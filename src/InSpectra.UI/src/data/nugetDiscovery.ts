const BASE_URL = "https://raw.githubusercontent.com/JKamsker/InSpectra-Discovery/refs/heads/main/";
const SUMMARY_INDEX_URL = `${BASE_URL}index/index.json`;
export const DEFAULT_PACKAGE_ICON_URL = "https://nuget.org/Content/gallery/img/default-package-icon-256x256.png";

export type DiscoveryStatus = "ok" | "partial";
export type DiscoveryCompleteness = "full" | "partial";

export interface DiscoverySummaryIndex {
  schemaVersion: number;
  generatedAt: string;
  packageCount: number;
  packages: DiscoveryPackageSummary[];
}

export interface DiscoveryPackageSummary {
  packageId: string;
  commandName: string;
  versionCount: number;
  latestVersion: string;
  completeness: DiscoveryCompleteness;
  packageIconUrl?: string;
  commandCount: number;
  commandGroupCount: number;
}

export interface DiscoveryPackageDetail {
  schemaVersion: number;
  packageId: string;
  trusted: boolean;
  latestVersion: string;
  latestStatus: DiscoveryStatus;
  latestPaths: DiscoveryPaths;
  versions: DiscoveryVersion[];
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
const cachedPackageDetails = new Map<string, DiscoveryPackageDetail>();

export async function fetchDiscoveryIndex(signal?: AbortSignal): Promise<DiscoverySummaryIndex> {
  if (cachedIndex) return cachedIndex;

  const response = await fetch(SUMMARY_INDEX_URL, { signal });
  if (!response.ok) {
    throw new Error(`Failed to load discovery index: ${response.status} ${response.statusText}`);
  }

  cachedIndex = (await response.json()) as DiscoverySummaryIndex;
  return cachedIndex;
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
      opencliUrl: `${BASE_URL}${ver.paths.opencliPath}`,
      xmldocUrl: `${BASE_URL}${ver.paths.xmldocPath}`,
    };
  }

  return {
    opencliUrl: `${BASE_URL}${pkg.latestPaths.opencliPath}`,
    xmldocUrl: `${BASE_URL}${pkg.latestPaths.xmldocPath}`,
  };
}

export function getPackageStatus(pkg: DiscoveryPackageSummary): DiscoveryStatus {
  return pkg.completeness === "full" ? "ok" : "partial";
}

export function buildPackageIndexUrl(packageId: string): string {
  return `${BASE_URL}index/packages/${encodeURIComponent(packageId.toLowerCase())}/index.json`;
}

export function resetDiscoveryCacheForTests() {
  cachedIndex = null;
  cachedPackageDetails.clear();
}
