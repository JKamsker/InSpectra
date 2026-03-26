import { startTransition, useDeferredValue, useEffect, useRef, useState } from "react";
import { resolveStartupRequest } from "../boot/bootstrap";
import { defaultFeatureFlags, defaultViewerOptions, FeatureFlags, ViewerOptions } from "../boot/contracts";
import { loadFromFiles, loadFromStartupRequest, loadFromUrls, LoadedSource } from "../data/loadSource";
import { buildCommandHash, buildPackageHash, HashRoute, parseHashRoute } from "../data/navigation";
import { fetchDiscoveryIndex, findPackageById, resolvePackageUrls } from "../data/nugetDiscovery";
import { findCommandByPath, normalizeOpenCliDocument, NormalizedCliDocument } from "../data/normalize";

interface LoadState {
  status: "loading" | "ready" | "empty";
  message?: string;
}

function readBool(key: string, fallback: boolean): boolean {
  const v = localStorage.getItem(key);
  return v === null ? fallback : v === "true";
}

function readNumber(key: string, fallback: number): number {
  const v = localStorage.getItem(key);
  if (v === null) return fallback;
  const n = Number(v);
  return Number.isFinite(n) ? n : fallback;
}

function toMessage(error: unknown): string {
  return error instanceof Error ? error.message : "Unknown error.";
}

export function useAppState() {
  const searchInputRef = useRef<HTMLInputElement>(null);
  const mobileSearchInputRef = useRef<HTMLInputElement>(null);

  const [loadState, setLoadState] = useState<LoadState>({ status: "loading", message: "Resolving viewer boot mode." });
  const [error, setError] = useState<string | null>(null);
  const [warnings, setWarnings] = useState<string[]>([]);
  const [sourceLabel, setSourceLabel] = useState("");
  const [viewerOptions, setViewerOptions] = useState<ViewerOptions>(defaultViewerOptions());
  const [featureFlags, setFeatureFlags] = useState<FeatureFlags>(defaultFeatureFlags());
  const [document, setDocument] = useState<NormalizedCliDocument | null>(null);
  const [searchTerm, setSearchTerm] = useState("");
  const [route, setRoute] = useState<HashRoute>(() => parseHashRoute(window.location.hash));
  const deferredSearch = useDeferredValue(searchTerm);
  const [paletteOpen, setPaletteOpen] = useState(false);
  const [composerOpen, setComposerOpen] = useState(() => {
    return window.innerWidth <= 768 ? false : readBool("inspectra-composer-open", true);
  });
  const [composerWidth, setComposerWidth] = useState(() => readNumber("inspectra-composer-width", 304));
  const [mobileSidebarOpen, setMobileSidebarOpen] = useState(false);
  const [mobileSidebarSearch, setMobileSidebarSearch] = useState(false);
  const [packageContext, setPackageContext] = useState<{ packageId: string; version: string | undefined } | null>(null);

  // --- Effects ---

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
    if (document && route.kind === "command" && !findCommandByPath(document.commands, route.commandPath)) {
      window.location.hash = "#/";
    }
  }, [document]);

  useEffect(() => {
    if (route.kind === "browse") {
      setDocument(null);
      setPackageContext(null);
      setLoadState({ status: "empty" });
      setError(null);
      setWarnings([]);
      setSourceLabel("");
    }
  }, [route.kind]);

  const packageRouteKey = route.kind === "package" ? `${route.packageId.toLowerCase()}/${route.version ?? "latest"}` : null;
  useEffect(() => {
    if (route.kind !== "package") return;
    if (
      packageContext &&
      packageContext.packageId.toLowerCase() === route.packageId.toLowerCase() &&
      (route.version ? packageContext.version === route.version : true) &&
      loadState.status === "ready"
    ) return;

    const controller = new AbortController();
    void loadPackageFromRoute(route.packageId, route.version, controller.signal);
    return () => controller.abort();
  }, [packageRouteKey]);

  useEffect(() => {
    function handleKeyDown(e: KeyboardEvent) {
      const mod = e.ctrlKey || e.metaKey;
      if (mod && e.key === "f") {
        e.preventDefault();
        searchInputRef.current?.focus();
      }
      if (mod && e.key === "k") {
        e.preventDefault();
        setPaletteOpen((o) => !o);
      }
    }
    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, []);

  useEffect(() => {
    if (!featureFlags.darkTheme) {
      window.document.documentElement.dataset.theme = "light";
    } else if (!featureFlags.lightTheme) {
      window.document.documentElement.dataset.theme = "dark";
    }
  }, [featureFlags.darkTheme, featureFlags.lightTheme]);

  // --- Business logic ---

  async function initialize(signal: AbortSignal) {
    try {
      const request = resolveStartupRequest({ search: window.location.search, href: window.location.href });
      setFeatureFlags(request.features);
      const loaded = await loadFromStartupRequest(request, signal);
      if (loaded) { applyLoadedSource(loaded); return; }

      const initialRoute = parseHashRoute(window.location.hash);
      if (initialRoute.kind === "package") {
        setLoadState({ status: "loading", message: `Loading ${initialRoute.packageId}${initialRoute.version ? ` v${initialRoute.version}` : ""}` });
        return;
      }
      setLoadState({ status: "empty" });
    } catch (err) {
      setError(toMessage(err));
      setLoadState({ status: "empty" });
    }
  }

  async function loadPackageFromRoute(packageId: string, version: string | undefined, signal?: AbortSignal) {
    try {
      setLoadState({ status: "loading", message: `Loading ${packageId}${version ? ` v${version}` : ""}` });
      const index = await fetchDiscoveryIndex(signal);
      const pkg = findPackageById(index, packageId);
      if (!pkg) {
        setError(`Package "${packageId}" not found in the discovery index.`);
        setLoadState({ status: "empty" });
        return;
      }
      const resolvedVersion = version ?? pkg.latestVersion;
      if (!pkg.versions.some((v) => v.version === resolvedVersion)) {
        setError(`Version "${resolvedVersion}" not found for package "${packageId}".`);
        setLoadState({ status: "empty" });
        return;
      }
      const urls = resolvePackageUrls(pkg, resolvedVersion);
      const label = `${pkg.packageId} v${resolvedVersion}`;
      const loaded = await loadFromUrls(urls.opencliUrl, urls.xmldocUrl, viewerOptions, label, featureFlags);
      applyLoadedSource(loaded);
      setPackageContext({ packageId: pkg.packageId, version: resolvedVersion });
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
    setFeatureFlags(source.features);
    setDocument(normalizeOpenCliDocument(source.document, source.options.includeHidden));
    setSearchTerm("");
    setError(null);
    setLoadState({ status: "ready" });
  }

  async function handleFiles(files: File[]) {
    try {
      setLoadState({ status: "loading", message: "Importing local files." });
      const loaded = await loadFromFiles(files, viewerOptions, featureFlags);
      applyLoadedSource(loaded);
      window.location.hash = "#/";
    } catch (err) {
      setError(toMessage(err));
      setLoadState(document ? { status: "ready" } : { status: "empty" });
    }
  }

  function toggleOption(option: keyof ViewerOptions) {
    if (!document) return;
    startTransition(() => {
      setViewerOptions((current) => {
        const next = { ...current, [option]: !current[option] };
        setDocument(normalizeOpenCliDocument(document.source, next.includeHidden));
        return next;
      });
    });
  }

  function toggleComposer() {
    setComposerOpen((prev) => {
      const next = !prev;
      localStorage.setItem("inspectra-composer-open", String(next));
      if (next) setMobileSidebarOpen(false);
      return next;
    });
  }

  function handleComposerResize(width: number) {
    setComposerWidth(width);
    localStorage.setItem("inspectra-composer-width", String(width));
  }

  function buildCurrentHash(commandPath?: string): string {
    if (packageContext) return buildPackageHash(packageContext.packageId, packageContext.version, commandPath);
    return commandPath ? buildCommandHash(commandPath) : "#/";
  }

  function handlePaletteSelect(path: string) {
    window.location.hash = buildCurrentHash(path);
  }

  function handleMobileCommandSelect(path: string) {
    window.location.hash = buildCurrentHash(path);
    setMobileSidebarOpen(false);
  }

  async function handleLoadPackage(opencliUrl: string, xmldocUrl: string, label: string, packageId: string, version: string | undefined) {
    try {
      setLoadState({ status: "loading", message: `Loading ${label}` });
      const loaded = await loadFromUrls(opencliUrl, xmldocUrl, viewerOptions, label, featureFlags);
      applyLoadedSource(loaded);
      setPackageContext({ packageId, version });
      window.location.hash = buildPackageHash(packageId, version);
    } catch (err) {
      setError(toMessage(err));
      setLoadState(document ? { status: "ready" } : { status: "empty" });
    }
  }

  function resetToHome() {
    setPackageContext(null);
    setDocument(null);
    setLoadState({ status: "empty" });
    window.location.hash = "#/";
  }

  return {
    loadState, error, warnings, sourceLabel, viewerOptions, featureFlags,
    document, searchTerm, deferredSearch, route,
    paletteOpen, composerOpen, composerWidth,
    mobileSidebarOpen, mobileSidebarSearch, packageContext,
    searchInputRef, mobileSearchInputRef,
    setSearchTerm, setPaletteOpen, setMobileSidebarOpen, setMobileSidebarSearch, setComposerOpen,
    toggleOption, toggleComposer, handleComposerResize, handleFiles,
    handlePaletteSelect, handleMobileCommandSelect, handleLoadPackage,
    buildCurrentHash, resetToHome,
  };
}
