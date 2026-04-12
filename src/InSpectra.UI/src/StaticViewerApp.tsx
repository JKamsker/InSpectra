import { type ReactNode, useEffect, useState } from "react";
import { CliViewer } from "./components/CliViewer";
import { ImportScreen } from "./components/ImportScreen";
import { NugetBrowser } from "./components/NugetBrowser";
import { StaticViewerErrorState } from "./components/StaticViewerErrorState";
import { PackageLoadingScreen } from "./components/PackageLoadingScreen";
import { StaticViewerShell } from "./components/StaticViewerShell";
import { resolveStartupRequest } from "./boot/bootstrap";
import { defaultViewerOptions, disabledFeatureFlags, FeatureFlags } from "./boot/contracts";
import { useThemeEnforcement } from "./hooks/useThemeEnforcement";
import { loadFromFiles, loadFromStartupRequest } from "./data/loadSource";
import { buildCommandHash, buildPackageHash, HashRoute, parseHashRoute } from "./data/navigation";
import { fetchDiscoveryPackage, resolvePackageUrls } from "./data/nugetDiscovery";
import {
  applyHomeSourceState,
  applyPackageSourceState,
  clearPendingPackageLoadState,
  didLeavePendingPackageLoad,
  getPackageRequestKey,
  LoadState,
  loadPackageDocument,
  PendingPackageLoad,
  setLoadErrorState,
} from "./staticViewerState";
import {
  buildToolbarRoutes,
  isLoadedPackageRoute,
  isSupportedStaticRoute,
  isUnsupportedStaticRoute,
  PackageContext,
  resolvePackageCommand,
  supportsBrowseRoute,
  supportsImportRoute,
  ViewerState,
} from "./staticViewerSupport";

export function StaticViewerApp() {
  const [loadState, setLoadState] = useState<LoadState>({
    status: "loading",
    message: "Resolving viewer boot mode.",
  });
  const [bootResolved, setBootResolved] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [browseLoadError, setBrowseLoadError] = useState<string | null>(null);
  const [featureFlags, setFeatureFlags] = useState<FeatureFlags>(() => disabledFeatureFlags());
  const [homeViewer, setHomeViewer] = useState<ViewerState | null>(null);
  const [packageViewer, setPackageViewer] = useState<ViewerState | null>(null);
  const [route, setRoute] = useState<HashRoute>(() => parseHashRoute(window.location.hash));
  const [packageContext, setPackageContext] = useState<PackageContext | null>(null);
  const [pendingPackageLoad, setPendingPackageLoad] = useState<PendingPackageLoad | null>(null);
  const toolbarRoutes = buildToolbarRoutes(featureFlags);
  const effectiveToolbarRoutes = homeViewer ? toolbarRoutes : { ...toolbarRoutes, homeHref: undefined };
  const viewerOptions = packageViewer?.viewerOptions ?? homeViewer?.viewerOptions ?? defaultViewerOptions();
  const redirectUnsupportedRoute = bootResolved && isUnsupportedStaticRoute(route, featureFlags);
  const shellFeatureFlags = bootResolved ? featureFlags : disabledFeatureFlags();
  const shellToolbarRoutes = bootResolved ? effectiveToolbarRoutes : buildToolbarRoutes(shellFeatureFlags);
  const restorePendingHomeViewer =
    loadState.status === "loading" &&
    !!homeViewer &&
    !!pendingPackageLoad &&
    didLeavePendingPackageLoad(route, pendingPackageLoad);
  const stateSetters = {
    setFeatureFlags,
    setHomeViewer,
    setPackageViewer,
    setPackageContext,
    setPendingPackageLoad,
    setError,
    setLoadState,
  };

  useEffect(() => {
    const controller = new AbortController();
    void initialize(controller.signal);
    return () => controller.abort();
  }, []);

  useEffect(() => {
    const handle = () => setRoute(parseHashRoute(window.location.hash));
    window.addEventListener("hashchange", handle);
    return () => window.removeEventListener("hashchange", handle);
  }, []);

  useEffect(() => {
    setBrowseLoadError(null);
  }, [route]);

  useEffect(() => {
    if (redirectUnsupportedRoute) {
      window.location.hash = "#/";
    }
  }, [redirectUnsupportedRoute]);

  useEffect(() => {
    if (!bootResolved || route.kind !== "package" || !supportsBrowseRoute(featureFlags)) {
      return;
    }

    if (packageViewer && isLoadedPackageRoute(route, packageContext)) {
      return;
    }

    const controller = new AbortController();
    void loadPackageFromRoute(route.packageId, route.version, controller.signal);
    return () => controller.abort();
  }, [bootResolved, featureFlags, packageContext, packageViewer, route]);

  useThemeEnforcement(featureFlags, viewerOptions);

  async function initialize(signal: AbortSignal) {
    try {
      const request = await resolveStartupRequest({ search: window.location.search, href: window.location.href });
      setFeatureFlags(request.features);
      const loaded = await loadFromStartupRequest(request, signal);
      if (loaded) {
        applyHomeSourceState(loaded, stateSetters);
        return;
      }

      const initialRoute = parseHashRoute(window.location.hash);
      if (initialRoute.kind === "package" && supportsBrowseRoute(request.features)) {
        setLoadState({ status: "loading", message: `Loading ${initialRoute.packageId}${initialRoute.version ? ` v${initialRoute.version}` : ""}` });
        return;
      }

      if (isSupportedStaticRoute(initialRoute, request.features)) {
        setLoadState({ status: "empty" });
        return;
      }

      setError("No bootstrap data found.");
      setLoadState({ status: "error" });
    } catch (err) {
      setLoadErrorState(err, null, stateSetters);
    } finally {
      if (!signal.aborted) {
        setBootResolved(true);
      }
    }
  }

  async function loadPackageFromRoute(packageId: string, version: string | undefined, signal?: AbortSignal) {
    const requestKey = getPackageRequestKey(packageId, version);
    try {
      const pkg = await fetchDiscoveryPackage(packageId, signal);
      const resolvedVersion = version ?? pkg.latestVersion;
      if (!pkg.versions.some((candidate) => candidate.version === resolvedVersion)) {
        clearPendingPackageLoadState(requestKey, setPendingPackageLoad);
        setError(`Version "${resolvedVersion}" not found for package "${packageId}".`);
        setLoadState({ status: "error" });
        return;
      }

      const urls = resolvePackageUrls(pkg, resolvedVersion);
      const command = resolvePackageCommand(pkg, resolvedVersion);
      await loadPackageDocument({
        requestKey,
        origin: "package-route",
        label: `${pkg.packageId} v${resolvedVersion}`,
        packageId: pkg.packageId,
        version,
        command,
        opencliUrl: urls.opencliUrl,
        xmldocUrl: urls.xmldocUrl,
        viewerOptions,
        featureFlags,
        homeViewer,
        fallbackViewer: null,
        fallbackStatus: "error",
        stateSetters,
      });
    } catch (err) {
      if (err instanceof DOMException && err.name === "AbortError") {
        clearPendingPackageLoadState(requestKey, setPendingPackageLoad);
        if (
          didLeavePendingPackageLoad(parseHashRoute(window.location.hash), {
            key: requestKey,
            origin: "package-route",
            packageId,
            version,
          })
        ) {
          setLoadState(homeViewer ? { status: "ready" } : { status: "empty" });
        }
        return;
      }

      clearPendingPackageLoadState(requestKey, setPendingPackageLoad);
      setLoadErrorState(err, null, stateSetters, "error");
    }
  }

  async function handleFiles(files: File[]) {
    try {
      setLoadState({ status: "loading", message: "Importing local files." });
      const loaded = await loadFromFiles(files, viewerOptions, featureFlags);
      applyHomeSourceState(loaded, stateSetters);
      window.location.hash = "#/";
    } catch (err) {
      setLoadErrorState(err, homeViewer, stateSetters);
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
    const requestKey = getPackageRequestKey(packageId, version);
    setBrowseLoadError(null);
    await loadPackageDocument({
      requestKey,
      origin: "browse-route",
      label,
      packageId,
      version,
      browseVersion: route.kind === "browse" ? route.version : undefined,
      command,
      opencliUrl,
      xmldocUrl,
      fallbackViewer: packageViewer ?? homeViewer,
      viewerOptions,
      featureFlags,
      homeViewer,
      stateSetters,
      onError: setBrowseLoadError,
      onSuccess: () => { window.location.hash = buildPackageHash(packageId, version); },
    });
  }

  if (!bootResolved) {
    return renderShell(<PackageLoadingScreen message={loadState.message} />);
  }

  if (redirectUnsupportedRoute) {
    return renderShell(<PackageLoadingScreen message="Returning to the embedded viewer." />);
  }

  if (route.kind === "browse") {
    return renderShell(
      <NugetBrowser
        packageId={route.packageId}
        version={route.version}
        onBack={() => { window.location.hash = "#/"; }}
        onBackToBrowse={() => { window.location.hash = "#/browse"; }}
        onLoadPackage={handleLoadPackage}
        inspectError={browseLoadError}
      />,
    );
  }

  if (route.kind === "import") {
    return renderShell(
      <ImportScreen
        error={error}
        loading={loadState.status === "loading"}
        onFilesSelected={handleFiles}
      />,
    );
  }

  if (restorePendingHomeViewer && homeViewer) {
    return renderViewer(homeViewer, route.kind === "command" ? route.commandPath : undefined, null);
  }

  if (route.kind === "package") {
    if (loadState.status === "error") {
      return renderShell(<StaticViewerErrorState error={error} message={loadState.message} />);
    }

    if (loadState.status === "ready" && packageViewer && isLoadedPackageRoute(route, packageContext)) {
      return renderViewer(packageViewer, route.commandPath, packageContext);
    }

    return renderShell(<PackageLoadingScreen message={loadState.message ?? `Loading ${route.packageId}${route.version ? ` v${route.version}` : ""}`} />);
  }

  if (loadState.status === "loading") {
    return renderShell(<PackageLoadingScreen message={loadState.message} />);
  }

  if (!homeViewer) {
    return renderShell(<StaticViewerErrorState error={error} message={loadState.message} />);
  }

  return renderViewer(homeViewer, route.kind === "command" ? route.commandPath : undefined, null);

  function renderViewer(currentViewer: ViewerState, commandPath: string | undefined, currentPackageContext: PackageContext | null) {
    return (
      <CliViewer
        document={currentViewer.document}
        viewerOptions={currentViewer.viewerOptions}
        featureFlags={featureFlags}
        packageContext={currentPackageContext}
        sourceLabel={currentViewer.sourceLabel}
        warnings={currentViewer.warnings}
        error={error}
        commandPath={commandPath}
        onNavigate={(path) => handleNavigate(path, currentPackageContext)}
        toolbarRoutes={effectiveToolbarRoutes}
      />
    );
  }

  function handleNavigate(path: string | undefined, currentPackageContext: PackageContext | null) {
    if (currentPackageContext) {
      window.location.hash = buildPackageHash(currentPackageContext.packageId, currentPackageContext.version, path);
      return;
    }

    window.location.hash = path ? buildCommandHash(path) : "#/";
  }

  function renderShell(content: ReactNode) {
    return <StaticViewerShell featureFlags={shellFeatureFlags} routes={shellToolbarRoutes}>{content}</StaticViewerShell>;
  }
}
