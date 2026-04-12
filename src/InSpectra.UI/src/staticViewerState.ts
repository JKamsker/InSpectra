import { Dispatch, SetStateAction } from "react";
import { FeatureFlags, ViewerOptions } from "./boot/contracts";
import { loadFromUrls, LoadedSource } from "./data/loadSource";
import { HashRoute, parseHashRoute } from "./data/navigation";
import { buildViewerState, PackageContext, ViewerState } from "./staticViewerSupport";
import { toMessage } from "./utils";

export interface LoadState {
  status: "loading" | "ready" | "empty" | "error";
  message?: string;
}

export interface PendingPackageLoad {
  key: string;
  origin: "browse-route" | "package-route";
  packageId: string;
  version?: string;
  browseVersion?: string;
}

export interface StaticViewerStateSetters {
  setFeatureFlags: Dispatch<SetStateAction<FeatureFlags>>;
  setHomeViewer: Dispatch<SetStateAction<ViewerState | null>>;
  setPackageViewer: Dispatch<SetStateAction<ViewerState | null>>;
  setPackageContext: Dispatch<SetStateAction<PackageContext | null>>;
  setPendingPackageLoad: Dispatch<SetStateAction<PendingPackageLoad | null>>;
  setError: Dispatch<SetStateAction<string | null>>;
  setLoadState: Dispatch<SetStateAction<LoadState>>;
}

export function applyHomeSourceState(source: LoadedSource, setters: StaticViewerStateSetters) {
  setters.setHomeViewer(buildViewerState(source));
  setters.setPackageViewer(null);
  setters.setPackageContext(null);
  setters.setPendingPackageLoad(null);
  setters.setFeatureFlags(source.features);
  setters.setError(null);
  setters.setLoadState({ status: "ready" });
}

export function applyPackageSourceState(
  source: LoadedSource,
  nextPackageContext: PackageContext,
  setters: StaticViewerStateSetters,
) {
  setters.setPackageViewer(buildViewerState(source));
  setters.setPackageContext(nextPackageContext);
  setters.setPendingPackageLoad(null);
  setters.setFeatureFlags(source.features);
  setters.setError(null);
  setters.setLoadState({ status: "ready" });
}

export function setLoadErrorState(
  err: unknown,
  fallbackViewer: ViewerState | null,
  setters: Pick<StaticViewerStateSetters, "setError" | "setLoadState">,
  fallbackStatus: LoadState["status"] = "empty",
) {
  setters.setError(toMessage(err));
  setters.setLoadState(fallbackViewer ? { status: "ready" } : { status: fallbackStatus });
}

export function clearPendingPackageLoadState(
  requestKey: string,
  setPendingPackageLoad: Dispatch<SetStateAction<PendingPackageLoad | null>>,
) {
  setPendingPackageLoad((current) => (current?.key === requestKey ? null : current));
}

export function getPackageRequestKey(packageId: string, version: string | undefined): string {
  return `${packageId.toLowerCase()}/${version ?? "latest"}`;
}

export async function loadPackageDocument(params: {
  requestKey: string;
  origin: PendingPackageLoad["origin"];
  label: string;
  packageId: string;
  version: string | undefined;
  browseVersion?: string;
  command: string | undefined;
  opencliUrl: string;
  xmldocUrl: string | undefined;
  viewerOptions: ViewerOptions;
  featureFlags: FeatureFlags;
  homeViewer: ViewerState | null;
  fallbackViewer: ViewerState | null;
  fallbackStatus?: LoadState["status"];
  stateSetters: StaticViewerStateSetters;
  onError?: (message: string) => void;
  onSuccess?: () => void;
}) {
  const pendingLoad: PendingPackageLoad = {
    key: params.requestKey,
    origin: params.origin,
    packageId: params.packageId,
    version: params.version,
    browseVersion: params.browseVersion,
  };

  try {
    params.stateSetters.setPendingPackageLoad(pendingLoad);
    params.stateSetters.setError(null);
    params.stateSetters.setLoadState({ status: "loading", message: `Loading ${params.label}` });
    const loaded = await loadFromUrls(params.opencliUrl, params.xmldocUrl, params.viewerOptions, params.label, params.featureFlags, {
      title: params.packageId,
      commandPrefix: params.command,
    });
    if (didLeavePendingPackageLoad(parseHashRoute(window.location.hash), pendingLoad)) {
      clearPendingPackageLoadState(params.requestKey, params.stateSetters.setPendingPackageLoad);
      params.stateSetters.setLoadState(params.homeViewer ? { status: "ready" } : { status: "empty" });
      return;
    }

    clearPendingPackageLoadState(params.requestKey, params.stateSetters.setPendingPackageLoad);
    applyPackageSourceState(loaded, {
      packageId: params.packageId,
      version: params.version,
      command: params.command,
    }, params.stateSetters);
    params.onSuccess?.();
  } catch (err) {
    clearPendingPackageLoadState(params.requestKey, params.stateSetters.setPendingPackageLoad);
    if (didLeavePendingPackageLoad(parseHashRoute(window.location.hash), pendingLoad)) {
      params.stateSetters.setLoadState(params.homeViewer ? { status: "ready" } : { status: "empty" });
      return;
    }

    if (params.onError) {
      params.onError(toMessage(err));
      params.stateSetters.setLoadState(params.fallbackViewer ? { status: "ready" } : { status: params.fallbackStatus ?? "empty" });
      return;
    }

    setLoadErrorState(err, params.fallbackViewer, params.stateSetters, params.fallbackStatus);
  }
}

function normalizeVersion(version: string | undefined): string {
  return (version ?? "latest").toLowerCase();
}

export function didLeavePendingPackageLoad(route: HashRoute, pendingLoad: PendingPackageLoad): boolean {
  const stillOnTargetPackage =
    route.kind === "package" &&
    route.packageId.toLowerCase() === pendingLoad.packageId.toLowerCase() &&
    normalizeVersion(route.version) === normalizeVersion(pendingLoad.version);

  if (pendingLoad.origin === "package-route") {
    return !stillOnTargetPackage;
  }

  const stillOnOriginBrowse =
    route.kind === "browse" &&
    route.packageId?.toLowerCase() === pendingLoad.packageId.toLowerCase() &&
    normalizeVersion(route.version) === normalizeVersion(pendingLoad.browseVersion);

  return !(stillOnOriginBrowse || stillOnTargetPackage);
}
