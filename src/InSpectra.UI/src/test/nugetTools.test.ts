import { afterEach, describe, expect, it, vi } from "vitest";
import { downloadNugetPackage, fetchNugetToolVersions, resetNugetToolCachesForTests } from "../data/nugetTools";

describe("nugetTools", () => {
  afterEach(() => {
    resetNugetToolCachesForTests();
    vi.unstubAllGlobals();
  });

  it("caches published version lookups by package and prerelease mode", async () => {
    const fetchMock = vi.fn(async () => ({
      ok: true,
      json: async () => ({
        versions: ["1.0.0", "1.1.0-beta.1", "1.1.0"],
      }),
    }));
    vi.stubGlobal("fetch", fetchMock);

    const stableFirst = await fetchNugetToolVersions("Demo.Tool", false);
    const stableSecond = await fetchNugetToolVersions("Demo.Tool", false);
    const prerelease = await fetchNugetToolVersions("Demo.Tool", true);

    expect(stableFirst).toEqual(["1.1.0", "1.0.0"]);
    expect(stableSecond).toEqual(["1.1.0", "1.0.0"]);
    expect(prerelease).toEqual(["1.1.0", "1.1.0-beta.1", "1.0.0"]);
    expect(fetchMock).toHaveBeenCalledTimes(2);
  });

  it("drops failed version lookups from the cache so a retry can succeed", async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce({
        ok: false,
        status: 503,
        statusText: "Service Unavailable",
      })
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          versions: ["1.0.0"],
        }),
      });
    vi.stubGlobal("fetch", fetchMock);

    await expect(fetchNugetToolVersions("Demo.Tool", false)).rejects.toThrow("NuGet versions failed: 503 Service Unavailable");
    await expect(fetchNugetToolVersions("Demo.Tool", false)).resolves.toEqual(["1.0.0"]);
    expect(fetchMock).toHaveBeenCalledTimes(2);
  });

  it("caches package downloads by normalized package and version", async () => {
    const bytes = new Uint8Array([1, 2, 3, 4]).buffer;
    const fetchMock = vi.fn(async () => ({
      ok: true,
      arrayBuffer: async () => bytes,
    }));
    vi.stubGlobal("fetch", fetchMock);

    const first = await downloadNugetPackage("Demo.Tool", "1.2.3");
    const second = await downloadNugetPackage("demo.tool", "1.2.3");

    expect(first).toEqual(new Uint8Array([1, 2, 3, 4]));
    expect(second).toEqual(new Uint8Array([1, 2, 3, 4]));
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });

  it("drops failed package downloads from the cache so a retry can succeed", async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce({
        ok: false,
        status: 502,
        statusText: "Bad Gateway",
      })
      .mockResolvedValueOnce({
        ok: true,
        arrayBuffer: async () => new Uint8Array([9]).buffer,
      });
    vi.stubGlobal("fetch", fetchMock);

    await expect(downloadNugetPackage("Demo.Tool", "1.2.3")).rejects.toThrow("NuGet download failed: 502 Bad Gateway");
    await expect(downloadNugetPackage("Demo.Tool", "1.2.3")).resolves.toEqual(new Uint8Array([9]));
    expect(fetchMock).toHaveBeenCalledTimes(2);
  });
});
