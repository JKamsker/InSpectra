import { defaultFeatureFlags, defaultViewerOptions } from "../boot/contracts";
import { loadFromStartupRequest } from "../data/loadSource";
import { testDocument, testXmlDoc } from "./fixtures";

describe("startup loading", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("treats inferred xmldoc.xml as optional for directory links", async () => {
    const openCliUrl = "https://example.test/viewer/data/opencli.json";
    const xmlDocUrl = "https://example.test/viewer/data/xmldoc.xml";
    const metadataUrl = "https://example.test/viewer/data/metadata.json";
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const url = input.toString();
      if (url === openCliUrl) {
        return new Response(JSON.stringify(testDocument), { status: 200 });
      }

      if (url === xmlDocUrl) {
        return new Response("", { status: 404, statusText: "Not Found" });
      }

      if (url === metadataUrl) {
        return new Response("", { status: 404, statusText: "Not Found" });
      }

      throw new Error(`Unexpected fetch: ${url}`);
    });

    vi.stubGlobal("fetch", fetchMock);

    const result = await loadFromStartupRequest({
      kind: "links",
      links: {
        openCliUrl,
        xmlDocUrl,
        directoryUrl: "https://example.test/viewer/data/",
        xmlDocIsOptional: true,
      },
      options: defaultViewerOptions(),
      features: defaultFeatureFlags(),
      source: "query",
    });

    expect(result).not.toBeNull();
    expect(result?.xmlDoc).toBeUndefined();
    expect(result?.warnings).toEqual([]);
    expect(fetchMock).toHaveBeenCalledTimes(3);
  });

  it("fails when an explicitly requested xmldoc URL cannot be loaded", async () => {
    const openCliUrl = "https://example.test/viewer/raw/opencli.json";
    const xmlDocUrl = "https://example.test/viewer/raw/xmldoc.xml";
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const url = input.toString();
      if (url === openCliUrl) {
        return new Response(JSON.stringify(testDocument), { status: 200 });
      }

      if (url === xmlDocUrl) {
        return new Response(testXmlDoc, { status: 404, statusText: "Not Found" });
      }

      throw new Error(`Unexpected fetch: ${url}`);
    });

    vi.stubGlobal("fetch", fetchMock);

    await expect(loadFromStartupRequest({
      kind: "links",
      links: {
        openCliUrl,
        xmlDocUrl,
        xmlDocIsOptional: false,
      },
      options: defaultViewerOptions(),
      features: defaultFeatureFlags(),
      source: "query",
    })).rejects.toThrow(`Failed to load ${xmlDocUrl}: 404 Not Found`);
  });

  it("overrides the visible CLI title when viewer options specify one", async () => {
    const result = await loadFromStartupRequest({
      kind: "inline",
      openCli: testDocument,
      options: {
        ...defaultViewerOptions(),
        title: "JellyfinCli",
        commandPrefix: "jf",
      },
      features: defaultFeatureFlags(),
    });

    expect(result).not.toBeNull();
    expect(result?.document.info.title).toBe("JellyfinCli");
    expect(result?.commandPrefix).toBe("jf");
  });

  it("infers package title and command prefix from sibling metadata for linked OpenCLI documents", async () => {
    const openCliUrl = "https://inspectra-data.kamsker.at/packages/jellyfincli/latest/opencli.json";
    const metadataUrl = "https://inspectra-data.kamsker.at/packages/jellyfincli/latest/metadata.json";
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const url = input.toString();
      if (url === openCliUrl) {
        return new Response(JSON.stringify(testDocument), { status: 200 });
      }

      if (url === metadataUrl) {
        return new Response(JSON.stringify({ packageId: "JellyfinCli", command: "jf" }), { status: 200 });
      }

      throw new Error(`Unexpected fetch: ${url}`);
    });

    vi.stubGlobal("fetch", fetchMock);

    const result = await loadFromStartupRequest({
      kind: "links",
      links: {
        openCliUrl,
        xmlDocIsOptional: true,
      },
      options: defaultViewerOptions(),
      features: defaultFeatureFlags(),
      source: "query",
    });

    expect(result).not.toBeNull();
    expect(result?.document.info.title).toBe("JellyfinCli");
    expect(result?.commandPrefix).toBe("jf");
  });
});
