import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { InSpectraApp } from "../InSpectraApp";
import { defaultViewerOptions } from "../boot/contracts";
import { testDocument } from "./fixtures";
import { vi } from "vitest";

const { MockNugetToolProbeError, loadFromNugetTool } = vi.hoisted(() => {
  class MockNugetToolProbeError extends Error {
    constructor(
      message: string,
      readonly diagnostics: {
        status: string;
        confidence: string;
        error?: string;
        warnings: string[];
        summary?: {
          id: string;
          version: string;
          isDotnetTool: boolean;
          isSpectreCli: boolean;
          commandName?: string;
          runner?: string;
          entryPoint?: string;
          targetFramework?: string;
          hasPackagedOpenCli: boolean;
          documentSource: string;
          confidence: string;
        };
      },
    ) {
      super(message);
      this.name = "NugetToolProbeError";
    }
  }

  return {
    MockNugetToolProbeError,
    loadFromNugetTool: vi.fn(),
  };
});

vi.mock("../data/loadNugetTool", () => ({
  loadFromNugetTool,
  NugetToolProbeError: MockNugetToolProbeError,
}));

describe("InSpectraUI NuGet mode", () => {
  beforeEach(() => {
    document.body.innerHTML =
      '<div id="inspectra-root"></div><script id="inspectra-bootstrap" type="application/json">__INSPECTRA_BOOTSTRAP__</script>';
    window.history.replaceState({}, "", "https://example.test/viewer/index.html#/");
    loadFromNugetTool.mockReset();
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: string | URL) => {
        const url = String(input);
        if (url.includes("/index.json")) {
          return {
            ok: true,
            json: async () => ({
              versions: ["1.9.0", "2.0.0"],
            }),
          };
        }

        return {
          ok: true,
          json: async () => ({
            data: [
              {
                id: "Demo.Tool",
                version: "2.0.0",
                description: "Probe me",
                authors: "Kamsker",
                totalDownloads: 42,
                versions: [{ version: "2.0.0" }, { version: "1.9.0" }],
              },
            ],
          }),
        };
      }),
    );
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("searches NuGet tools and loads a generated OpenCLI document", async () => {
    loadFromNugetTool.mockResolvedValue({
      document: testDocument,
      warnings: ["Static Spectre probe."],
      options: defaultViewerOptions(),
      label: "NuGet: Demo.Tool 2.0.0",
      mode: "generated",
      probeSummary: {
        id: "Demo.Tool",
        version: "2.0.0",
        isDotnetTool: true,
        isSpectreCli: true,
        commandName: "demo",
        runner: "dotnet",
        entryPoint: "demo.dll",
        targetFramework: "net10.0",
        hasPackagedOpenCli: false,
        documentSource: "static-spectre",
        confidence: "partial",
      },
    });

    const user = userEvent.setup();
    render(<InSpectraApp />);

    await user.click(screen.getByRole("tab", { name: /nuget tool/i }));
    await user.type(screen.getByLabelText("NuGet package query"), "Demo.Tool");
    await user.click(screen.getByRole("button", { name: /search nuget/i }));

    expect(await screen.findByRole("button", { name: /demo.tool/i })).toBeInTheDocument();
    expect(await screen.findByText("Loaded 2 published versions.")).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: /inspect tool/i }));

    await waitFor(() => {
      expect(loadFromNugetTool).toHaveBeenCalledWith({ id: "Demo.Tool", version: "2.0.0" }, defaultViewerOptions());
    });

    expect(await screen.findByText("NuGet Tool Probe")).toBeInTheDocument();
    expect(screen.getAllByText("Static Spectre recovery").length).toBeGreaterThan(0);
    expect(screen.getByText("No tool code executed. The package was downloaded and inspected in the browser.")).toBeInTheDocument();
    expect(await screen.findAllByText("demo")).not.toHaveLength(0);
    expect(screen.getByText("Static Spectre probe.")).toBeInTheDocument();
  });

  it("shows probe diagnostics when a NuGet package is unsupported", async () => {
    loadFromNugetTool.mockRejectedValue(
      new MockNugetToolProbeError("The package does not bundle opencli.json.", {
        status: "unsupported",
        confidence: "unsupported",
        error: "The package does not bundle opencli.json.",
        warnings: ["No tool code was executed."],
        summary: {
          id: "Demo.Tool",
          version: "2.0.0",
          isDotnetTool: true,
          isSpectreCli: false,
          commandName: "demo",
          runner: "dotnet",
          entryPoint: "demo.dll",
          targetFramework: "net10.0",
          hasPackagedOpenCli: false,
          documentSource: "none",
          confidence: "unsupported",
        },
      }),
    );

    const user = userEvent.setup();
    render(<InSpectraApp />);

    await user.click(screen.getByRole("tab", { name: /nuget tool/i }));
    await user.type(screen.getByLabelText("NuGet package query"), "Demo.Tool");
    await user.click(screen.getByRole("button", { name: /search nuget/i }));
    expect(await screen.findByText("Loaded 2 published versions.")).toBeInTheDocument();
    await user.click(await screen.findByRole("button", { name: /inspect tool/i }));

    expect(await screen.findByText("Last Probe Attempt")).toBeInTheDocument();
    expect(screen.getByText("No document")).toBeInTheDocument();
    expect(screen.getByText("Not detected")).toBeInTheDocument();
    expect(screen.getByText("No tool code was executed.")).toBeInTheDocument();
    expect(screen.getAllByRole("alert")[0]).toHaveTextContent("The package does not bundle opencli.json.");
  });

  it("clears stale NuGet diagnostics when the user starts a new search", async () => {
    loadFromNugetTool.mockRejectedValue(
      new MockNugetToolProbeError("The package does not bundle opencli.json.", {
        status: "unsupported",
        confidence: "unsupported",
        error: "The package does not bundle opencli.json.",
        warnings: ["No tool code was executed."],
        summary: {
          id: "Demo.Tool",
          version: "2.0.0",
          isDotnetTool: true,
          isSpectreCli: false,
          commandName: "demo",
          runner: "dotnet",
          entryPoint: "demo.dll",
          targetFramework: "net10.0",
          hasPackagedOpenCli: false,
          documentSource: "none",
          confidence: "unsupported",
        },
      }),
    );

    const user = userEvent.setup();
    render(<InSpectraApp />);

    await user.click(screen.getByRole("tab", { name: /nuget tool/i }));
    await user.type(screen.getByLabelText("NuGet package query"), "Demo.Tool");
    await user.click(screen.getByRole("button", { name: /search nuget/i }));
    await user.click(await screen.findByRole("button", { name: /inspect tool/i }));

    expect(await screen.findByText("Last Probe Attempt")).toBeInTheDocument();

    await user.type(screen.getByLabelText("NuGet package query"), " again");

    expect(screen.queryByText("Last Probe Attempt")).not.toBeInTheDocument();
    expect(screen.queryByText("The package does not bundle opencli.json.")).not.toBeInTheDocument();
  });

  it("clears stale NuGet diagnostics when switching back to file import", async () => {
    loadFromNugetTool.mockRejectedValue(
      new MockNugetToolProbeError("The package does not bundle opencli.json.", {
        status: "unsupported",
        confidence: "unsupported",
        error: "The package does not bundle opencli.json.",
        warnings: ["No tool code was executed."],
        summary: {
          id: "Demo.Tool",
          version: "2.0.0",
          isDotnetTool: true,
          isSpectreCli: false,
          commandName: "demo",
          runner: "dotnet",
          entryPoint: "demo.dll",
          targetFramework: "net10.0",
          hasPackagedOpenCli: false,
          documentSource: "none",
          confidence: "unsupported",
        },
      }),
    );

    const user = userEvent.setup();
    render(<InSpectraApp />);

    await user.click(screen.getByRole("tab", { name: /nuget tool/i }));
    await user.type(screen.getByLabelText("NuGet package query"), "Demo.Tool");
    await user.click(screen.getByRole("button", { name: /search nuget/i }));
    await user.click(await screen.findByRole("button", { name: /inspect tool/i }));

    expect(await screen.findByText("Last Probe Attempt")).toBeInTheDocument();

    await user.click(screen.getByRole("tab", { name: /^files$/i }));

    expect(screen.queryByText("Last Probe Attempt")).not.toBeInTheDocument();
    expect(screen.queryByText("The package does not bundle opencli.json.")).not.toBeInTheDocument();
  });
});
