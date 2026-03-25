import {
  Eye,
  EyeOff,
  FileUp,
  Menu,
  PanelRight,
  PanelRightClose,
  Search,
  Sparkles,
  X,
} from "lucide-react";
import { startTransition, useDeferredValue, useEffect, useRef, useState } from "react";
import { resolveStartupRequest } from "./boot/bootstrap";
import { defaultViewerOptions, ViewerOptions } from "./boot/contracts";
import { CommandPalette } from "./components/CommandPalette";
import { CommandPanel } from "./components/CommandPanel";
import { CommandTree } from "./components/CommandTree";
import { ComposerPanel } from "./components/ComposerPanel";
import { ImportScreen } from "./components/ImportScreen";
import { OverviewPanel } from "./components/OverviewPanel";
import { ThemeToggle } from "./components/ThemeToggle";
import { loadFromFiles, loadFromStartupRequest, LoadedSource } from "./data/loadSource";
import { buildCommandHash, parseHashRoute } from "./data/navigation";
import { findCommandByPath, normalizeOpenCliDocument, NormalizedCliDocument } from "./data/normalize";

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

export function InSpectraApp() {
  const pickerRef = useRef<HTMLInputElement>(null);
  const searchInputRef = useRef<HTMLInputElement>(null);
  const [loadState, setLoadState] = useState<LoadState>({ status: "loading", message: "Resolving viewer boot mode." });
  const [error, setError] = useState<string | null>(null);
  const [warnings, setWarnings] = useState<string[]>([]);
  const [sourceLabel, setSourceLabel] = useState<string>("");
  const [viewerOptions, setViewerOptions] = useState<ViewerOptions>(defaultViewerOptions());
  const [document, setDocument] = useState<NormalizedCliDocument | null>(null);
  const [searchTerm, setSearchTerm] = useState("");
  const [routePath, setRoutePath] = useState<string | undefined>(parseHashRoute(window.location.hash).commandPath);
  const deferredSearch = useDeferredValue(searchTerm);

  const [paletteOpen, setPaletteOpen] = useState(false);
  const [composerOpen, setComposerOpen] = useState(() => readBool("inspectra-composer-open", true));
  const [composerWidth, setComposerWidth] = useState(() => readNumber("inspectra-composer-width", 304));
  const [mobileSidebarOpen, setMobileSidebarOpen] = useState(false);
  const [mobileSidebarSearch, setMobileSidebarSearch] = useState(false);
  const mobileSearchInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    const controller = new AbortController();
    void initialize(controller.signal);

    return () => controller.abort();
  }, []);

  useEffect(() => {
    function handleHashChange() {
      const route = parseHashRoute(window.location.hash);
      setRoutePath(route.commandPath);
    }

    window.addEventListener("hashchange", handleHashChange);
    return () => window.removeEventListener("hashchange", handleHashChange);
  }, []);

  useEffect(() => {
    if (!document) {
      return;
    }

    const route = parseHashRoute(window.location.hash);
    if (route.kind === "command" && !findCommandByPath(document.commands, route.commandPath)) {
      window.location.hash = "#/";
    }
  }, [document]);

  // Keyboard shortcuts: Ctrl+F (focus search), Ctrl+K (palette)
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

  async function initialize(signal: AbortSignal) {
    try {
      const request = resolveStartupRequest({
        search: window.location.search,
        href: window.location.href,
      });
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
    setViewerOptions(source.options);
    setDocument(normalizeOpenCliDocument(source.document, source.options.includeHidden));
    setSearchTerm("");
    setError(null);
    setLoadState({ status: "ready" });
  }

  async function handleFiles(files: File[]) {
    try {
      setLoadState({ status: "loading", message: "Importing local files." });
      const loaded = await loadFromFiles(files, viewerOptions);
      applyLoadedSource(loaded);
      window.location.hash = "#/";
    } catch (loadError) {
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
        const next = {
          ...current,
          [option]: !current[option],
        };

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

  function handlePaletteSelect(path: string) {
    window.location.hash = buildCommandHash(path);
  }

  function handleMobileCommandSelect(path: string) {
    window.location.hash = buildCommandHash(path);
    setMobileSidebarOpen(false);
  }

  if (loadState.status !== "ready" || !document) {
    return (
      <ImportScreen
        error={error}
        loading={loadState.status === "loading"}
        onFilesSelected={handleFiles}
      />
    );
  }

  const activeCommand = findCommandByPath(document.commands, routePath);

  return (
    <div className="app-shell">
      <header className="topbar">
        <button
          type="button"
          className="mobile-menu-btn"
          onClick={() => {
            setMobileSidebarOpen((o) => {
              if (!o) setComposerOpen(false);
              return !o;
            });
          }}
          aria-label={mobileSidebarOpen ? "Close navigation" : "Open navigation"}
        >
          {mobileSidebarOpen ? <X aria-hidden="true" /> : <Menu aria-hidden="true" />}
        </button>

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

        <div className="toolbar">
          <button type="button" className="toolbar-button" onClick={() => toggleOption("includeHidden")}>
            {viewerOptions.includeHidden ? <EyeOff aria-hidden="true" /> : <Eye aria-hidden="true" />}
            <span>{viewerOptions.includeHidden ? "Hide hidden" : "Show hidden"}</span>
          </button>

          <button type="button" className="toolbar-button" onClick={() => toggleOption("includeMetadata")}>
            <Sparkles aria-hidden="true" />
            <span>{viewerOptions.includeMetadata ? "Hide metadata" : "Show metadata"}</span>
          </button>

          <button type="button" className="toolbar-button" onClick={() => pickerRef.current?.click()}>
            <FileUp aria-hidden="true" />
            <span>Import</span>
          </button>
          <input
            ref={pickerRef}
            className="visually-hidden"
            type="file"
            multiple
            accept=".json,.xml"
            onChange={(event) => {
              void handleFiles(Array.from(event.target.files ?? []));
              event.target.value = "";
            }}
          />

          <button type="button" className="toolbar-button" onClick={() => setPaletteOpen(true)} title="Search commands (Ctrl+K)">
            <Search aria-hidden="true" />
            <span>Search</span>
            <kbd className="kbd-hint">Ctrl K</kbd>
          </button>

          <button
            type="button"
            className={`toolbar-button composer-toggle${composerOpen ? " active" : ""}`}
            onClick={toggleComposer}
            title="Toggle Composer"
          >
            {composerOpen ? <PanelRightClose aria-hidden="true" /> : <PanelRight aria-hidden="true" />}
            <span>Composer</span>
          </button>

          <ThemeToggle />
        </div>
      </header>

      <div className="app-grid">
        {(mobileSidebarOpen || composerOpen) && (
          <div
            className="mobile-drawer-overlay"
            onClick={() => {
              setMobileSidebarOpen(false);
              setMobileSidebarSearch(false);
              setSearchTerm("");
              setComposerOpen(false);
            }}
            aria-hidden="true"
          />
        )}

        <aside className={`sidebar${mobileSidebarOpen ? " sidebar-open" : ""}`}>
          <div className="sidebar-header">
            {mobileSidebarSearch ? (
              <div className="sidebar-header-search">
                <input
                  ref={mobileSearchInputRef}
                  type="search"
                  placeholder="Filter commands…"
                  value={searchTerm}
                  onChange={(event) => setSearchTerm(event.target.value)}
                  autoFocus
                />
                <button
                  type="button"
                  className="sidebar-header-btn"
                  onClick={() => {
                    setMobileSidebarSearch(false);
                    setSearchTerm("");
                  }}
                  aria-label="Close search"
                >
                  <X aria-hidden="true" />
                </button>
              </div>
            ) : (
              <>
                <Menu aria-hidden="true" className="sidebar-header-icon" />
                <span className="sidebar-header-title">Navigation</span>
                <button
                  type="button"
                  className="sidebar-header-btn"
                  onClick={() => setMobileSidebarSearch(true)}
                  aria-label="Search commands"
                >
                  <Search aria-hidden="true" />
                </button>
                <button
                  type="button"
                  className="sidebar-header-btn"
                  onClick={() => {
                    setMobileSidebarOpen(false);
                    setMobileSidebarSearch(false);
                    setSearchTerm("");
                  }}
                  aria-label="Close navigation"
                >
                  <X aria-hidden="true" />
                </button>
              </>
            )}
          </div>

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
            <button
              type="button"
              className={`overview-row ${!activeCommand ? "selected" : ""}`}
              onClick={() => {
                window.location.hash = "#/";
                setMobileSidebarOpen(false);
              }}
            >
              Overview
            </button>
            <div className="nav-label">Commands</div>
            <CommandTree
              commands={document.commands}
              searchTerm={deferredSearch}
              selectedPath={activeCommand?.path}
              onSelect={handleMobileCommandSelect}
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

        {composerOpen && (
          <ComposerPanel
            command={activeCommand}
            cliTitle={document.source.info.title || "cli"}
            width={composerWidth}
            onResize={handleComposerResize}
          />
        )}
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
