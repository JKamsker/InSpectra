import { cpSync, mkdirSync, rmSync } from "node:fs";
import { spawnSync } from "node:child_process";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const scriptDir = dirname(fileURLToPath(import.meta.url));
const uiRoot = resolve(scriptDir, "..");
const probeProject = resolve(uiRoot, "../InSpectra.Probe.Wasm/InSpectra.Probe.Wasm.csproj");
const probeOutput = resolve(uiRoot, ".probe-dist");
const distRoot = resolve(uiRoot, "dist");
const distProbe = resolve(distRoot, "probe");

run("npx", ["tsc", "--noEmit"]);
run("dotnet", ["workload", "restore", probeProject]);
run("dotnet", ["publish", probeProject, "-c", "Release", "-o", probeOutput]);
run("npx", ["vite", "build"]);

rmSync(distProbe, { recursive: true, force: true });
mkdirSync(distRoot, { recursive: true });
cpSync(probeOutput, distProbe, { recursive: true });

function run(command, args) {
  const executable = process.platform === "win32" && command === "npx" ? "npx.cmd" : command;
  const result = spawnSync(executable, args, {
    cwd: uiRoot,
    stdio: "inherit",
    shell: false,
  });

  if (result.status !== 0) {
    process.exit(result.status ?? 1);
  }
}
