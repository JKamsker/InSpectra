import { test, expect } from "@playwright/test";
import { execFileSync } from "node:child_process";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, "../../..");

let outDir: string;
let indexPath: string;

test.beforeAll(() => {
  outDir = fs.mkdtempSync(path.join(os.tmpdir(), "inspectra-single-file-"));
  execFileSync("dotnet", [
    "run", "--project", path.join(repoRoot, "src/InSpectra.Gen/InSpectra.Gen.csproj"),
    "--", "render", "file", "html",
    path.join(repoRoot, "examples/jellyfin-cli/opencli.json"),
    "--xmldoc", path.join(repoRoot, "examples/jellyfin-cli/xmldoc.xml"),
    "--out-dir", outDir, "--overwrite", "--single-file",
  ], { cwd: repoRoot, stdio: "pipe", timeout: 60_000 });

  indexPath = path.join(outDir, "index.html");
});

test.afterAll(() => {
  if (outDir && fs.existsSync(outDir)) {
    fs.rmSync(outDir, { recursive: true, force: true });
  }
});

test("single-file produces exactly one output file", () => {
  const files = fs.readdirSync(outDir);
  expect(files).toEqual(["index.html"]);
});

test("single-file HTML works from file:// protocol", async ({ page }) => {
  const errors: string[] = [];
  page.on("pageerror", (err) => errors.push(err.message));

  const fileUrl = `file:///${indexPath.replace(/\\/g, "/")}`;
  await page.goto(fileUrl);

  await expect(page.locator(".brand-title")).toHaveText("jf", { timeout: 10_000 });
  await expect(page.locator(".content-column")).toBeVisible();
  await expect(page.locator(".sidebar-nav")).toBeVisible();

  expect(errors).toEqual([]);
});

test("single-file command navigation works from file://", async ({ page }) => {
  const fileUrl = `file:///${indexPath.replace(/\\/g, "/")}`;
  await page.goto(fileUrl);
  await page.waitForSelector(".brand-title");

  await page.locator(".sidebar-nav").getByRole("button", { name: "auth", exact: true }).click();
  await expect(page.locator(".content-column")).toContainText("auth");
});
