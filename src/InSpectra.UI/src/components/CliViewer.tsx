import { Menu, PanelRight, PanelRightClose, Search, X, ArrowLeft } from "lucide-react";
import { CommandPalette } from "./CommandPalette";
import { CommandPanel } from "./CommandPanel";
import { CommandTree } from "./CommandTree";
import { ComposerPanel } from "./ComposerPanel";
import { OverviewPanel } from "./OverviewPanel";
import { ThemeToggle } from "./ThemeToggle";
import { useViewerInteraction } from "../hooks/useViewerInteraction";
import { findCommandByPath, NormalizedCliDocument } from "../data/normalize";
import { buildCommandHash, buildPackageHash } from "../data/navigation";
import { FeatureFlags, ViewerOptions } from "../boot/contracts";

interface CliViewerProps {
  document: NormalizedCliDocument;
  viewerOptions: ViewerOptions;
  featureFlags: FeatureFlags;
  packageContext: { packageId: string; version?: string; command?: string } | null;
  sourceLabel: string;
  warnings: string[];
  error: string | null;
  commandPath: string | undefined;
  onNavigate: (commandPath?: string) => void;
  onBack?: () => void;
  showThemeToggle?: boolean;
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
  onBack,
  showThemeToggle = true,
}: CliViewerProps) {
  const {
    searchInputRef,
    mobileSearchInputRef,
    searchTerm,
    deferredSearch,
    paletteOpen,
    composerOpen,
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
  const cliPrefix = packageContext?.command || toCliPrefix(document.source.info.title) || "cli";

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
          {onBack && (
            <button type="button" className="toolbar-button" onClick={onBack} title="Back to browser">
              <ArrowLeft aria-hidden="true" />
              <span>Back</span>
            </button>
          )}
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
          <button type="button" className="toolbar-button" onClick={() => setPaletteOpen(true)} title="Search commands (Ctrl+K)">
            <Search aria-hidden="true" />
            <span>Search</span>
            <kbd className="kbd-hint">Ctrl K</kbd>
          </button>

          {featureFlags.composer && !isEmptyPackage && (
            <button
              type="button"
              className={`toolbar-button composer-toggle${composerOpen ? " active" : ""}`}
              onClick={toggleComposer}
              title="Toggle Composer"
            >
              {composerOpen ? <PanelRightClose aria-hidden="true" /> : <PanelRight aria-hidden="true" />}
              <span>Composer</span>
            </button>
          )}

          {showThemeToggle && featureFlags.darkTheme && featureFlags.lightTheme && <ThemeToggle />}
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
              onSelect={(path) => handleMobileCommandSelect(path, onNavigate)}
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
