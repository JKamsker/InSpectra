import { useEffect, useState } from "react";
import { AboutPage } from "./components/AboutPage";
import { CIGuidePage } from "./components/CIGuidePage";
import { CliViewer } from "./components/CliViewer";
import { ImportScreen } from "./components/ImportScreen";
import { NugetBrowser } from "./components/NugetBrowser";
import { PackageLoadingScreen } from "./components/PackageLoadingScreen";
import { SiteHeader } from "./components/SiteHeader";
import { ViewerDropzone } from "./components/ViewerDropzone";
import { defaultFeatureFlags, defaultViewerOptions, FeatureFlags, ViewerOptions } from "./boot/contracts";
import { useThemeEnforcement } from "./hooks/useThemeEnforcement";
import { loadFromFiles, loadFromUrls, LoadedSource } from "./data/loadSource";
import { buildPackageHash, HashRoute, parseHashRoute } from "./data/navigation";
import { fetchDiscoveryPackage, resolvePackageUrls } from "./data/nugetDiscovery";
import { normalizeOpenCliDocument, NormalizedCliDocument } from "./data/normalize";
import { toMessage } from "./utils";

interface LoadState {
  status: "loading" | "ready" | "empty";
  message?: string;
}

export function WebsiteApp() {
  const [loadState, setLoadState] = useState<LoadState>({ status: "empty" });
  const [error, setError] = useState<string | null>(null);
  const [warnings, setWarnings] = useState<string[]>([]);
  const [sourceLabel, setSourceLabel] = useState("");
  const [viewerOptions, setViewerOptions] = useState<ViewerOptions>(defaultViewerOptions());
  const [featureFlags] = useState<FeatureFlags>(() => defaultFeatureFlags());
  const [document, setDocument] = useState<NormalizedCliDocument | null>(null);
  const [route, setRoute] = useState<HashRoute>(() => parseHashRoute(window.location.hash));
  const [packageContext, setPackageContext] = useState<{ packageId: string; version?: string; command?: string } | null>(null);

  useEffect(() => {
    const handle = () => setRoute(parseHashRoute(window.location.hash));
    window.addEventListener("hashchange", handle);
    return () => window.removeEventListener("hashchange", handle);
  }, []);

  // Redirect #/browse (no params) to #/ since browse IS the home page now
  useEffect(() => {
    if (route.kind === "browse" && !route.packageId) {
      window.location.hash = "#/";
    }
  }, [route]);

  // Clear viewer state when navigating away from package
  useEffect(() => {
    if (route.kind !== "package") {
      setDocument(null);
      setPackageContext(null);
      setLoadState({ status: "empty" });
      setError(null);
      setWarnings([]);
      return;
    }

    const key = `${route.packageId.toLowerCase()}/${route.version ?? "latest"}`;
    if (
      packageContext &&
      packageContext.packageId.toLowerCase() === route.packageId.toLowerCase() &&
      (route.version ? packageContext.version === route.version : true) &&
      loadState.status === "ready"
    ) return;

    const controller = new AbortController();
    void loadPackageFromRoute(route.packageId, route.version, controller.signal);
    return () => controller.abort();
  }, [route.kind === "package" ? `${route.packageId.toLowerCase()}/${route.version ?? "latest"}` : null]);

  useThemeEnforcement(featureFlags, viewerOptions);

  async function loadPackageFromRoute(packageId: string, version: string | undefined, signal?: AbortSignal) {
    try {
      setLoadState({ status: "loading", message: `Loading ${packageId}${version ? ` v${version}` : ""}` });
      const pkg = await fetchDiscoveryPackage(packageId, signal);
      const resolvedVersion = version ?? pkg.latestVersion;
      if (!pkg.versions.some((v) => v.version === resolvedVersion)) {
        setError(`Version "${resolvedVersion}" not found for package "${packageId}".`);
        setLoadState({ status: "empty" });
        return;
      }
      const urls = resolvePackageUrls(pkg, resolvedVersion);
      const label = `${pkg.packageId} v${resolvedVersion}`;
      const command = pkg.versions.find((v) => v.version === resolvedVersion)?.command ?? pkg.versions[0]?.command;
      const loaded = await loadFromUrls(urls.opencliUrl, urls.xmldocUrl, viewerOptions, label, featureFlags, {
        title: pkg.packageId,
        commandPrefix: command,
      });
      applyLoadedSource(loaded);
      setPackageContext({
        packageId: pkg.packageId,
        version: resolvedVersion,
        command,
      });
    } catch (err) {
      if (err instanceof DOMException && err.name === "AbortError") return;
      setError(toMessage(err));
      setLoadState({ status: "empty" });
    }
  }

  function applyLoadedSource(source: LoadedSource) {
    setWarnings(source.warnings);
    setSourceLabel(source.label);
    setViewerOptions(source.options);
    setDocument(normalizeOpenCliDocument(source.document, source.options.includeHidden));
    setError(null);
    setLoadState({ status: "ready" });
  }

  async function handleFiles(files: File[]) {
    try {
      setLoadState({ status: "loading", message: "Importing local files." });
      const loaded = await loadFromFiles(files, viewerOptions, featureFlags);
      applyLoadedSource(loaded);
      setPackageContext({ packageId: "_local", version: undefined });
      window.location.hash = "#/pkg/_local";
    } catch (err) {
      setError(toMessage(err));
      setLoadState(document ? { status: "ready" } : { status: "empty" });
    }
  }

  async function handleLoadPackage(
    opencliUrl: string,
    xmldocUrl: string | undefined,
    label: string,
    packageId: string,
    version: string | undefined,
    command: string | undefined,
  ) {
    try {
      setLoadState({ status: "loading", message: `Loading ${label}` });
      const loaded = await loadFromUrls(opencliUrl, xmldocUrl, viewerOptions, label, featureFlags, {
        title: packageId,
        commandPrefix: command,
      });
      applyLoadedSource(loaded);
      setPackageContext({ packageId, version, command });
      window.location.hash = buildPackageHash(packageId, version);
    } catch (err) {
      setError(toMessage(err));
      setLoadState(document ? { status: "ready" } : { status: "empty" });
    }
  }

  // About page
  if (route.kind === "about") {
    return (
      <>
        <SiteHeader route={route} />
        <AboutPage />
      </>
    );
  }

  // CI Guide page
  if (route.kind === "guide") {
    return (
      <>
        <SiteHeader route={route} />
        <CIGuidePage section={route.section} />
      </>
    );
  }

  // Import page
  if (route.kind === "import") {
    return (
      <>
        <SiteHeader route={route} />
        <ImportScreen
          error={error}
          loading={loadState.status === "loading"}
          onFilesSelected={handleFiles}
        />
      </>
    );
  }

  // Package viewer (full page transition)
  if (route.kind === "package" || (route.kind === "overview" && loadState.status === "ready" && document && packageContext)) {
    if (loadState.status === "loading") {
      return <PackageLoadingScreen message={loadState.message} />;
    }

    if (loadState.status === "ready" && document) {
      const commandPath = route.kind === "package" ? route.commandPath : undefined;

      function handleNavigate(path?: string) {
        if (packageContext) {
          window.location.hash = buildPackageHash(packageContext.packageId, packageContext.version, path);
        } else {
          window.location.hash = path ? `#/command/${path.split(" ").map(encodeURIComponent).join("/")}` : "#/";
        }
      }

      return (
        <>
          <CliViewer
            document={document}
            viewerOptions={viewerOptions}
            featureFlags={featureFlags}
            packageContext={packageContext}
            sourceLabel={sourceLabel}
            warnings={warnings}
            error={error}
            commandPath={commandPath}
            onNavigate={handleNavigate}
          />
          {featureFlags.packageUpload && <ViewerDropzone onFilesSelected={handleFiles} />}
        </>
      );
    }

    // Package route but not loaded yet - show loading
    if (route.kind === "package") {
      return <PackageLoadingScreen message={`Loading ${route.packageId}${route.version ? ` v${route.version}` : ""}`} />;
    }
  }

  // NuGet browser (home page) - handles overview, browse with packageId
  return (
    <>
      <SiteHeader route={route} />
      <NugetBrowser
        packageId={route.kind === "browse" ? route.packageId : undefined}
        version={route.kind === "browse" ? route.version : undefined}
        onLoadPackage={handleLoadPackage}
        onBack={() => { window.location.hash = "#/"; }}
        onBackToBrowse={() => { window.location.hash = "#/"; }}
      />
    </>
  );
}
