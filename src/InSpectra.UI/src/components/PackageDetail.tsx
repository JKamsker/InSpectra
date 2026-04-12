import { AlertTriangle, ArrowLeft, CheckCircle2, ExternalLink, LoaderCircle, Package } from "lucide-react";
import { ReactNode, SyntheticEvent, useCallback, useEffect, useRef, useState } from "react";
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
  loadError?: string | null;
  onBack: () => void;
  onLoadPackage: (
    opencliUrl: string,
    xmldocUrl: string | undefined,
    label: string,
    packageId: string,
    version: string | undefined,
    command: string | undefined,
  ) => Promise<void>;
}

export function PackageDetail({ pkg, summary, selectedVersion, loadError, onBack, onLoadPackage }: PackageDetailProps) {
  const [loadingVersion, setLoadingVersion] = useState<string | null>(null);
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
    const command = pkg.versions.find((candidate) => candidate.version === resolvedVersion)?.command ?? versionInfo.command;
    setLoadingVersion(resolvedVersion);
    void onLoadPackage(urls.opencliUrl, urls.xmldocUrl, label, pkg.packageId, isLatest ? undefined : resolvedVersion, command)
      .finally(() => setLoadingVersion(null));
  }

  return (
    <main className="ds-content-screen">
      <section className="ds-hero-panel panel">
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
          <button type="button" className="secondary-button" onClick={onBack}>
            <ArrowLeft aria-hidden="true" size={14} />
            All packages
          </button>
        </div>
        {loadError && (
          <p className="ds-inline-alert" role="alert">{loadError}</p>
        )}

        <div className="browse-detail-meta">
          {versionInfo.command && (
            <DetailField label="Command">
              <code>{versionInfo.command}</code>
            </DetailField>
          )}
          <DetailField label="Latest version">{pkg.latestVersion}</DetailField>
          {summary?.cliFramework && (
            <DetailField label="CLI Framework">{summary.cliFramework}</DetailField>
          )}
          <div className="browse-detail-field">
            <span className="browse-detail-label">Status</span>
            <StatusBadge status={pkg.latestStatus} />
          </div>
          <DetailField label="Downloads">{formatNumber(pkg.totalDownloads)}</DetailField>
          {summary && (
            <DetailField label="Created">{formatDate(summary.createdAt)}</DetailField>
          )}
          {summary && (
            <DetailField label="Updated">{formatDate(summary.updatedAt)}</DetailField>
          )}
          {summary && (
            <DetailField label="Coverage">
              {pkg.latestStatus === "ok"
                ? `${formatNumber(summary.commandCount)} commands across ${formatNumber(summary.commandGroupCount)} groups`
                : "Unavailable for partial analysis"}
            </DetailField>
          )}
          <div className="browse-detail-field">
            <span className="browse-detail-label">NuGet</span>
            <a
              href={nugetUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="browse-nuget-link"
              title={nugetUrl}
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
                title={projectUrl}
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
                title={sourceUrl}
              >
                Open source repo <ExternalLink aria-hidden="true" size={12} />
              </a>
            </div>
          )}
        </div>
      </section>

      <section className="ver-timeline-section panel section-card">
        <div className="section-heading">
          <Package aria-hidden="true" />
          <h2>Version history</h2>
        </div>

        <div className="ver-timeline">
          {pkg.versions.map((ver, index) => {
            const isLatest = ver.version === pkg.latestVersion;
            const isActive = ver.version === activeVersion;
            const isLast = index === pkg.versions.length - 1;
            return (
              <div
                key={ver.version}
                className={`ver-row${isActive ? " ver-row--active" : ""}`}
                style={{ animationDelay: `${0.08 * index}s` }}
              >
                <div className="ver-rail">
                  <span className={`ver-node${isLatest ? " ver-node--latest" : ""}`} />
                  {!isLast && <span className="ver-rail-line" />}
                </div>
                <div className="ver-body" role="button" tabIndex={0} onClick={() => !loadingVersion && handleLoad(ver.version)} onKeyDown={(e) => { if ((e.key === "Enter" || e.key === " ") && !loadingVersion) { e.preventDefault(); handleLoad(ver.version); } }}>
                  <div className="ver-content">
                    <div className="ver-meta">
                      <strong className="ver-number">v{ver.version}</strong>
                      <div className="ver-badges">
                        {isLatest && (
                          <span className="browse-badge browse-badge-latest">Latest</span>
                        )}
                        <StatusBadge status={ver.status} />
                        {ver.paths.opencliSource && (
                          <span className="browse-badge browse-badge-synth">Synthesized</span>
                        )}
                      </div>
                    </div>
                    <div className="ver-details">
                      <span className="ver-date">Published {formatDate(ver.publishedAt)}</span>
                      {ver.command && <code className="ver-command">{ver.command}</code>}
                    </div>
                  </div>
                  <button
                    type="button"
                    className="ver-inspect-btn"
                    disabled={!!loadingVersion}
                    onClick={(e) => { e.stopPropagation(); handleLoad(ver.version); }}
                  >
                    {loadingVersion === ver.version ? (
                      <><LoaderCircle className="spin" aria-hidden="true" size={13} /> Loading</>
                    ) : (
                      "Inspect"
                    )}
                  </button>
                </div>
              </div>
            );
          })}
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

function DetailField({ label, children }: { label: string; children: ReactNode }) {
  const valueRef = useRef<HTMLSpanElement>(null);
  const [truncated, setTruncated] = useState(false);

  const check = useCallback(() => {
    const el = valueRef.current;
    if (el) setTruncated(el.scrollWidth > el.clientWidth);
  }, []);

  useEffect(() => {
    check();
    const observer = new ResizeObserver(check);
    if (valueRef.current) observer.observe(valueRef.current);
    return () => observer.disconnect();
  }, [check]);

  const text = truncated ? (valueRef.current?.textContent ?? undefined) : undefined;

  return (
    <div className={`browse-detail-field${truncated ? " has-tooltip" : ""}`}>
      <span className="browse-detail-label">{label}</span>
      <span ref={valueRef} className="browse-detail-value" data-tooltip={truncated ? text : undefined}>
        {children}
      </span>
    </div>
  );
}
