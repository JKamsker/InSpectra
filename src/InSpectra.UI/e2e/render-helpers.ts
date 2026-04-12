import { execFileSync } from "node:child_process";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, "../../..");

/**
 * Renders an opencli.json (and optional xmldoc.xml) to an HTML bundle using the .NET CLI.
 * Returns the path to the output directory containing the rendered files.
 */
export function renderHtml(options: {
  opencliPath: string;
  xmldocPath?: string;
  outDir?: string;
  title?: string;
  commandPrefix?: string;
  singleFile?: boolean;
  compressionLevel?: number;
}): string {
  const outDir = options.outDir ?? fs.mkdtempSync(path.join(os.tmpdir(), "inspectra-e2e-"));
  const ownsOutDir = !options.outDir;
  const dotnetConfiguration = process.env.INSPECTRA_E2E_DOTNET_CONFIGURATION;
  const noBuild = process.env.INSPECTRA_E2E_DOTNET_NO_BUILD === "1";

  const args = [
    "run", "--project", path.join(repoRoot, "src/InSpectra.Gen/InSpectra.Gen.csproj"),
  ];

  if (dotnetConfiguration) {
    args.push("--configuration", dotnetConfiguration);
  }

  if (noBuild) {
    args.push("--no-build");
  }

  args.push("--", "render", "file", "html", options.opencliPath);

  if (options.xmldocPath) {
    args.push("--xmldoc", options.xmldocPath);
  }

  if (options.title) {
    args.push("--title", options.title);
  }

  if (options.commandPrefix) {
    args.push("--command-prefix", options.commandPrefix);
  }

  if (options.singleFile) {
    args.push("--single-file");
  }

  if (options.compressionLevel !== undefined) {
    args.push("--compression-level", String(options.compressionLevel));
  }

  args.push("--out-dir", outDir, "--overwrite");

  try {
    execFileSync("dotnet", args, { cwd: repoRoot, stdio: "pipe", timeout: 60_000 });
    return outDir;
  } catch (error) {
    if (ownsOutDir) {
      fs.rmSync(outDir, { recursive: true, force: true });
    }

    throw error;
  }
}

/**
 * Renders the test fixture opencli.json to an HTML bundle.
 * Uses a fresh temporary output directory by default so E2E always exercises
 * the current renderer output instead of any checked-in bundle snapshot.
 */
export function renderTestFixture(options?: {
  outDir?: string;
  title?: string;
  commandPrefix?: string;
  singleFile?: boolean;
  compressionLevel?: number;
}): string {
  return renderHtml({
    opencliPath: path.join(repoRoot, "examples/jellyfin-cli/opencli.json"),
    xmldocPath: path.join(repoRoot, "examples/jellyfin-cli/xmldoc.xml"),
    outDir: options?.outDir,
    title: options?.title,
    commandPrefix: options?.commandPrefix,
    singleFile: options?.singleFile,
    compressionLevel: options?.compressionLevel,
  });
}
