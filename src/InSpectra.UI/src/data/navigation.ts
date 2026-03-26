export type HashRoute =
  | { kind: "overview" }
  | { kind: "command"; commandPath: string }
  | { kind: "browse"; packageId?: string; version?: string };

export function buildCommandHash(path: string): string {
  const segments = path
    .split(" ")
    .filter((segment) => segment.length > 0)
    .map((segment) => encodeURIComponent(segment));

  return `#/command/${segments.join("/")}`;
}

export function buildBrowseHash(packageId?: string, version?: string): string {
  if (!packageId) return "#/browse";
  if (!version) return `#/browse/${encodeURIComponent(packageId)}`;
  return `#/browse/${encodeURIComponent(packageId)}/${encodeURIComponent(version)}`;
}

export function parseHashRoute(hash: string): HashRoute {
  const normalized = hash.startsWith("#") ? hash.slice(1) : hash;
  if (normalized.length === 0 || normalized === "/") {
    return { kind: "overview" };
  }

  if (normalized === "/browse" || normalized.startsWith("/browse/")) {
    const rest = normalized.slice("/browse".length);
    if (!rest || rest === "/") {
      return { kind: "browse" };
    }

    const segments = rest.slice(1).split("/").filter(Boolean);
    try {
      const packageId = decodeURIComponent(segments[0]);
      const version = segments[1] ? decodeURIComponent(segments[1]) : undefined;
      return { kind: "browse", packageId, version };
    } catch {
      return { kind: "browse" };
    }
  }

  if (!normalized.startsWith("/command/")) {
    return { kind: "overview" };
  }

  const encodedSegments = normalized.slice("/command/".length).split("/").filter(Boolean);
  if (encodedSegments.length === 0) {
    return { kind: "overview" };
  }

  try {
    const commandPath = encodedSegments.map((segment) => decodeURIComponent(segment)).join(" ");
    return { kind: "command", commandPath };
  } catch {
    return { kind: "overview" };
  }
}

export function normalizeHash(hash: string): string {
  return parseHashRoute(hash).kind === "overview" ? "#/" : hash;
}
