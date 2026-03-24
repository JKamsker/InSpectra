export interface HashRoute {
  kind: "overview" | "command";
  commandPath?: string;
}

export function buildCommandHash(path: string): string {
  const segments = path
    .split(" ")
    .filter((segment) => segment.length > 0)
    .map((segment) => encodeURIComponent(segment));

  return `#/command/${segments.join("/")}`;
}

export function parseHashRoute(hash: string): HashRoute {
  const normalized = hash.startsWith("#") ? hash.slice(1) : hash;
  if (normalized.length === 0 || normalized === "/") {
    return { kind: "overview" };
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
