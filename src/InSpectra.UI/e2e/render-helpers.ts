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
}): string {
  const outDir = options.outDir ?? fs.mkdtempSync(path.join(os.tmpdir(), "inspectra-e2e-"));

  const args = [
    "run", "--project", path.join(repoRoot, "src/InSpectra.Gen/InSpectra.Gen.csproj"),
    "--", "render", "file", "html", options.opencliPath,
  ];

  if (options.xmldocPath) {
    args.push("--xmldoc", options.xmldocPath);
  }

  if (options.title) {
    args.push("--title", options.title);
  }

  if (options.commandPrefix) {
    args.push("--command-prefix", options.commandPrefix);
  }

  args.push("--out-dir", outDir, "--overwrite");

  execFileSync("dotnet", args, { cwd: repoRoot, stdio: "pipe", timeout: 60_000 });
  return outDir;
}

/**
 * Renders the test fixture opencli.json to an HTML bundle.
 * Uses the pre-built output at e2e/.rendered only for the default no-override case.
 */
export function renderTestFixture(options?: { outDir?: string; title?: string; commandPrefix?: string }): string {
  const prebuilt = path.resolve(__dirname, ".rendered");
  if (!options?.outDir && !options?.title && !options?.commandPrefix && fs.existsSync(path.join(prebuilt, "index.html"))) {
    return prebuilt;
  }

  return renderHtml({
    opencliPath: path.join(repoRoot, "examples/jellyfin-cli/opencli.json"),
    xmldocPath: path.join(repoRoot, "examples/jellyfin-cli/xmldoc.xml"),
    outDir: options?.outDir ?? prebuilt,
    title: options?.title,
    commandPrefix: options?.commandPrefix,
  });
}
