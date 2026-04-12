import { Home, Package, Upload } from "lucide-react";
import { PropsWithChildren } from "react";
import { FeatureFlags } from "../boot/contracts";
import { ViewerToolbarRoutes } from "../staticViewerSupport";
import { ThemeToggle } from "./ThemeToggle";

interface StaticViewerShellProps extends PropsWithChildren {
  featureFlags: FeatureFlags;
  routes: ViewerToolbarRoutes;
}

export function StaticViewerShell({ children, featureFlags, routes }: StaticViewerShellProps) {
  const brandHref = routes.homeHref ?? routes.browseHref;
  const brand = (
    <>
      <span className="brand-mark">{">_"}</span>
      <div className="brand-info">
        <div className="brand-title">InSpectraUI</div>
        <div className="brand-subtitle">
          <span>Static routes</span>
        </div>
      </div>
    </>
  );

  return (
    <div className="app-shell">
      <header className="topbar">
        {brandHref ? (
          <a href={brandHref} className="brand-block brand-link">
            {brand}
          </a>
        ) : (
          <div className="brand-block">{brand}</div>
        )}

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

            {featureFlags.darkTheme && featureFlags.lightTheme && (
              <ThemeToggle colorThemePicker={featureFlags.colorThemePicker} />
            )}
          </div>
        </div>
      </header>

      {children}
    </div>
  );
}
