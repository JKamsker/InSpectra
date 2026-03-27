import { OpenCliDocument } from "../data/openCli";

export interface ViewerOptions {
  includeHidden: boolean;
  includeMetadata: boolean;
  label?: string;
}

export interface FeatureFlags {
  showHome: boolean;
  composer: boolean;
  darkTheme: boolean;
  lightTheme: boolean;
  urlLoading: boolean;
  nugetBrowser: boolean;
  packageUpload: boolean;
}

export type InSpectraBootstrap =
  | {
      mode: "inline";
      openCli: OpenCliDocument;
      xmlDoc?: string;
      options: ViewerOptions;
      features?: Partial<FeatureFlags>;
    }
  | {
      mode: "links";
      openCliUrl?: string;
      xmlDocUrl?: string;
      directoryUrl?: string;
      options?: Partial<ViewerOptions>;
      features?: Partial<FeatureFlags>;
    };

export function defaultViewerOptions(): ViewerOptions {
  return {
    includeHidden: false,
    includeMetadata: false,
  };
}

export function defaultFeatureFlags(): FeatureFlags {
  return {
    showHome: true,
    composer: true,
    darkTheme: true,
    lightTheme: true,
    urlLoading: true,
    nugetBrowser: true,
    packageUpload: true,
  };
}

export function disabledFeatureFlags(): FeatureFlags {
  return {
    showHome: false,
    composer: false,
    darkTheme: true,
    lightTheme: true,
    urlLoading: false,
    nugetBrowser: false,
    packageUpload: false,
  };
}
