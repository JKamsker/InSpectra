import { buildPackageIndexUrl } from "../data/nugetDiscovery";
import { testDocument } from "./fixtures";

export function setBootstrap(payload?: unknown) {
  document.body.innerHTML = `
    <script id="inspectra-bootstrap" type="application/json">${payload ? JSON.stringify(payload) : "__INSPECTRA_BOOTSTRAP__"}</script>
  `;
}

export function setInlineBootstrap(features?: Record<string, boolean>) {
  setBootstrap({
    mode: "inline",
    openCli: testDocument,
    features,
  });
}

export function stubDiscoveryFetch() {
  const summaryIndex = createDiscoverySummaryIndex();

  vi.stubGlobal("fetch", vi.fn(async (input: RequestInfo | URL) => {
    const url = input.toString();
    if (url === "https://inspectra-data.kamsker.at/index.min.json" || url === "https://inspectra-data.kamsker.at/index.json") {
      return new Response(JSON.stringify(summaryIndex), { status: 200 });
    }

    throw new Error(`Unexpected fetch: ${url}`);
  }));
}

export function stubPackageFetch() {
  const packageDetail = createDiscoveryPackageDetail();

  vi.stubGlobal("fetch", vi.fn(async (input: RequestInfo | URL) => {
    const url = input.toString();
    if (url === buildPackageIndexUrl("Alpha.Tool")) {
      return new Response(JSON.stringify(packageDetail), { status: 200 });
    }

    if (url === "https://inspectra-data.kamsker.at/packages/alpha.tool/2.0.0/opencli.json") {
      return new Response(JSON.stringify(createPackageDocument("2.0.0", "beta")), { status: 200 });
    }

    if (url === "https://inspectra-data.kamsker.at/packages/alpha.tool/1.0.0/opencli.json") {
      return new Response(JSON.stringify(createPackageDocument("1.0.0", "alpha")), { status: 200 });
    }

    throw new Error(`Unexpected fetch: ${url}`);
  }));
}

export function stubPendingPackageIndexFetch() {
  vi.stubGlobal("fetch", vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
    const url = input.toString();
    if (url === buildPackageIndexUrl("Alpha.Tool")) {
      return new Promise<Response>((_resolve, reject) => {
        init?.signal?.addEventListener(
          "abort",
          () => reject(new DOMException("Aborted", "AbortError")),
          { once: true },
        );
      });
    }

    throw new Error(`Unexpected fetch: ${url}`);
  }));
}

export function createPackageDocument(version: string, commandName: string) {
  return {
    ...testDocument,
    info: {
      ...testDocument.info,
      title: "Alpha.Tool",
      version,
    },
    commands: [
      {
        ...testDocument.commands[0],
        name: commandName,
        path: undefined,
      },
    ],
  };
}

export function createDiscoverySummaryIndex() {
  return {
    schemaVersion: 1,
    generatedAt: "2026-03-28T00:00:00Z",
    packageCount: 1,
    packages: [{
      packageId: "Alpha.Tool",
      commandName: "alpha",
      versionCount: 2,
      latestVersion: "2.0.0",
      createdAt: "2026-03-01T00:00:00Z",
      updatedAt: "2026-04-01T00:00:00Z",
      completeness: "full" as const,
      totalDownloads: 100,
      commandCount: 10,
      commandGroupCount: 4,
    }],
  };
}

export function createDiscoveryPackageDetail() {
  return {
    schemaVersion: 1,
    packageId: "Alpha.Tool",
    trusted: true,
    totalDownloads: 100,
    latestVersion: "2.0.0",
    latestStatus: "ok" as const,
    latestPaths: {
      metadataPath: "packages/alpha.tool/2.0.0/metadata.json",
      opencliPath: "packages/alpha.tool/2.0.0/opencli.json",
      xmldocPath: null,
    },
    versions: [
      {
        version: "2.0.0",
        publishedAt: "2026-04-01T00:00:00Z",
        evaluatedAt: "2026-04-01T00:00:00Z",
        status: "ok" as const,
        command: "beta",
        timings: {
          totalMs: 100,
          installMs: 25,
          opencliMs: 25,
          xmldocMs: null,
        },
        paths: {
          metadataPath: "packages/alpha.tool/2.0.0/metadata.json",
          opencliPath: "packages/alpha.tool/2.0.0/opencli.json",
          xmldocPath: null,
        },
      },
      {
        version: "1.0.0",
        publishedAt: "2026-03-28T00:00:00Z",
        evaluatedAt: "2026-03-28T00:00:00Z",
        status: "ok" as const,
        command: "alpha",
        timings: {
          totalMs: 100,
          installMs: 25,
          opencliMs: 25,
          xmldocMs: null,
        },
        paths: {
          metadataPath: "packages/alpha.tool/1.0.0/metadata.json",
          opencliPath: "packages/alpha.tool/1.0.0/opencli.json",
          xmldocPath: null,
        },
      },
    ],
  };
}

export function createDeferredResponse() {
  let resolve!: (response: Response) => void;
  const promise = new Promise<Response>((resolver) => {
    resolve = resolver;
  });

  return { promise, resolve };
}
