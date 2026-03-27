import {
  buildPackageIndexUrl,
  DiscoveryPackageDetail,
  DiscoverySummaryIndex,
  fetchDiscoveryIndex,
  fetchDiscoveryPackage,
  resetDiscoveryCacheForTests,
  resolvePackageUrls,
  searchPackages,
} from "../data/nugetDiscovery";

describe("nuget discovery", () => {
  afterEach(() => {
    resetDiscoveryCacheForTests();
    vi.unstubAllGlobals();
  });

  it("loads and caches the summary index from index/index.json", async () => {
    const payload = {
      schemaVersion: 1,
      generatedAt: "2026-03-27T11:43:21Z",
      packageCount: 1,
      packages: [{
        packageId: "JellyfinCli",
        commandName: "jf",
        versionCount: 10,
        latestVersion: "0.1.19",
        createdAt: "2026-03-13T11:49:23.673+01:00",
        updatedAt: "2026-03-27T02:40:52.81+01:00",
        completeness: "full",
        totalDownloads: 740,
        commandCount: 153,
        commandGroupCount: 40,
      }],
    };

    const fetchMock = vi.fn(async () => new Response(JSON.stringify(payload), { status: 200 }));
    vi.stubGlobal("fetch", fetchMock);

    const first = await fetchDiscoveryIndex();
    const second = await fetchDiscoveryIndex();

    expect(first).toEqual(payload);
    expect(second).toBe(first);
    expect(fetchMock).toHaveBeenCalledTimes(1);
    expect(fetchMock).toHaveBeenCalledWith(
      "https://raw.githubusercontent.com/JKamsker/InSpectra-Discovery/refs/heads/main/index/index.json",
      { signal: undefined },
    );
  });

  it("loads package details from the per-package index and caches by lowercase id", async () => {
    const payload = {
      schemaVersion: 1,
      packageId: "JellyfinCli",
      trusted: false,
      totalDownloads: 740,
      links: {
        nuget: "https://www.nuget.org/packages/JellyfinCli",
        project: "https://example.test/jellyfincli",
        source: "https://github.com/example/jellyfincli",
      },
      latestVersion: "0.1.19",
      latestStatus: "ok",
      latestPaths: {
        metadataPath: "index/packages/jellyfincli/latest/metadata.json",
        opencliPath: "index/packages/jellyfincli/latest/opencli.json",
        xmldocPath: "index/packages/jellyfincli/latest/xmldoc.xml",
      },
      versions: [{
        version: "0.1.19",
        publishedAt: "2026-03-27T01:40:52.8100000+00:00",
        evaluatedAt: "2026-03-27T04:03:27.2315150+00:00",
        status: "ok",
        command: "jf",
        timings: {
          totalMs: 8358,
          installMs: 1634,
          opencliMs: 721,
          xmldocMs: 462,
        },
        paths: {
          metadataPath: "index/packages/jellyfincli/0.1.19/metadata.json",
          opencliPath: "index/packages/jellyfincli/0.1.19/opencli.json",
          xmldocPath: "index/packages/jellyfincli/0.1.19/xmldoc.xml",
        },
      }],
    } as const;

    const fetchMock = vi.fn(async () => new Response(JSON.stringify(payload), { status: 200 }));
    vi.stubGlobal("fetch", fetchMock);

    const first = await fetchDiscoveryPackage("JellyfinCli");
    const second = await fetchDiscoveryPackage("jellyfincli");

    expect(first).toEqual(payload);
    expect(second).toBe(first);
    expect(fetchMock).toHaveBeenCalledTimes(1);
    expect(fetchMock).toHaveBeenCalledWith(buildPackageIndexUrl("JellyfinCli"), { signal: undefined });
  });

  it("searches the summary index by package id and command name", () => {
    const index: DiscoverySummaryIndex = {
      schemaVersion: 1,
      generatedAt: "2026-03-27T11:43:21Z",
      packageCount: 2,
      packages: [
        {
          packageId: "JellyfinCli",
          commandName: "jf",
          versionCount: 10,
          latestVersion: "0.1.19",
          createdAt: "2026-03-13T11:49:23.673+01:00",
          updatedAt: "2026-03-27T02:40:52.81+01:00",
          completeness: "full",
          totalDownloads: 740,
          commandCount: 153,
          commandGroupCount: 40,
        },
        {
          packageId: "CodexD",
          commandName: "codex-d",
          versionCount: 20,
          latestVersion: "0.0.35",
          createdAt: "2025-08-01T10:00:00+02:00",
          updatedAt: "2026-03-20T12:00:00+01:00",
          completeness: "full",
          totalDownloads: 1200,
          commandCount: 26,
          commandGroupCount: 7,
        },
      ],
    };

    expect(searchPackages(index, "jelly")).toEqual([index.packages[0]]);
    expect(searchPackages(index, "codex")).toEqual([index.packages[1]]);
    expect(searchPackages(index, "jf")).toEqual([index.packages[0]]);
  });

  it("resolves latest and explicit version URLs from package details", () => {
    const pkg: DiscoveryPackageDetail = {
      schemaVersion: 1,
      packageId: "JellyfinCli",
      trusted: false,
      totalDownloads: 740,
      links: {
        nuget: "https://www.nuget.org/packages/JellyfinCli",
        project: "https://example.test/jellyfincli",
        source: "https://github.com/example/jellyfincli",
      },
      latestVersion: "0.1.19",
      latestStatus: "ok",
      latestPaths: {
        metadataPath: "index/packages/jellyfincli/latest/metadata.json",
        opencliPath: "index/packages/jellyfincli/latest/opencli.json",
        xmldocPath: "index/packages/jellyfincli/latest/xmldoc.xml",
      },
      versions: [{
        version: "0.1.19",
        publishedAt: "2026-03-27T01:40:52.8100000+00:00",
        evaluatedAt: "2026-03-27T04:03:27.2315150+00:00",
        status: "ok",
        command: "jf",
        timings: {
          totalMs: 8358,
          installMs: 1634,
          opencliMs: 721,
          xmldocMs: 462,
        },
        paths: {
          metadataPath: "index/packages/jellyfincli/0.1.19/metadata.json",
          opencliPath: "index/packages/jellyfincli/0.1.19/opencli.json",
          xmldocPath: "index/packages/jellyfincli/0.1.19/xmldoc.xml",
        },
      }],
    };

    expect(resolvePackageUrls(pkg)).toEqual({
      opencliUrl: "https://raw.githubusercontent.com/JKamsker/InSpectra-Discovery/refs/heads/main/index/packages/jellyfincli/latest/opencli.json",
      xmldocUrl: "https://raw.githubusercontent.com/JKamsker/InSpectra-Discovery/refs/heads/main/index/packages/jellyfincli/latest/xmldoc.xml",
    });
    expect(resolvePackageUrls(pkg, "0.1.19")).toEqual({
      opencliUrl: "https://raw.githubusercontent.com/JKamsker/InSpectra-Discovery/refs/heads/main/index/packages/jellyfincli/0.1.19/opencli.json",
      xmldocUrl: "https://raw.githubusercontent.com/JKamsker/InSpectra-Discovery/refs/heads/main/index/packages/jellyfincli/0.1.19/xmldoc.xml",
    });
  });
});
