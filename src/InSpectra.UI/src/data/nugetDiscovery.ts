const BASE_URL = "https://raw.githubusercontent.com/JKamsker/InSpectra-Discovery/refs/heads/main/";
const INDEX_URL = `${BASE_URL}index/all.json`;

export interface DiscoveryIndex {
  schemaVersion: number;
  generatedAt: string;
  packageCount: number;
  packages: DiscoveryPackage[];
}

export interface DiscoveryPackage {
  schemaVersion: number;
  packageId: string;
  trusted: boolean;
  latestVersion: string;
  latestStatus: "ok" | "partial";
  latestPaths: DiscoveryPaths;
  versions: DiscoveryVersion[];
}

export interface DiscoveryVersion {
  version: string;
  publishedAt: string;
  evaluatedAt: string;
  status: "ok" | "partial";
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

let cachedIndex: DiscoveryIndex | null = null;

export async function fetchDiscoveryIndex(signal?: AbortSignal): Promise<DiscoveryIndex> {
  if (cachedIndex) return cachedIndex;

  const response = await fetch(INDEX_URL, { signal });
  if (!response.ok) {
    throw new Error(`Failed to load discovery index: ${response.status} ${response.statusText}`);
  }

  cachedIndex = (await response.json()) as DiscoveryIndex;
  return cachedIndex;
}

export function searchPackages(index: DiscoveryIndex, query: string): DiscoveryPackage[] {
  const q = query.toLowerCase().trim();
  if (!q) return index.packages;

  return index.packages.filter((pkg) => {
    const id = pkg.packageId.toLowerCase();
    if (id.includes(q)) return true;

    const cmd = pkg.versions[0]?.command?.toLowerCase();
    if (cmd && cmd.includes(q)) return true;

    return false;
  });
}

export function findPackageById(index: DiscoveryIndex, packageId: string): DiscoveryPackage | undefined {
  return index.packages.find((p) => p.packageId.toLowerCase() === packageId.toLowerCase());
}

export function resolvePackageUrls(
  pkg: DiscoveryPackage,
  version?: string,
): { opencliUrl: string; xmldocUrl: string } {
  const ver = version
    ? pkg.versions.find((v) => v.version === version)
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
