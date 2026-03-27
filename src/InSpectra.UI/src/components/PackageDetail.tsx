import { AlertTriangle, ArrowLeft, CheckCircle2, ExternalLink, LoaderCircle, Package } from "lucide-react";
import { SyntheticEvent, useState } from "react";
import {
  DEFAULT_PACKAGE_ICON_URL,
  DiscoveryPackageDetail,
  DiscoveryPackageSummary,
  resolvePackageUrls,
} from "../data/nugetDiscovery";

interface PackageDetailProps {
  pkg: DiscoveryPackageDetail;
  summary?: DiscoveryPackageSummary;
  selectedVersion?: string;
  onLoadPackage: (opencliUrl: string, xmldocUrl: string, label: string, packageId: string, version: string | undefined) => void;
}

export function PackageDetail({ pkg, summary, selectedVersion, onLoadPackage }: PackageDetailProps) {
  const [loadingSpec, setLoadingSpec] = useState(false);
  const activeVersion = selectedVersion || pkg.latestVersion;
  const versionInfo = pkg.versions.find((v) => v.version === activeVersion) || pkg.versions[0];
  const iconUrl = summary?.packageIconUrl || DEFAULT_PACKAGE_ICON_URL;
  const nugetUrl = pkg.links?.nuget || `https://www.nuget.org/packages/${pkg.packageId}`;
  const projectUrl = pkg.links?.project;
  const sourceUrl = pkg.links?.source;
  const showProjectLink = !!projectUrl;
  const showSourceLink = !!sourceUrl && sourceUrl !== projectUrl;

  function handleLoad(ver?: string) {
    const resolvedVersion = ver || pkg.latestVersion;
    const isLatest = resolvedVersion === pkg.latestVersion;
    const urls = resolvePackageUrls(pkg, resolvedVersion);
    const label = `${pkg.packageId} v${resolvedVersion}`;
    setLoadingSpec(true);
    onLoadPackage(urls.opencliUrl, urls.xmldocUrl, label, pkg.packageId, isLatest ? undefined : resolvedVersion);
  }

  return (
    <main className="import-screen">
      <section className="import-hero panel">
        <div className="browse-header-row">
          <div className="browse-detail-title-row">
            <img
              className="browse-package-icon browse-package-icon-lg"
              src={iconUrl}
              alt=""
              loading="lazy"
              onError={handlePackageIconError}
            />
            <div>
              <div className="eyebrow">NuGet Browser</div>
              <h1>{pkg.packageId}</h1>
            </div>
          </div>
          <button type="button" className="secondary-button" onClick={() => history.back()}>
            <ArrowLeft aria-hidden="true" size={14} />
            All packages
          </button>
        </div>

        <div className="browse-detail-meta">
          {versionInfo.command && (
            <div className="browse-detail-field">
              <span className="browse-detail-label">Command</span>
              <code>{versionInfo.command}</code>
            </div>
          )}
          <div className="browse-detail-field">
            <span className="browse-detail-label">Latest version</span>
            <span>{pkg.latestVersion}</span>
          </div>
          <div className="browse-detail-field">
            <span className="browse-detail-label">Status</span>
            <StatusBadge status={pkg.latestStatus} />
          </div>
          <div className="browse-detail-field">
            <span className="browse-detail-label">Downloads</span>
            <span>{formatNumber(pkg.totalDownloads)}</span>
          </div>
          {summary && (
            <div className="browse-detail-field">
              <span className="browse-detail-label">Created</span>
              <span>{formatDate(summary.createdAt)}</span>
            </div>
          )}
          {summary && (
            <div className="browse-detail-field">
              <span className="browse-detail-label">Updated</span>
              <span>{formatDate(summary.updatedAt)}</span>
            </div>
          )}
          {summary && (
            <div className="browse-detail-field">
              <span className="browse-detail-label">Coverage</span>
              <span>{summary.commandCount} commands across {summary.commandGroupCount} groups</span>
            </div>
          )}
          <div className="browse-detail-field">
            <span className="browse-detail-label">NuGet</span>
            <a
              href={nugetUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="browse-nuget-link"
            >
              View on nuget.org <ExternalLink aria-hidden="true" size={12} />
            </a>
          </div>
          {showProjectLink && (
            <div className="browse-detail-field">
              <span className="browse-detail-label">Website</span>
              <a
                href={projectUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="browse-nuget-link"
              >
                Open project site <ExternalLink aria-hidden="true" size={12} />
              </a>
            </div>
          )}
          {showSourceLink && (
            <div className="browse-detail-field">
              <span className="browse-detail-label">Repository</span>
              <a
                href={sourceUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="browse-nuget-link"
              >
                Open source repo <ExternalLink aria-hidden="true" size={12} />
              </a>
            </div>
          )}
        </div>
      </section>

      <section className="browse-versions panel section-card">
        <div className="section-heading">
          <Package aria-hidden="true" />
          <h2>Versions</h2>
        </div>

        <div className="browse-version-list">
          {pkg.versions.map((ver) => (
            <div key={ver.version} className={`browse-version-row ${ver.version === activeVersion ? "active" : ""}`}>
              <div className="browse-version-info">
                <strong>v{ver.version}</strong>
                {ver.version === pkg.latestVersion && (
                  <span className="browse-badge browse-badge-latest">Latest</span>
                )}
                <StatusBadge status={ver.status} />
                {ver.paths.opencliSource && (
                  <span className="browse-badge browse-badge-synth">Synthesized</span>
                )}
              </div>
              <div className="browse-version-dates">
                <span>Published {formatDate(ver.publishedAt)}</span>
              </div>
              <button
                type="button"
                className="secondary-button"
                disabled={loadingSpec}
                onClick={() => handleLoad(ver.version)}
              >
                {loadingSpec ? (
                  <><LoaderCircle className="spin" aria-hidden="true" size={14} /> Loading...</>
                ) : (
                  "Inspect"
                )}
              </button>
            </div>
          ))}
        </div>
      </section>
    </main>
  );
}

function handlePackageIconError(event: SyntheticEvent<HTMLImageElement>) {
  const img = event.currentTarget;
  if (img.src === DEFAULT_PACKAGE_ICON_URL) return;
  img.src = DEFAULT_PACKAGE_ICON_URL;
}

export function StatusBadge({ status }: { status: "ok" | "partial" }) {
  if (status === "ok") {
    return (
      <span className="browse-badge browse-badge-ok">
        <CheckCircle2 aria-hidden="true" size={12} />
        Full
      </span>
    );
  }
  return (
    <span className="browse-badge browse-badge-partial">
      <AlertTriangle aria-hidden="true" size={12} />
      Partial
    </span>
  );
}

function formatDate(iso: string): string {
  try {
    return new Date(iso).toLocaleDateString(undefined, { year: "numeric", month: "short", day: "numeric" });
  } catch {
    return iso;
  }
}

function formatNumber(value: number): string {
  return new Intl.NumberFormat().format(value);
}
