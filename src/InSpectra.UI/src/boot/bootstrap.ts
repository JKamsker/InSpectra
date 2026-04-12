import { toMessage } from "../utils";
import { defaultFeatureFlags, defaultViewerOptions, disabledFeatureFlags, FeatureFlags, InSpectraBootstrap, ViewerOptions } from "./contracts";
import { resolveViewerLinks, resolveViewerLinksFromSearch, ViewerLinks } from "./urlParams";

export type StartupRequest =
  | {
      kind: "inline";
      openCli: unknown;
      xmlDoc?: string;
      options: ViewerOptions;
      features: FeatureFlags;
    }
  | {
      kind: "links";
      links: ViewerLinks;
      options: ViewerOptions;
      features: FeatureFlags;
      source: "bootstrap" | "query";
    }
  | {
      kind: "empty";
      features: FeatureFlags;
    };

export async function readInjectedBootstrap(documentRef: Document = document): Promise<InSpectraBootstrap | null> {
  // Fast path: pre-parsed via JSON.parse string literal (V8-optimized)
  const win = (typeof window !== "undefined" ? window : undefined) as (Window & { __inspectraBootstrap?: InSpectraBootstrap }) | undefined;
  if (win?.__inspectraBootstrap) {
    return win.__inspectraBootstrap;
  }

  const element = documentRef.getElementById("inspectra-bootstrap");
  const payload = element?.textContent?.trim();

  if (!payload || payload === "__INSPECTRA_BOOTSTRAP__") {
    return null;
  }

  // If it starts with '{', it's plain JSON (dev / legacy). Otherwise it's gzip+base64.
  if (payload.startsWith("{")) {
    try {
      return JSON.parse(payload) as InSpectraBootstrap;
    } catch (error) {
      throw new Error(`Injected viewer bootstrap is not valid JSON: ${toMessage(error)}`);
    }
  }

  try {
    const binary = Uint8Array.from(atob(payload), (c) => c.charCodeAt(0));
    const stream = new Blob([binary]).stream().pipeThrough(new DecompressionStream("gzip"));
    const text = await new Response(stream).text();
    return JSON.parse(text) as InSpectraBootstrap;
  } catch (error) {
    throw new Error(`Failed to decompress bootstrap: ${toMessage(error)}`);
  }
}

export async function resolveStartupRequest(params: {
  documentRef?: Document;
  search: string;
  href: string;
}): Promise<StartupRequest> {
  const bootstrap = await readInjectedBootstrap(params.documentRef);
  const queryLinks = resolveViewerLinksFromSearch(params.search, params.href);

  if (bootstrap?.mode === "inline") {
    const features = readFeatures(bootstrap.features, true);
    if (features.urlLoading && queryLinks) {
      return {
        kind: "links",
        links: queryLinks,
        options: readOptions(bootstrap.options),
        features,
        source: "query",
      };
    }

    return {
      kind: "inline",
      openCli: bootstrap.openCli,
      xmlDoc: bootstrap.xmlDoc,
      options: readOptions(bootstrap.options),
      features,
    };
  }

  if (bootstrap?.mode === "links") {
    const features = readFeatures(bootstrap.features, true);
    if (features.urlLoading && queryLinks) {
      return {
        kind: "links",
        links: queryLinks,
        options: readOptions(bootstrap.options),
        features,
        source: "query",
      };
    }

    const links = resolveViewerLinks(bootstrap, params.href);
    if (!links) {
      throw new Error("Injected links bootstrap must provide openCliUrl or directoryUrl.");
    }

    return {
      kind: "links",
      links,
      options: readOptions(bootstrap.options),
      features,
      source: "bootstrap",
    };
  }

  const features = readFeatures(undefined, false);

  if (features.urlLoading && queryLinks) {
    return {
      kind: "links",
      links: queryLinks,
      options: defaultViewerOptions(),
      features,
      source: "query",
    };
  }

  return {
    kind: "empty",
    features,
  };
}

function readOptions(options: Partial<ViewerOptions> | ViewerOptions | undefined): ViewerOptions {
  const defaults = defaultViewerOptions();
  return {
    includeHidden: options?.includeHidden ?? defaults.includeHidden,
    includeMetadata: options?.includeMetadata ?? defaults.includeMetadata,
    label: options?.label,
    title: options?.title,
    commandPrefix: options?.commandPrefix,
    theme: options?.theme,
    colorTheme: options?.colorTheme,
    customAccent: options?.customAccent,
    customAccentDark: options?.customAccentDark,
  };
}

function readFeatures(features: Partial<FeatureFlags> | undefined, hasBootstrap: boolean): FeatureFlags {
  if (!hasBootstrap) {
    return defaultFeatureFlags();
  }

  const defaults = features ? defaultFeatureFlags() : disabledFeatureFlags();
  return {
    showHome: features?.showHome ?? defaults.showHome,
    composer: features?.composer ?? defaults.composer,
    darkTheme: features?.darkTheme ?? defaults.darkTheme,
    lightTheme: features?.lightTheme ?? defaults.lightTheme,
    urlLoading: features?.urlLoading ?? defaults.urlLoading,
    nugetBrowser: features?.nugetBrowser ?? defaults.nugetBrowser,
    packageUpload: features?.packageUpload ?? defaults.packageUpload,
    colorThemePicker: features?.colorThemePicker ?? defaults.colorThemePicker,
  };
}

