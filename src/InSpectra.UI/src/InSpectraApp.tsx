import { CliViewer } from "./components/CliViewer";
import { ImportScreen } from "./components/ImportScreen";
import { NugetBrowser } from "./components/NugetBrowser";
import { PackageLoadingScreen } from "./components/PackageLoadingScreen";
import { useAppState } from "./hooks/useAppState";

import { buildCommandHash, buildPackageHash } from "./data/navigation";

export function InSpectraApp() {
  const {
    loadState, error, warnings, sourceLabel, viewerOptions, featureFlags,
    document, route,
    packageContext,
    handleFiles,
    handleLoadPackage,
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
        onBackToBrowse={() => { window.location.hash = "#/browse"; }}
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
      />
    );
  }

  const commandPath = route.kind === "command" || route.kind === "package" ? route.commandPath : undefined;

  function handleNavigate(path?: string) {
    if (packageContext) {
      window.location.hash = buildPackageHash(packageContext.packageId, packageContext.version, path);
    } else {
      window.location.hash = path ? buildCommandHash(path) : "#/";
    }
  }

  return (
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
  );
}
