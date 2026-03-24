import { resolveStartupRequest } from "../boot/bootstrap";
import { resolveViewerLinksFromSearch } from "../boot/urlParams";
import { testDocument } from "./fixtures";

describe("boot resolution", () => {
  beforeEach(() => {
    document.body.innerHTML = "";
  });

  it("prefers injected bootstrap over URL params", () => {
    document.body.innerHTML = `
      <script id="inspectre-bootstrap" type="application/json">${JSON.stringify({
        mode: "inline",
        openCli: testDocument,
        options: { includeHidden: true, includeMetadata: true },
      })}</script>
    `;

    const result = resolveStartupRequest({
      documentRef: document,
      search: "?dir=/docs/",
      href: "https://example.test/viewer/index.html",
    });

    if (result.kind !== "inline") {
      throw new Error("Expected inline mode.");
    }

    expect(result.options.includeHidden).toBe(true);
  });

  it("uses query params when no bootstrap is injected", () => {
    const result = resolveStartupRequest({
      documentRef: document,
      search: "?opencli=./opencli.json",
      href: "https://example.test/viewer/index.html",
    });

    expect(result.kind).toBe("links");
    if (result.kind !== "links") {
      throw new Error("Expected links mode.");
    }

    expect(result.links.openCliUrl).toBe("https://example.test/viewer/opencli.json");
  });

  it("prefers injected links bootstrap over URL params", () => {
    document.body.innerHTML = `
      <script id="inspectre-bootstrap" type="application/json">${JSON.stringify({
        mode: "links",
        directoryUrl: "./bundle-data/",
        options: { includeMetadata: true },
      })}</script>
    `;

    const result = resolveStartupRequest({
      documentRef: document,
      search: "?opencli=./ignored.json",
      href: "https://example.test/viewer/index.html",
    });

    expect(result.kind).toBe("links");
    if (result.kind !== "links") {
      throw new Error("Expected links mode.");
    }

    expect(result.source).toBe("bootstrap");
    expect(result.links.openCliUrl).toBe("https://example.test/viewer/bundle-data/opencli.json");
    expect(result.options.includeMetadata).toBe(true);
  });

  it("falls back to empty mode without bootstrap or query params", () => {
    const result = resolveStartupRequest({
      documentRef: document,
      search: "",
      href: "https://example.test/viewer/index.html",
    });

    expect(result.kind).toBe("empty");
  });
});

describe("query param parsing", () => {
  it("infers opencli and xmldoc from dir", () => {
    const result = resolveViewerLinksFromSearch(
      "?dir=./bundle-data/",
      "https://example.test/viewer/index.html",
    );

    expect(result).toEqual({
      openCliUrl: "https://example.test/viewer/bundle-data/opencli.json",
      xmlDocUrl: "https://example.test/viewer/bundle-data/xmldoc.xml",
      directoryUrl: "https://example.test/viewer/bundle-data/",
      xmlDocIsOptional: true,
    });
  });

  it("lets explicit URLs override inferred directory links", () => {
    const result = resolveViewerLinksFromSearch(
      "?dir=./bundle-data/&opencli=../raw/opencli.json&xmldoc=../raw/xmldoc.xml",
      "https://example.test/viewer/index.html",
    );

    expect(result?.openCliUrl).toBe("https://example.test/raw/opencli.json");
    expect(result?.xmlDocUrl).toBe("https://example.test/raw/xmldoc.xml");
    expect(result?.xmlDocIsOptional).toBe(false);
  });
});
