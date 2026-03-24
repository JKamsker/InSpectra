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
rmSync(probeOutput, { recursive: true, force: true });
run("dotnet", ["workload", "restore", probeProject], "The browser probe requires the .NET wasm-tools workload.");
run("dotnet", ["publish", probeProject, "-c", "Release", "-o", probeOutput], "The browser probe publish failed.");
run("npx", ["vite", "build"]);

rmSync(distProbe, { recursive: true, force: true });
mkdirSync(distRoot, { recursive: true });
cpSync(probeOutput, distProbe, { recursive: true });

function run(command, args, failureHint = null) {
  const executable = process.platform === "win32" && command === "npx" ? "npx.cmd" : command;
  const result = spawnSync(executable, args, {
    cwd: uiRoot,
    stdio: "inherit",
    shell: false,
  });

  if (result.status !== 0) {
    if (failureHint) {
      console.error(failureHint);
      console.error("Install the workload manually with `dotnet workload install wasm-tools` if automatic restore fails.");
    }

    process.exit(result.status ?? 1);
  }
}
