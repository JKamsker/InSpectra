import { ArrowDownToLine, Clock3, Layers3, Terminal } from "lucide-react";
import { DEFAULT_PACKAGE_ICON_URL, getPackageStatus, type DiscoveryPackageSummary } from "../data/nugetDiscovery";
import { buildBrowseHash } from "../data/navigation";
import { StatusBadge } from "./PackageDetail";
import {
  buildUpdatedTooltip,
  formatCount,
  formatNumber,
  formatRelativeAgeLong,
  formatRelativeAgeShort,
  handlePackageIconError,
} from "./NugetBrowserSupport";

export function PackageCard({ pkg }: { pkg: DiscoveryPackageSummary }) {
  const iconUrl = pkg.packageIconUrl || DEFAULT_PACKAGE_ICON_URL;

  return (
    <a className="browse-card panel" href={buildBrowseHash(pkg.packageId)}>
      <div className="browse-card-header">
        <div className="browse-card-title-group">
          <img
            className="browse-package-icon"
            src={iconUrl}
            alt=""
            loading="lazy"
            onError={handlePackageIconError}
          />
          <div className="browse-card-title">{pkg.packageId}</div>
        </div>
        <StatusBadge status={getPackageStatus(pkg)} />
      </div>

      <div className="browse-card-body">
        {pkg.commandName && (
          <div className="browse-card-command" aria-label={`Command alias ${pkg.commandName}`}>
            <span className="browse-card-command-prefix">&gt;</span>
            <code>{pkg.commandName}</code>
          </div>
        )}
      </div>

      <div className="browse-card-footer">
        <div className="browse-card-meta">
          <span className="browse-card-version">v{pkg.latestVersion}</span>
          {pkg.versionCount > 1 && (
            <>
              <span className="browse-card-meta-separator" aria-hidden="true">•</span>
              <span className="browse-card-versions">{pkg.versionCount} versions</span>
            </>
          )}
        </div>

        <div className="browse-card-stats">
          <span
            className="browse-card-stat"
            aria-label={`Last updated ${formatRelativeAgeLong(pkg.updatedAt)} ago`}
            data-tooltip={buildUpdatedTooltip(pkg.updatedAt)}
            title={buildUpdatedTooltip(pkg.updatedAt)}
          >
            <Clock3 aria-hidden="true" size={13} />
            <span>{formatRelativeAgeShort(pkg.updatedAt)}</span>
          </span>
          <span
            className="browse-card-stat"
            aria-label={`${pkg.totalDownloads} total downloads`}
            data-tooltip={`Downloads: ${formatNumber(pkg.totalDownloads)}`}
            title={`Downloads: ${formatNumber(pkg.totalDownloads)}`}
          >
            <ArrowDownToLine aria-hidden="true" size={13} />
            <span>{formatCount(pkg.totalDownloads)}</span>
          </span>
          <span
            className="browse-card-stat"
            aria-label={`${pkg.commandCount} commands`}
            data-tooltip={`Commands: ${formatNumber(pkg.commandCount)}`}
            title={`Commands: ${formatNumber(pkg.commandCount)}`}
          >
            <Terminal aria-hidden="true" size={13} />
            <span>{pkg.commandCount}</span>
          </span>
          <span
            className="browse-card-stat"
            aria-label={`${pkg.commandGroupCount} command groups`}
            data-tooltip={`Groups: ${formatNumber(pkg.commandGroupCount)}`}
            title={`Groups: ${formatNumber(pkg.commandGroupCount)}`}
          >
            <Layers3 aria-hidden="true" size={13} />
            <span>{pkg.commandGroupCount}</span>
          </span>
        </div>
      </div>
    </a>
  );
}
