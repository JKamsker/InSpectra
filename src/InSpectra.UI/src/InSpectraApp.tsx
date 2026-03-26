import { Home, Menu, PanelRight, PanelRightClose, Search, X } from "lucide-react";
import { CommandPalette } from "./components/CommandPalette";
import { CommandPanel } from "./components/CommandPanel";
import { CommandTree } from "./components/CommandTree";
import { ComposerPanel } from "./components/ComposerPanel";
import { ImportScreen } from "./components/ImportScreen";
import { NugetBrowser } from "./components/NugetBrowser";
import { PackageLoadingScreen } from "./components/PackageLoadingScreen";
import { OverviewPanel } from "./components/OverviewPanel";
import { ThemeToggle } from "./components/ThemeToggle";
import { useAppState } from "./hooks/useAppState";
import { findCommandByPath } from "./data/normalize";

export function InSpectraApp() {
  const {
    loadState, error, warnings, sourceLabel, viewerOptions, featureFlags,
    document, searchTerm, deferredSearch, route,
    paletteOpen, composerOpen, composerWidth,
    mobileSidebarOpen, mobileSidebarSearch,
    searchInputRef, mobileSearchInputRef,
    setSearchTerm, setPaletteOpen, setMobileSidebarOpen, setMobileSidebarSearch, setComposerOpen,
    toggleComposer, handleComposerResize, handleFiles,
    handlePaletteSelect, handleMobileCommandSelect,
    handleLoadPackage, buildCurrentHash, resetToHome,
  } = useAppState();

  if (route.kind === "browse") {
    if (!featureFlags.nugetBrowser) {
      window.location.hash = "#/";
      return null;
    }
    return (
      <NugetBrowser
        packageId={route.packageId}
        version={route.version}
        onLoadPackage={handleLoadPackage}
        onBack={() => { window.location.hash = "#/"; }}
      />
    );
  }

  if (loadState.status !== "ready" || !document) {
    if (loadState.status === "loading") {
      return <PackageLoadingScreen message={loadState.message} />;
    }
    return (
      <ImportScreen
        error={error}
        loading={false}
        onFilesSelected={handleFiles}
        showUpload={featureFlags.packageUpload}
        showNugetBrowser={featureFlags.nugetBrowser}
      />
    );
  }

  const commandPath =
    route.kind === "command" ? route.commandPath :
    route.kind === "package" ? route.commandPath :
    undefined;
  const activeCommand = findCommandByPath(document.commands, commandPath);
  const isEmptyPackage =
    document.commands.length === 0 &&
    document.rootArguments.length === 0 &&
    document.rootOptions.length === 0;

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
          {featureFlags.showHome && (
            <button type="button" className="toolbar-button" onClick={resetToHome} title="Back to start">
              <Home aria-hidden="true" />
              <span>Home</span>
            </button>
          )}

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

          {featureFlags.darkTheme && featureFlags.lightTheme && <ThemeToggle />}
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
              onClick={() => { window.location.hash = buildCurrentHash(); setMobileSidebarOpen(false); }}
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
                includeMetadata={viewerOptions.includeMetadata}
                onCommandSelect={(path) => { window.location.hash = buildCurrentHash(path); }}
                deepLinkHash={buildCurrentHash(activeCommand.path)}
              />
            ) : (
              <OverviewPanel
                document={document}
                includeMetadata={viewerOptions.includeMetadata}
                onCommandSelect={(path) => { window.location.hash = buildCurrentHash(path); }}
              />
            )}
          </div>
        </main>

        {featureFlags.composer && composerOpen && !isEmptyPackage && (
          <ComposerPanel
            command={activeCommand}
            cliTitle={document.source.info.title || "cli"}
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
        onSelect={handlePaletteSelect}
      />
    </div>
  );
}
