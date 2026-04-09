import { OpenCliDocument } from "../data/openCli";

export interface ViewerOptions {
  includeHidden: boolean;
  includeMetadata: boolean;
  label?: string;
  title?: string;
  commandPrefix?: string;
  /** Preset light/dark mode (overrides localStorage on load). */
  theme?: "light" | "dark";
  /** Preset color theme ID (overrides localStorage on load). */
  colorTheme?: string;
  /** Custom accent color for light mode (hex, e.g. "#7c3aed"). */
  customAccent?: string;
  /** Custom accent color for dark mode (hex). Falls back to customAccent if omitted. */
  customAccentDark?: string;
}

export interface FeatureFlags {
  showHome: boolean;
  composer: boolean;
  darkTheme: boolean;
  lightTheme: boolean;
  urlLoading: boolean;
  nugetBrowser: boolean;
  packageUpload: boolean;
  /** Whether the user can open the color theme picker (long-press). Default true. */
  colorThemePicker: boolean;
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
    colorThemePicker: true,
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
    colorThemePicker: true,
  };
}
