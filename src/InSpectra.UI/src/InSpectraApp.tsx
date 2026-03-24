import { startTransition, useDeferredValue, useEffect, useRef, useState } from "react";
import { resolveStartupRequest } from "./boot/bootstrap";
import { defaultViewerOptions, ViewerOptions } from "./boot/contracts";
import { loadFromNugetTool, NugetToolProbeError } from "./data/loadNugetTool";
import { loadFromFiles, loadFromStartupRequest, LoadedSource } from "./data/loadSource";
import { buildCommandHash, parseHashRoute } from "./data/navigation";
import { findCommandByPath, normalizeOpenCliDocument, NormalizedCliDocument } from "./data/normalize";
import { AppToolbar } from "./components/AppToolbar";
import { CommandPalette } from "./components/CommandPalette";
import { CommandPanel } from "./components/CommandPanel";
import { CommandTree } from "./components/CommandTree";
import { ComposerPanel } from "./components/ComposerPanel";
import { ImportScreen } from "./components/ImportScreen";
import { OverviewPanel } from "./components/OverviewPanel";
import { SourceSummaryCard } from "./components/SourceSummaryCard";
import { ProbeDiagnostics, ProbePackageSummary } from "./data/toolProbe";

interface LoadState { status: "loading" | "ready" | "empty"; message?: string; }

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

export function InSpectraApp() {
  const searchInputRef = useRef<HTMLInputElement>(null);
  const [importMode, setImportMode] = useState<"files" | "nuget">("files");
  const [loadState, setLoadState] = useState<LoadState>({ status: "loading", message: "Resolving viewer boot mode." });
  const [error, setError] = useState<string | null>(null);
  const [warnings, setWarnings] = useState<string[]>([]);
  const [sourceLabel, setSourceLabel] = useState<string>("");
  const [probeDiagnostics, setProbeDiagnostics] = useState<ProbeDiagnostics | null>(null);
  const [probeSummary, setProbeSummary] = useState<ProbePackageSummary | null>(null);
  const [viewerOptions, setViewerOptions] = useState<ViewerOptions>(defaultViewerOptions());
  const [document, setDocument] = useState<NormalizedCliDocument | null>(null);
  const [searchTerm, setSearchTerm] = useState("");
  const [routePath, setRoutePath] = useState<string | undefined>(parseHashRoute(window.location.hash).commandPath);
  const [paletteOpen, setPaletteOpen] = useState(false);
  const [composerOpen, setComposerOpen] = useState(() => readBool("inspectra-composer-open", true));
  const [composerWidth, setComposerWidth] = useState(() => readNumber("inspectra-composer-width", 304));
  const deferredSearch = useDeferredValue(searchTerm);

  useEffect(() => {
    const controller = new AbortController();
    void initialize(controller.signal);
    return () => controller.abort();
  }, []);
  useEffect(() => {
    function handleHashChange() {
      setRoutePath(parseHashRoute(window.location.hash).commandPath);
    }
    window.addEventListener("hashchange", handleHashChange);
    return () => window.removeEventListener("hashchange", handleHashChange);
  }, []);
  useEffect(() => {
    if (!document) return;
    const route = parseHashRoute(window.location.hash);
    if (route.kind === "command" && !findCommandByPath(document.commands, route.commandPath)) {
      window.location.hash = "#/";
    }
  }, [document]);

  useEffect(() => {
    function handleKeyDown(event: KeyboardEvent) {
      const mod = event.ctrlKey || event.metaKey;
      if (mod && event.key === "f") {
        event.preventDefault();
        searchInputRef.current?.focus();
      }

      if (mod && event.key === "k") {
        event.preventDefault();
        setPaletteOpen((open) => !open);
      }
    }

    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, []);
  async function initialize(signal: AbortSignal) {
    try {
      const request = resolveStartupRequest({ search: window.location.search, href: window.location.href });
      const loaded = await loadFromStartupRequest(request, signal);
      if (!loaded) {
        setLoadState({ status: "empty" });
        return;
      }
      applyLoadedSource(loaded);
    } catch (loadError) {
      setError(toMessage(loadError));
      setLoadState({ status: "empty" });
    }
  }
  function applyLoadedSource(source: LoadedSource) {
    setWarnings(source.warnings);
    setSourceLabel(source.label);
    setProbeDiagnostics(null);
    setProbeSummary(source.probeSummary ?? null);
    setViewerOptions(source.options);
    setDocument(normalizeOpenCliDocument(source.document, source.options.includeHidden));
    setSearchTerm("");
    setError(null);
    setLoadState({ status: "ready" });
  }
  function clearNugetTransientState() { setError(null); setProbeDiagnostics(null); }
  function handleImportModeChange(mode: "files" | "nuget") { clearNugetTransientState(); setImportMode(mode); }
  async function handleFiles(files: File[]) {
    try {
      setLoadState({ status: "loading", message: "Importing local files." });
      applyLoadedSource(await loadFromFiles(files, viewerOptions));
      window.location.hash = "#/";
    } catch (loadError) {
      setProbeDiagnostics(null);
      setError(toMessage(loadError));
      setLoadState(document ? { status: "ready" } : { status: "empty" });
    }
  }

  async function handleNugetTool(request: { id: string; version: string }) {
    try {
      setLoadState({ status: "loading", message: "Downloading and probing the NuGet tool." });
      applyLoadedSource(await loadFromNugetTool(request, viewerOptions));
      window.location.hash = "#/";
    } catch (loadError) {
      setProbeDiagnostics(loadError instanceof NugetToolProbeError ? loadError.diagnostics : null);
      setError(toMessage(loadError));
      setLoadState(document ? { status: "ready" } : { status: "empty" });
    }
  }

  function toggleOption(option: keyof ViewerOptions) {
    if (!document) {
      return;
    }
    startTransition(() => {
      setViewerOptions((current) => {
        const next = { ...current, [option]: !current[option] };
        setDocument(normalizeOpenCliDocument(document.source, next.includeHidden));
        return next;
      });
    });
  }

  function resetToImportScreen() {
    setDocument(null);
    setWarnings([]);
    setError(null);
    setSourceLabel("");
    setProbeDiagnostics(null);
    setProbeSummary(null);
    setSearchTerm("");
    setLoadState({ status: "empty" });
  }
  function toggleComposer() {
    setComposerOpen((prev) => {
      const next = !prev;
      localStorage.setItem("inspectra-composer-open", String(next));
      return next;
    });
  }
  function handleComposerResize(width: number) {
    setComposerWidth(width);
    localStorage.setItem("inspectra-composer-width", String(width));
  }

  function handlePaletteSelect(path: string) {
    window.location.hash = buildCommandHash(path);
  }

  function downloadCurrentDocument() {
    if (!document) {
      return;
    }

    const blob = new Blob([JSON.stringify(document.source, null, 2)], { type: "application/json" });
    const url = URL.createObjectURL(blob);
    const link = window.document.createElement("a");
    link.href = url;
    link.download = "opencli.generated.json";
    link.click();
    URL.revokeObjectURL(url);
  }

  if (loadState.status !== "ready" || !document) {
    return (
      <ImportScreen
        error={error}
        loading={loadState.status === "loading"}
        mode={importMode}
        onFilesSelected={handleFiles}
        onModeChange={handleImportModeChange}
        onNugetInteraction={clearNugetTransientState}
        onToolInspect={handleNugetTool}
        probeDiagnostics={probeDiagnostics}
      />
    );
  }

  const activeCommand = findCommandByPath(document.commands, routePath);

  return (
    <div className="app-shell">
      <header className="topbar">
        <div className="brand-block">
          <span className="brand-mark">{">_"}</span>
          <div className="brand-info">
            <div className="brand-title">{document.source.info.title || "OpenCLI Viewer"}</div>
            <div className="brand-subtitle">
              <span>{sourceLabel || `v${document.source.info.version || "0.0.0"}`}</span>
              <span>OpenCLI {document.source.opencli || "unknown"}</span>
            </div>
          </div>
        </div>

        <AppToolbar
          composerOpen={composerOpen}
          hasDocument={true}
          onDownloadJson={downloadCurrentDocument}
          onFilesSelected={(files) => void handleFiles(files)}
          onPaletteOpen={() => setPaletteOpen(true)}
          onResetToImport={resetToImportScreen}
          onToggleComposer={toggleComposer}
          onToggleOption={toggleOption}
          viewerOptions={viewerOptions}
        />
      </header>

      <div className="app-grid">
        <aside className="sidebar">
          <div className="sidebar-search">
            <input
              ref={searchInputRef}
              type="search"
              placeholder="Filter commands…"
              value={searchTerm}
              onChange={(event) => setSearchTerm(event.target.value)}
            />
            <kbd className="kbd-hint sidebar-kbd">Ctrl F</kbd>
          </div>
          <nav className="sidebar-nav">
            <button type="button" className={`overview-row ${!activeCommand ? "selected" : ""}`} onClick={() => {
              window.location.hash = "#/";
            }}>
              Overview
            </button>
            <div className="nav-label">Commands</div>
            <CommandTree
              commands={document.commands}
              searchTerm={deferredSearch}
              selectedPath={activeCommand?.path}
              onSelect={(path) => {
                window.location.hash = buildCommandHash(path);
              }}
            />
          </nav>
        </aside>

        <main className="content-column">
          <div className="content-stack" key={activeCommand?.path ?? "overview"}>
            {warnings.length > 0 ? (
              <div className="warning-banner" role="status">
                {warnings.map((warning) => (
                  <p key={warning}>{warning}</p>
                ))}
              </div>
            ) : null}

            {error ? (
              <div className="warning-banner" role="alert">
                <p>{error}</p>
              </div>
            ) : null}

            {probeSummary ? <SourceSummaryCard summary={probeSummary} /> : null}

            {activeCommand ? (
              <CommandPanel
                command={activeCommand}
                includeMetadata={viewerOptions.includeMetadata}
                onCommandSelect={(path) => {
                  window.location.hash = buildCommandHash(path);
                }}
              />
            ) : (
              <OverviewPanel
                document={document}
                includeMetadata={viewerOptions.includeMetadata}
                onCommandSelect={(path) => {
                  window.location.hash = buildCommandHash(path);
                }}
              />
            )}
          </div>
        </main>

        {composerOpen ? (
          <ComposerPanel
            command={activeCommand}
            cliTitle={document.source.info.title || "cli"}
            width={composerWidth}
            onResize={handleComposerResize}
          />
        ) : null}
      </div>

      <CommandPalette
        commands={document.commands}
        open={paletteOpen}
        onClose={() => setPaletteOpen(false)}
        onSelect={handlePaletteSelect}
      />
    </div>
  );
}

function toMessage(error: unknown): string {
  return error instanceof Error ? error.message : "Unknown error.";
}
