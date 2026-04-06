export type HashRoute =
  | { kind: "overview" }
  | { kind: "about" }
  | { kind: "guide"; section?: string }
  | { kind: "command"; commandPath: string }
  | { kind: "browse"; packageId?: string; version?: string }
  | { kind: "package"; packageId: string; version?: string; commandPath?: string };

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

export function buildPackageHash(packageId: string, version?: string, commandPath?: string): string {
  const base = version
    ? `#/pkg/${encodeURIComponent(packageId)}/${encodeURIComponent(version)}`
    : `#/pkg/${encodeURIComponent(packageId)}`;
  if (!commandPath) return base;

  const segments = commandPath
    .split(" ")
    .filter((s) => s.length > 0)
    .map((s) => encodeURIComponent(s));

  return `${base}/command/${segments.join("/")}`;
}

export function parseHashRoute(hash: string): HashRoute {
  const normalized = hash.startsWith("#") ? hash.slice(1) : hash;
  if (normalized.length === 0 || normalized === "/") {
    return { kind: "overview" };
  }

  if (normalized === "/about") {
    return { kind: "about" };
  }

  if (normalized === "/guide" || normalized.startsWith("/guide/")) {
    const section = normalized.slice("/guide".length).replace(/^\//, "") || undefined;
    return { kind: "guide", section };
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

  if (normalized.startsWith("/pkg/")) {
    const rest = normalized.slice("/pkg/".length);
    const segments = rest.split("/").filter(Boolean);

    if (segments.length < 1) {
      return { kind: "overview" };
    }

    try {
      const packageId = decodeURIComponent(segments[0]);

      // #/pkg/{id}/command/... — no version, with command path
      if (segments[1] === "command") {
        const commandSegments = segments.slice(2);
        if (commandSegments.length > 0) {
          const commandPath = commandSegments.map((s) => decodeURIComponent(s)).join(" ");
          return { kind: "package", packageId, commandPath };
        }
        return { kind: "package", packageId };
      }

      // #/pkg/{id} — no version, use latest
      if (segments.length === 1) {
        return { kind: "package", packageId };
      }

      // "latest" is treated as no version (resolve to latest at runtime)
      const rawVersion = decodeURIComponent(segments[1]);
      const version = rawVersion.toLowerCase() === "latest" ? undefined : rawVersion;

      // Check for /command/... after packageId/version
      if (segments[2] === "command") {
        const commandSegments = segments.slice(3);
        if (commandSegments.length > 0) {
          const commandPath = commandSegments.map((s) => decodeURIComponent(s)).join(" ");
          return { kind: "package", packageId, version, commandPath };
        }
      }

      return { kind: "package", packageId, version };
    } catch {
      return { kind: "overview" };
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
