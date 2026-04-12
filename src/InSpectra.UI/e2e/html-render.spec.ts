import { test, expect } from "@playwright/test";
import { renderTestFixture } from "./render-helpers";
import { serveDirectory } from "./serve";
import fs from "node:fs";

let server: { url: string; close: () => Promise<void> } | undefined;
let outDir: string | undefined;

test.beforeAll(async () => {
  const renderedOutDir = renderTestFixture({ compressionLevel: 1 });
  outDir = renderedOutDir;
  server = await serveDirectory(renderedOutDir);
});

test.afterAll(async () => {
  await server?.close();

  if (outDir) {
    fs.rmSync(outDir, { recursive: true, force: true });
  }
});

test("rendered HTML loads and displays the CLI title", async ({ page }) => {
  await page.goto(server!.url);
  await expect(page.locator(".brand-title")).toHaveText("jf");
});

test("rendered HTML honors an explicit title override", async ({ page }) => {
  let customDir: string | undefined;
  let customServer: { url: string; close: () => Promise<void> } | undefined;

  try {
    customDir = renderTestFixture({
      title: "JellyfinCli",
      commandPrefix: "jf",
      compressionLevel: 1,
    });
    customServer = await serveDirectory(customDir);

    await page.goto(customServer!.url);
    await expect(page.locator(".brand-title")).toHaveText("JellyfinCli");
    await expect(page.locator(".content-column h1")).toHaveText("JellyfinCli");
    await page.locator(".sidebar-nav").getByRole("button", { name: "artists", exact: true }).click();
    await expect(page.locator(".example-block").first()).toContainText("jf artists");
    await expect(page.locator(".composer-output")).toContainText("jf artists");
  } finally {
    await customServer?.close();

    if (customDir) {
      fs.rmSync(customDir, { recursive: true, force: true });
    }
  }
});

test("rendered HTML shows the overview panel with command surface", async ({ page }) => {
  await page.goto(server!.url);
  const content = page.locator(".content-column");
  await expect(content).toBeVisible();
  await expect(content.getByText("Command surface")).toBeVisible();
});

test("command tree is populated and navigable", async ({ page }) => {
  await page.goto(server!.url);
  const sidebar = page.locator(".sidebar-nav");
  await expect(sidebar).toBeVisible();

  await sidebar.getByRole("button", { name: "artists", exact: true }).click();
  await expect(page.locator(".content-column")).toContainText("artists");
  expect(page.url()).toContain("#/command/");
});

test("search/command palette opens and filters", async ({ page }) => {
  await page.goto(server!.url);
  await page.waitForSelector(".brand-title");

  await page.locator(".toolbar-button", { hasText: "Search" }).click();
  const dialog = page.locator("[role='dialog']");
  await expect(dialog).toBeVisible();

  // Type a search term and verify results are shown
  await dialog.locator("input").fill("auth login");
  await expect(dialog.locator(".cmd-item")).toHaveCount(1);
});

test("rendered bundle does not include website-only assets", async () => {
  const fs = await import("node:fs");
  const path = await import("node:path");
  const files = fs.readdirSync(path.join(outDir!, "assets"));
  const mainFiles = files.filter((f: string) => f.startsWith("main-"));
  expect(mainFiles).toEqual([]);
});

test("no console errors on load", async ({ page }) => {
  const errors: string[] = [];
  page.on("pageerror", (err) => errors.push(err.message));

  await page.goto(server!.url);
  await page.waitForSelector(".brand-title");

  expect(errors).toEqual([]);
});

test("theme toggle switches between dark and light", async ({ page }) => {
  await page.goto(server!.url);
  await page.waitForSelector(".brand-title");
  const toggle = page.locator("button[title*='theme' i], button[aria-label*='theme' i]");
  await expect(toggle).toHaveCount(1);

  const before = await page.evaluate(() => document.documentElement.dataset.theme ?? "light");
  expect(["light", "dark"]).toContain(before);

  await toggle.click();
  const afterFirstClick = await page.evaluate(() => document.documentElement.dataset.theme);
  expect(afterFirstClick).toBe(before === "dark" ? "light" : "dark");

  await toggle.click();
  const afterSecondClick = await page.evaluate(() => document.documentElement.dataset.theme);
  expect(afterSecondClick).toBe(before);
});

test("composer panel can be toggled", async ({ page }) => {
  await page.goto(server!.url);
  await page.waitForSelector(".brand-title");

  const composerBtn = page.locator(".composer-toggle");
  await expect(composerBtn).toBeVisible();

  // Composer defaults to open on desktop — verify it's visible
  await expect(page.locator("aside.composer-open")).toBeVisible();

  // Click to close
  await composerBtn.click();
  await expect(page.locator("aside.composer-open")).toHaveCount(0);

  // Click to re-open
  await composerBtn.click();
  await expect(page.locator("aside.composer-open")).toBeVisible();
});
