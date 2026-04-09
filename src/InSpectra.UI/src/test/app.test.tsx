import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { InSpectraApp } from "../InSpectraApp";
import { buildPackageIndexUrl, resetDiscoveryCacheForTests } from "../data/nugetDiscovery";
import { testDocument, testXmlDoc } from "./fixtures";

async function renderImportInput() {
  render(<InSpectraApp />);
  return await screen.findByLabelText("OpenCLI files");
}

describe("InSpectraUI app", () => {
  beforeEach(() => {
    document.body.innerHTML =
      '<div id="inspectra-root"></div><script id="inspectra-bootstrap" type="application/json">__INSPECTRA_BOOTSTRAP__</script>';
    window.history.replaceState({}, "", "https://example.test/viewer/index.html#/");
  });

  afterEach(() => {
    resetDiscoveryCacheForTests();
    vi.unstubAllGlobals();
  });

  it("imports JSON only through the manual picker", async () => {
    const user = userEvent.setup();
    const input = await renderImportInput();
    await user.upload(
      input,
      new File([JSON.stringify(testDocument)], "opencli.json", { type: "application/json" }),
    );

    expect((await screen.findAllByText("demo")).length).toBeGreaterThan(0);
    expect((await screen.findAllByText("alpha")).length).toBeGreaterThan(0);
  });

  it("imports JSON and XML together", async () => {
    const user = userEvent.setup();
    const input = await renderImportInput();
    await user.upload(input, [
      new File([JSON.stringify(testDocument)], "opencli.json", { type: "application/json" }),
      new File([testXmlDoc], "xmldoc.xml", { type: "application/xml" }),
    ]);

    expect((await screen.findAllByText("Filled from XML.")).length).toBeGreaterThan(0);
  });

  it("applies inline bootstrap visibility options", async () => {
    document.getElementById("inspectra-bootstrap")!.textContent = JSON.stringify({
      mode: "inline",
      openCli: testDocument,
      options: { includeHidden: true, includeMetadata: true },
    });

    render(<InSpectraApp />);

    expect((await screen.findAllByText("alpha")).length).toBeGreaterThan(0);
    expect((await screen.findAllByText("secret")).length).toBeGreaterThan(0);
    expect(await screen.findByText("Assembly")).toBeInTheDocument();
  });

  it("shows a picker error when opencli.json is missing", async () => {
    const user = userEvent.setup();
    const input = await renderImportInput();
    await user.upload(input, new File([testXmlDoc], "xmldoc.xml", { type: "application/xml" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("opencli.json is required.");
  });

  it("shows a picker error when more than two files are uploaded", async () => {
    const user = userEvent.setup();
    const input = await renderImportInput();
    await user.upload(input, [
      new File([JSON.stringify(testDocument)], "opencli.json", { type: "application/json" }),
      new File([testXmlDoc], "xmldoc.xml", { type: "application/xml" }),
      new File(["{}"], "extra.json", { type: "application/json" }),
    ]);

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Import accepts one or two files: opencli.json and optional xmldoc.xml.",
    );
  });

  it("shows a dropzone error for unsupported files", async () => {
    render(<InSpectraApp />);

    fireEvent.drop(await screen.findByRole("button", { name: "Import OpenCLI snapshot" }), {
      dataTransfer: {
        files: [new File(["oops"], "notes.txt", { type: "text/plain" })],
      },
    });

    await waitFor(() => {
      expect(screen.getByRole("alert")).toHaveTextContent('Unsupported file "notes.txt".');
    });
  });

  it("uses the package command token instead of the OpenCLI title on package command routes", async () => {
    const openCliUrl = "https://inspectra-data.kamsker.at/packages/dotnet-ef/latest/opencli.json";
    const xmlDocUrl = "https://inspectra-data.kamsker.at/packages/dotnet-ef/latest/xmldoc.xml";
    const packageDocument = {
      ...testDocument,
      info: {
        ...testDocument.info,
        title: "Entity Framework Core .NET Command-line Tools",
      },
      arguments: [],
      options: [],
      commands: [
        {
          name: "dbcontext",
          aliases: [],
          options: [],
          arguments: [],
          commands: [],
          exitCodes: [],
          description: "Commands to manage DbContext types.",
          hidden: false,
          examples: ["dbcontext --json"],
          interactive: false,
          metadata: [],
        },
      ],
    };
    const packageDetail = {
      schemaVersion: 1,
      packageId: "dotnet-ef",
      trusted: false,
      totalDownloads: 0,
      latestVersion: "11.0.0",
      latestStatus: "partial",
      latestPaths: {
        metadataPath: "index/packages/dotnet-ef/latest/metadata.json",
        opencliPath: "index/packages/dotnet-ef/latest/opencli.json",
        xmldocPath: "index/packages/dotnet-ef/latest/xmldoc.xml",
      },
      versions: [{
        version: "11.0.0",
        publishedAt: "2026-03-28T00:00:00Z",
        evaluatedAt: "2026-03-28T00:00:00Z",
        status: "partial",
        command: "dotnet-ef",
        timings: {
          totalMs: 1,
          installMs: 1,
          opencliMs: 1,
          xmldocMs: 1,
        },
        paths: {
          metadataPath: "index/packages/dotnet-ef/11.0.0/metadata.json",
          opencliPath: "index/packages/dotnet-ef/latest/opencli.json",
          xmldocPath: "index/packages/dotnet-ef/latest/xmldoc.xml",
        },
      }],
    };
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const url = input.toString();
      if (url === buildPackageIndexUrl("dotnet-ef")) {
        return new Response(JSON.stringify(packageDetail), { status: 200 });
      }

      if (url === openCliUrl) {
        return new Response(JSON.stringify(packageDocument), { status: 200 });
      }

      if (url === xmlDocUrl) {
        return new Response("", { status: 404, statusText: "Not Found" });
      }

      throw new Error(`Unexpected fetch: ${url}`);
    });

    vi.stubGlobal("fetch", fetchMock);
    window.history.replaceState({}, "", "https://example.test/viewer/index.html#/pkg/dotnet-ef/command/dbcontext");

    render(<InSpectraApp />);

    expect(await screen.findByRole("button", { name: "dotnet-ef" })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Entity Framework Core .NET Command-line Tools" })).not.toBeInTheDocument();
    expect(await screen.findByText("dotnet-ef dbcontext")).toBeInTheDocument();
    expect(await screen.findByText("dotnet-ef dbcontext --json")).toBeInTheDocument();
  });

  it("loads a package even when no XML doc is published", async () => {
    const summaryIndex = {
      schemaVersion: 1,
      generatedAt: "2026-03-28T00:00:00Z",
      packageCount: 1,
      packages: [{
        packageId: "weikio-cli",
        commandName: "weikio",
        versionCount: 1,
        latestVersion: "2024.1.0-preview.37",
        createdAt: "2024-10-16T11:31:51.5370000+00:00",
        updatedAt: "2024-10-16T11:31:51.5370000+00:00",
        completeness: "full",
        totalDownloads: 9154,
        commandCount: 94,
        commandGroupCount: 37,
      }],
    };
    const packageDetail = {
      schemaVersion: 1,
      packageId: "weikio-cli",
      trusted: false,
      totalDownloads: 9154,
      latestVersion: "2024.1.0-preview.37",
      latestStatus: "ok",
      latestPaths: {
        metadataPath: "index/packages/weikio-cli/latest/metadata.json",
        opencliPath: "index/packages/weikio-cli/latest/opencli.json",
        xmldocPath: null,
      },
      versions: [{
        version: "2024.1.0-preview.37",
        publishedAt: "2024-10-16T11:31:51.5370000+00:00",
        evaluatedAt: "2026-03-31T01:57:49.7504863+00:00",
        status: "ok",
        command: "weikio",
        timings: {
          totalMs: 9478,
          installMs: 6423,
          opencliMs: null,
          xmldocMs: null,
        },
        paths: {
          metadataPath: "index/packages/weikio-cli/2024.1.0-preview.37/metadata.json",
          opencliPath: "index/packages/weikio-cli/2024.1.0-preview.37/opencli.json",
          xmldocPath: null,
          opencliSource: "startup-hook",
        },
      }],
    };
    const openCliUrl = "https://inspectra-data.kamsker.at/packages/weikio-cli/2024.1.0-preview.37/opencli.json";

    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const url = input.toString();
      if (url === "https://inspectra-data.kamsker.at/index.min.json") {
        return new Response(JSON.stringify(summaryIndex), { status: 200 });
      }

      if (url === "https://inspectra-data.kamsker.at/index.json") {
        return new Response(JSON.stringify(summaryIndex), { status: 200 });
      }

      if (url === buildPackageIndexUrl("weikio-cli")) {
        return new Response(JSON.stringify(packageDetail), { status: 200 });
      }

      if (url === openCliUrl) {
        return new Response(JSON.stringify(testDocument), { status: 200 });
      }

      throw new Error(`Unexpected fetch: ${url}`);
    });

    vi.stubGlobal("fetch", fetchMock);
    window.history.replaceState({}, "", "https://example.test/viewer/index.html#/browse/weikio-cli");

    render(<InSpectraApp />);

    expect(await screen.findByRole("heading", { name: "weikio-cli" })).toBeInTheDocument();
    await userEvent.setup().click(await screen.findByRole("button", { name: "Inspect" }));

    expect(await screen.findAllByText("alpha")).not.toHaveLength(0);
    expect(fetchMock).toHaveBeenCalledWith(openCliUrl, expect.anything());
  });

  it("loads the preview index first and hydrates with the full index in the background", async () => {
    const previewIndex = {
      schemaVersion: 1,
      generatedAt: "2026-03-28T00:00:00Z",
      packageCount: 4851,
      includedPackageCount: 200,
      packages: [{
        packageId: "Alpha.Tool",
        commandName: "alpha",
        versionCount: 1,
        latestVersion: "1.0.0",
        createdAt: "2026-03-01T00:00:00Z",
        updatedAt: "2026-03-28T00:00:00Z",
        completeness: "full",
        totalDownloads: 100,
        commandCount: 10,
        commandGroupCount: 4,
      }],
    };
    const fullIndex = {
      ...previewIndex,
      includedPackageCount: undefined,
      packages: [
        previewIndex.packages[0],
        {
          packageId: "Beta.Tool",
          commandName: "beta",
          versionCount: 1,
          latestVersion: "1.0.0",
          createdAt: "2026-03-02T00:00:00Z",
          updatedAt: "2026-03-29T00:00:00Z",
          completeness: "full",
          totalDownloads: 200,
          commandCount: 12,
          commandGroupCount: 5,
        },
      ],
    };

    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const url = input.toString();
      if (url === "https://inspectra-data.kamsker.at/index.min.json") {
        return new Response(JSON.stringify(previewIndex), { status: 200 });
      }

      if (url === "https://inspectra-data.kamsker.at/index.json") {
        return new Response(JSON.stringify(fullIndex), { status: 200 });
      }

      throw new Error(`Unexpected fetch: ${url}`);
    });

    vi.stubGlobal("fetch", fetchMock);
    window.history.replaceState({}, "", "https://example.test/viewer/index.html#/browse");

    render(<InSpectraApp />);

    expect(await screen.findByText("Alpha.Tool")).toBeInTheDocument();
    expect(screen.getByText("4851 packages (showing first 200)")).toBeInTheDocument();
    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledWith("https://inspectra-data.kamsker.at/index.min.json", expect.any(Object));
      expect(fetchMock).toHaveBeenCalledWith("https://inspectra-data.kamsker.at/index.json", expect.anything());
    }, { timeout: 5000 });
    expect(await screen.findByText("Beta.Tool", {}, { timeout: 5000 })).toBeInTheDocument();
    expect(screen.getByText("4851 packages")).toBeInTheDocument();
  });

  it("hides unreliable coverage counts for partial package analyses", async () => {
    const summaryIndex = {
      schemaVersion: 1,
      generatedAt: "2026-03-28T00:00:00Z",
      packageCount: 1,
      packages: [{
        packageId: "RegisterBot",
        commandName: "RegisterBot",
        versionCount: 1,
        latestVersion: "2.0.20",
        createdAt: "2024-06-14T16:37:04.1670000+00:00",
        updatedAt: "2026-03-28T04:13:15.4893253+00:00",
        completeness: "partial",
        totalDownloads: 7553,
        commandCount: 122222,
        commandGroupCount: 22221,
      }],
    };
    const packageDetail = {
      schemaVersion: 1,
      packageId: "RegisterBot",
      trusted: false,
      totalDownloads: 7553,
      latestVersion: "2.0.20",
      latestStatus: "partial",
      latestPaths: {
        metadataPath: "index/packages/registerbot/latest/metadata.json",
        opencliPath: "index/packages/registerbot/latest/opencli.json",
        xmldocPath: "index/packages/registerbot/latest/xmldoc.xml",
      },
      versions: [{
        version: "2.0.20",
        publishedAt: "2024-06-14T16:37:04.1670000+00:00",
        evaluatedAt: "2026-03-28T04:13:15.4893253+00:00",
        status: "partial",
        command: "RegisterBot",
        timings: {
          totalMs: 1,
          installMs: 1,
          opencliMs: 1,
          xmldocMs: 1,
        },
        paths: {
          metadataPath: "index/packages/registerbot/2.0.20/metadata.json",
          opencliPath: "index/packages/registerbot/2.0.20/opencli.json",
          xmldocPath: "index/packages/registerbot/2.0.20/xmldoc.xml",
        },
      }],
    };
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const url = input.toString();
      if (url === "https://inspectra-data.kamsker.at/index.min.json") {
        return new Response(JSON.stringify(summaryIndex), { status: 200 });
      }

      if (url === "https://inspectra-data.kamsker.at/index.json") {
        return new Response(JSON.stringify(summaryIndex), { status: 200 });
      }

      if (url === buildPackageIndexUrl("RegisterBot")) {
        return new Response(JSON.stringify(packageDetail), { status: 200 });
      }

      throw new Error(`Unexpected fetch: ${url}`);
    });

    vi.stubGlobal("fetch", fetchMock);
    window.history.replaceState({}, "", "https://example.test/viewer/index.html#/browse/RegisterBot");

    render(<InSpectraApp />);

    expect(await screen.findByRole("heading", { name: "RegisterBot" })).toBeInTheDocument();
    expect(await screen.findByText("Unavailable for partial analysis")).toBeInTheDocument();
    expect(screen.queryByText("122222 commands across 22221 groups")).not.toBeInTheDocument();
  });
});
