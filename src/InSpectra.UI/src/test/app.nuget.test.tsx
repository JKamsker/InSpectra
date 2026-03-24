import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { InSpectraApp } from "../InSpectraApp";
import { defaultViewerOptions } from "../boot/contracts";
import { testDocument } from "./fixtures";
import { vi } from "vitest";

const { loadFromNugetTool } = vi.hoisted(() => ({
  loadFromNugetTool: vi.fn(),
}));

vi.mock("../data/loadNugetTool", () => ({
  loadFromNugetTool,
}));

describe("InSpectraUI NuGet mode", () => {
  beforeEach(() => {
    document.body.innerHTML =
      '<div id="inspectra-root"></div><script id="inspectra-bootstrap" type="application/json">__INSPECTRA_BOOTSTRAP__</script>';
    window.history.replaceState({}, "", "https://example.test/viewer/index.html#/");
    loadFromNugetTool.mockReset();
    vi.stubGlobal("fetch", vi.fn(async () => ({
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
    })));
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
        confidence: "partial",
      },
    });

    const user = userEvent.setup();
    render(<InSpectraApp />);

    await user.click(screen.getByRole("tab", { name: /nuget tool/i }));
    await user.type(screen.getByLabelText("NuGet package query"), "Demo.Tool");
    await user.click(screen.getByRole("button", { name: /search nuget/i }));

    expect(await screen.findByRole("button", { name: /demo.tool/i })).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: /inspect tool/i }));

    await waitFor(() => {
      expect(loadFromNugetTool).toHaveBeenCalledWith({ id: "Demo.Tool", version: "2.0.0" }, defaultViewerOptions());
    });

    expect(await screen.findByText("NuGet Tool Probe")).toBeInTheDocument();
    expect(await screen.findAllByText("demo")).not.toHaveLength(0);
    expect(screen.getByText("Static Spectre probe.")).toBeInTheDocument();
  });
});
