import { test, expect } from "@playwright/test";
import { renderTestFixture } from "./render-helpers";
import fs from "node:fs";
import path from "node:path";

let outDir: string | undefined;
let indexPath: string | undefined;

test.beforeAll(() => {
  const renderedOutDir = renderTestFixture({ singleFile: true });
  outDir = renderedOutDir;
  indexPath = path.join(renderedOutDir, "index.html");
});

test.afterAll(() => {
  if (outDir && fs.existsSync(outDir)) {
    fs.rmSync(outDir, { recursive: true, force: true });
  }
});

test("single-file produces exactly one output file", () => {
  const files = fs.readdirSync(outDir!);
  expect(files).toEqual(["index.html"]);
});

test("single-file HTML works from file:// protocol", async ({ page }) => {
  const errors: string[] = [];
  page.on("pageerror", (err) => errors.push(err.message));

  const fileUrl = `file:///${indexPath!.replace(/\\/g, "/")}`;
  await page.goto(fileUrl);

  await expect(page.locator(".brand-title")).toHaveText("jf", { timeout: 10_000 });
  await expect(page.locator(".content-column")).toBeVisible();
  await expect(page.locator(".sidebar-nav")).toBeVisible();

  expect(errors).toEqual([]);
});

test("single-file command navigation works from file://", async ({ page }) => {
  const fileUrl = `file:///${indexPath!.replace(/\\/g, "/")}`;
  await page.goto(fileUrl);
  await page.waitForSelector(".brand-title");

  await page.locator(".sidebar-nav").getByRole("button", { name: "auth", exact: true }).click();
  await expect(page.locator(".content-column")).toContainText("auth");
});
