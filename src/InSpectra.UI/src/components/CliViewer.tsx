import { Home, Menu, Package, PanelRight, PanelRightClose, Search, Upload, X } from "lucide-react";
import { useEffect, useRef, useState } from "react";
import { CommandPalette } from "./CommandPalette";
import { CommandPanel } from "./CommandPanel";
import { CommandTree } from "./CommandTree";
import { ComposerPanel } from "./ComposerPanel";
import { OverviewPanel } from "./OverviewPanel";
import { ThemeToggle } from "./ThemeToggle";
import { useViewerInteraction } from "../hooks/useViewerInteraction";
import { findCommandByPath, NormalizedCliDocument } from "../data/normalize";
import { buildBrowseHash, buildCommandHash, buildPackageHash } from "../data/navigation";
import { FeatureFlags, ViewerOptions } from "../boot/contracts";
import { PackageContext, ViewerToolbarRoutes } from "../staticViewerSupport";

interface CliViewerProps {
  document: NormalizedCliDocument;
  viewerOptions: ViewerOptions;
  featureFlags: FeatureFlags;
  packageContext: PackageContext | null;
  sourceLabel: string;
  warnings: string[];
  error: string | null;
  commandPath: string | undefined;
  onNavigate: (commandPath?: string) => void;
  showThemeToggle?: boolean;
  toolbarRoutes?: ViewerToolbarRoutes;
}

export function CliViewer({
  document,
  viewerOptions,
  featureFlags,
  packageContext,
  sourceLabel,
  warnings,
  error,
  commandPath,
  onNavigate,
  showThemeToggle = true,
  toolbarRoutes,
}: CliViewerProps) {
  const {
    searchInputRef,
    mobileSearchInputRef,
    searchTerm,
    deferredSearch,
    paletteOpen,
    composerOpen,
    composerOpenedByUser,
    composerWidth,
    mobileSidebarOpen,
    mobileSidebarSearch,
    setSearchTerm,
    setPaletteOpen,
    setComposerOpen,
    setMobileSidebarOpen,
    setMobileSidebarSearch,
    toggleComposer,
    handleComposerResize,
    handleMobileCommandSelect,
  } = useViewerInteraction();

  const activeCommand = findCommandByPath(document.commands, commandPath);

  function buildCurrentHash(path?: string): string {
    if (packageContext) return buildPackageHash(packageContext.packageId, packageContext.version, path);
    return path ? buildCommandHash(path) : "#/";
  }
  const isEmptyPackage =
    document.commands.length === 0 &&
    document.rootArguments.length === 0 &&
    document.rootOptions.length === 0;
  const cliPrefix = packageContext?.command || viewerOptions.commandPrefix || toCliPrefix(document.source.info.title) || "cli";
  const routes = toolbarRoutes ?? {
    homeHref: "#/",
    importHref: "#/import",
  };

  const gridRef = useRef<HTMLDivElement>(null);
  const [composerFloating, setComposerFloating] = useState(false);

  // Track whether the user has ever activated the composer.
  // Once they open it (at least once), the hint animation stops.
  const [hasUsedComposer, setHasUsedComposer] = useState(() => {
    try {
      return localStorage.getItem("inspectra-composer-used") === "true";
    } catch {
      return false;
    }
  });

  useEffect(() => {
    // Only mark the composer as "used" when the user explicitly opens it.
    // Auto-expansion (from saved prefs / default) shouldn't dismiss the hint.
    if (composerOpen && !hasUsedComposer && composerOpenedByUser.current) {
      setHasUsedComposer(true);
      try {
        localStorage.setItem("inspectra-composer-used", "true");
      } catch {
        /* ignore */
      }
    }
  }, [composerOpen, hasUsedComposer, composerOpenedByUser]);

  const showComposerHint =
    featureFlags.composer &&
    !isEmptyPackage &&
    !composerOpen &&
    !hasUsedComposer &&
    !!activeCommand;

  useEffect(() => {
    const grid = gridRef.current;
    if (!grid) return;

    function update() {
      if (!composerOpen || window.innerWidth <= 768) {
        setComposerFloating(false);
        return;
      }
      const rem = parseFloat(getComputedStyle(window.document.documentElement).fontSize);
      const sidebarWidth = 17 * rem;
      const wouldFloat = grid!.offsetWidth - sidebarWidth - composerWidth < 490;

      if (wouldFloat && !composerOpenedByUser.current) {
        // Auto-collapse when the default open state would cause floating
        setComposerOpen(false);
        setComposerFloating(false);
      } else {
        setComposerFloating(wouldFloat);
      }
    }

    const ro = new ResizeObserver(update);
    ro.observe(grid);
    update();
    return () => ro.disconnect();
  }, [composerOpen, composerWidth]);

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

        {(() => {
          const brandInner = (
            <>
              <span className="brand-mark">{">_"}</span>
              <div className="brand-info">
                <div className="brand-title">{document.source.info.title || "OpenCLI Viewer"}</div>
                <div className="brand-subtitle">
                  <span>{sourceLabel || `v${document.source.info.version || "0.0.0"}`}</span>
                  <span>OpenCLI {document.source.opencli || "unknown"}</span>
                </div>
              </div>
            </>
          );
          return packageContext ? (
            <a
              href={buildBrowseHash(packageContext.packageId)}
              className="brand-block brand-link"
              title={`Browse ${packageContext.packageId} versions`}
            >
              {brandInner}
            </a>
          ) : (
            <div className="brand-block">{brandInner}</div>
          );
        })()}

        <div className="toolbar">
          <div className="toolbar-group toolbar-group-secondary">
            {routes.homeHref && (
              <a href={routes.homeHref} className="toolbar-button" title="Home">
                <Home aria-hidden="true" />
                <span>Home</span>
              </a>
            )}

            {routes.browseHref && (
              <a href={routes.browseHref} className="toolbar-button" title="Browse packages">
                <Package aria-hidden="true" />
                <span>Browse</span>
              </a>
            )}

            {routes.importHref && (
              <a href={routes.importHref} className="toolbar-button" title="Import OpenCLI files">
                <Upload aria-hidden="true" />
                <span>Import</span>
              </a>
            )}

            {showThemeToggle && featureFlags.darkTheme && featureFlags.lightTheme && (
              <ThemeToggle colorThemePicker={featureFlags.colorThemePicker} />
            )}
          </div>

          <span className="toolbar-divider" aria-hidden="true" />

          <button type="button" className="toolbar-button" onClick={() => setPaletteOpen(true)} title="Search commands (Ctrl+K)">
            <Search aria-hidden="true" />
            <span>Search</span>
            <kbd className="kbd-hint">Ctrl K</kbd>
          </button>

          {featureFlags.composer && !isEmptyPackage && (
            <button
              type="button"
              className={`toolbar-button composer-toggle${composerOpen ? " active" : ""}${showComposerHint ? " composer-hint" : ""}`}
              onClick={toggleComposer}
              title={showComposerHint ? "Try the Composer — build a command visually" : "Toggle Composer"}
            >
              {composerOpen ? <PanelRightClose aria-hidden="true" /> : <PanelRight aria-hidden="true" />}
              <span>Composer</span>
            </button>
          )}
        </div>
      </header>

      <div className={`app-grid${composerFloating ? " composer-float-mode" : ""}`} ref={gridRef}>
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
                  onClick={() => { setMobileSidebarSearch(false); setSearchTerm(""); }}
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
                  onClick={() => { setMobileSidebarOpen(false); setMobileSidebarSearch(false); setSearchTerm(""); }}
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
              onClick={() => { onNavigate(undefined); setMobileSidebarOpen(false); }}
            >
              Overview
            </button>
            <div className="nav-label">Commands</div>
            <CommandTree
              commands={document.commands}
              searchTerm={deferredSearch}
              selectedPath={activeCommand?.path}
              onSelect={(path, hasChildren) => handleMobileCommandSelect(path, onNavigate, hasChildren)}
            />
          </nav>
        </aside>

        <main className="content-column">
          <div className="content-stack" key={activeCommand?.path ?? "overview"}>
            {warnings.length > 0 && (
              <div className="warning-banner" role="status">
                {warnings.map((warning) => <p key={warning}>{warning}</p>)}
              </div>
            )}

            {error && (
              <div className="warning-banner" role="alert">
                <p>{error}</p>
              </div>
            )}

            {activeCommand ? (
              <CommandPanel
                command={activeCommand}
                cliPrefix={cliPrefix}
                includeMetadata={viewerOptions.includeMetadata}
                onCommandSelect={(path) => { onNavigate(path ?? undefined); }}
                deepLinkHash={buildCurrentHash(activeCommand.path)}
              />
            ) : (
              <OverviewPanel
                document={document}
                includeMetadata={viewerOptions.includeMetadata}
                onCommandSelect={(path) => { onNavigate(path); }}
              />
            )}
          </div>
        </main>

        {featureFlags.composer && !isEmptyPackage && (
          <ComposerPanel
            command={activeCommand}
            cliPrefix={cliPrefix}
            isOpen={composerOpen}
            width={composerWidth}
            onResize={handleComposerResize}
            rootArguments={document.rootArguments}
            rootOptions={document.rootOptions}
          />
        )}
      </div>

      <CommandPalette
        commands={document.commands}
        open={paletteOpen}
        onClose={() => setPaletteOpen(false)}
        onSelect={(path) => onNavigate(path)}
      />
    </div>
  );
}

function toCliPrefix(title: string | undefined): string | undefined {
  if (!title) return undefined;
  const slug = title
    .trim()
    .replace(/([a-z])([A-Z])/g, "$1-$2")
    .replace(/\s+/g, "-")
    .toLowerCase()
    .replace(/[^a-z0-9._-]/g, "");
  return slug || undefined;
}
