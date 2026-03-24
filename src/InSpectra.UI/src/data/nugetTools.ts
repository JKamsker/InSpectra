export interface NugetSearchResult {
  id: string;
  version: string;
  description?: string;
  authors?: string;
  totalDownloads?: number;
  versions: string[];
}

interface NugetQueryResponse {
  data?: Array<{
    id?: string;
    version?: string;
    description?: string;
    authors?: string;
    totalDownloads?: number;
    versions?: Array<{ version?: string }>;
  }>;
}

interface NugetVersionIndexResponse {
  versions?: string[];
}

const versionIndexCache = new Map<string, Promise<string[]>>();

export async function searchNugetTools(query: string, includePrerelease: boolean): Promise<NugetSearchResult[]> {
  const normalized = query.trim();
  if (!normalized) {
    return [];
  }

  const url = new URL("https://azuresearch-usnc.nuget.org/query");
  url.searchParams.set("q", normalized);
  url.searchParams.set("packageType", "DotnetTool");
  url.searchParams.set("prerelease", String(includePrerelease));
  url.searchParams.set("take", "8");

  const response = await fetch(url);
  if (!response.ok) {
    throw new Error(`NuGet search failed: ${response.status} ${response.statusText}`);
  }

  const payload = (await response.json()) as NugetQueryResponse;
  return (payload.data ?? [])
    .filter((item) => typeof item.id === "string" && typeof item.version === "string")
    .map((item) => ({
      id: item.id!,
      version: item.version!,
      description: item.description,
      authors: item.authors,
      totalDownloads: item.totalDownloads,
      versions: (item.versions ?? [])
        .map((version) => version.version)
        .filter((version): version is string => typeof version === "string"),
    }));
}

export async function fetchNugetToolVersions(id: string, includePrerelease: boolean): Promise<string[]> {
  const normalizedId = id.trim().toLowerCase();
  const cacheKey = `${normalizedId}|${includePrerelease ? "all" : "stable"}`;
  const cached = versionIndexCache.get(cacheKey);
  if (cached) {
    return cached;
  }

  const pending = loadNugetToolVersions(normalizedId, includePrerelease).catch((error: unknown) => {
    versionIndexCache.delete(cacheKey);
    throw error;
  });

  versionIndexCache.set(cacheKey, pending);
  return pending;
}

export async function downloadNugetPackage(id: string, version: string): Promise<Uint8Array> {
  const normalizedId = id.trim().toLowerCase();
  const normalizedVersion = version.trim().toLowerCase();
  const fileName = `${normalizedId}.${normalizedVersion}.nupkg`;
  const url = `https://api.nuget.org/v3-flatcontainer/${normalizedId}/${normalizedVersion}/${fileName}`;

  const response = await fetch(url);
  if (!response.ok) {
    throw new Error(`NuGet download failed: ${response.status} ${response.statusText}`);
  }

  return new Uint8Array(await response.arrayBuffer());
}

export function resetNugetToolCachesForTests(): void {
  versionIndexCache.clear();
}

async function loadNugetToolVersions(normalizedId: string, includePrerelease: boolean): Promise<string[]> {
  const url = `https://api.nuget.org/v3-flatcontainer/${normalizedId}/index.json`;
  const response = await fetch(url);
  if (!response.ok) {
    throw new Error(`NuGet versions failed: ${response.status} ${response.statusText}`);
  }

  const payload = (await response.json()) as NugetVersionIndexResponse;
  const versions = (payload.versions ?? []).filter((version): version is string => typeof version === "string");
  const filtered = includePrerelease ? versions : versions.filter((version) => !version.includes("-"));
  return filtered.reverse();
}
