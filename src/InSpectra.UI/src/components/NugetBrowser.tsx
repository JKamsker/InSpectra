import { ArrowDownToLine, ArrowLeft, Clock3, Layers3, LoaderCircle, Search, Terminal } from "lucide-react";
import { SyntheticEvent, useDeferredValue, useEffect, useRef, useState } from "react";
import {
  DEFAULT_PACKAGE_ICON_URL,
  DiscoveryPackageDetail,
  DiscoveryPackageSummary,
  DiscoverySummaryIndex,
  fetchDiscoveryIndex,
  fetchDiscoveryPackage,
  findPackageSummaryById,
  getPackageStatus,
  searchPackages,
} from "../data/nugetDiscovery";
import { buildBrowseHash } from "../data/navigation";
import { BrowsePalette } from "./BrowsePalette";
import { PackageDetail, StatusBadge } from "./PackageDetail";

interface NugetBrowserProps {
  packageId?: string;
  version?: string;
  onLoadPackage: (
    opencliUrl: string,
    xmldocUrl: string,
    label: string,
    packageId: string,
    version: string | undefined,
    command: string | undefined,
  ) => void;
  onBack: () => void;
}

type BrowseOrder =
  | "index"
  | "updated"
  | "created"
  | "downloads"
  | "name"
  | "commands"
  | "groups"
  | "versions";

export function NugetBrowser({ packageId, version, onLoadPackage, onBack }: NugetBrowserProps) {
  const searchInputRef = useRef<HTMLInputElement>(null);
  const [index, setIndex] = useState<DiscoverySummaryIndex | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [packageLoading, setPackageLoading] = useState(false);
  const [packageError, setPackageError] = useState<string | null>(null);
  const [packageDetail, setPackageDetail] = useState<DiscoveryPackageDetail | null>(null);
  const [searchTerm, setSearchTerm] = useState(() => sessionStorage.getItem("browse:search") ?? "");
  const deferredSearch = useDeferredValue(searchTerm);
  const [orderBy, setOrderBy] = useState<BrowseOrder>(() => (sessionStorage.getItem("browse:order") as BrowseOrder) || "index");
  const [paletteOpen, setPaletteOpen] = useState(false);

  useEffect(() => { sessionStorage.setItem("browse:search", searchTerm); }, [searchTerm]);
  useEffect(() => { sessionStorage.setItem("browse:order", orderBy); }, [orderBy]);

  useEffect(() => {
    const controller = new AbortController();
    setLoading(true);
    setError(null);
    fetchDiscoveryIndex(controller.signal)
      .then((data) => {
        setIndex(data);
        setLoading(false);
      })
      .catch((err) => {
        if (err instanceof DOMException && err.name === "AbortError") return;
        setError(err instanceof Error ? err.message : "Failed to load index.");
        setLoading(false);
      });
    return () => controller.abort();
  }, []);

  useEffect(() => {
    if (!packageId) {
      setPackageDetail(null);
      setPackageError(null);
      setPackageLoading(false);
      return;
    }

    const controller = new AbortController();
    setPackageDetail(null);
    setPackageError(null);
    setPackageLoading(true);
    fetchDiscoveryPackage(packageId, controller.signal)
      .then((data) => {
        setPackageDetail(data);
        setPackageLoading(false);
      })
      .catch((err) => {
        if (err instanceof DOMException && err.name === "AbortError") return;
        setPackageError(err instanceof Error ? err.message : "Failed to load package.");
        setPackageLoading(false);
      });
    return () => controller.abort();
  }, [packageId]);

  useEffect(() => {
    function handleKeyDown(e: KeyboardEvent) {
      const mod = e.ctrlKey || e.metaKey;
      if (mod && e.key === "f") {
        e.preventDefault();
        if (searchInputRef.current) {
          searchInputRef.current.focus({ preventScroll: true });
          window.scrollTo({ top: 0, behavior: "smooth" });
        } else {
          setPaletteOpen((open) => !open);
        }
      }
    }
    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, []);

  const browsePalette = index ? (
    <BrowsePalette
      packages={index.packages}
      open={paletteOpen}
      onClose={() => setPaletteOpen(false)}
      onSelect={(pkgId) => {
        window.location.hash = buildBrowseHash(pkgId);
      }}
    />
  ) : null;

  if (packageId) {
    const packageSummary = index ? findPackageSummaryById(index, packageId) : undefined;

    if (packageError) {
      return (
        <>
          <main className="import-screen">
            <section className="import-hero panel">
              <div className="eyebrow">NuGet Browser</div>
              <h1>Package not found</h1>
              <p className="inline-alert" role="alert">{packageError}</p>
              <button type="button" className="secondary-button" onClick={() => history.back()} style={{ marginTop: "1rem" }}>
                Back to browser
              </button>
            </section>
          </main>
          {browsePalette}
        </>
      );
    }

    if (packageLoading || !packageDetail) {
      return (
        <>
          <main className="import-screen">
            <section className="import-hero panel">
              <div className="browse-loading">
                <LoaderCircle className="spin" aria-hidden="true" />
                <span>Loading package details...</span>
              </div>
            </section>
          </main>
          {browsePalette}
        </>
      );
    }

    return (
      <>
        <PackageDetail
          pkg={packageDetail}
          summary={packageSummary}
          selectedVersion={version}
          onLoadPackage={onLoadPackage}
        />
        {browsePalette}
      </>
    );
  }

  if (loading) {
    return (
      <main className="import-screen">
        <section className="import-hero panel">
          <div className="browse-loading">
            <LoaderCircle className="spin" aria-hidden="true" />
            <span>Loading package index...</span>
          </div>
        </section>
      </main>
    );
  }

  if (error || !index) {
    return (
      <main className="import-screen">
        <section className="import-hero panel">
          <div className="eyebrow">NuGet Browser</div>
          <h1>Failed to load package index</h1>
          <p className="inline-alert" role="alert">{error}</p>
          <button type="button" className="secondary-button" onClick={onBack} style={{ marginTop: "1rem" }}>
            Go back
          </button>
        </section>
      </main>
    );
  }

  const results = sortPackages(searchPackages(index, deferredSearch), orderBy);
  const DISPLAY_LIMIT = 200;
  const displayedResults = results.slice(0, DISPLAY_LIMIT);
  const hasMore = results.length > DISPLAY_LIMIT;

  return (
    <>
      <main className="import-screen">
        <section className="import-hero panel">
          <div className="browse-header-row">
            <div>
              <div className="eyebrow">NuGet Browser</div>
              <h1>Explore .NET CLI tools</h1>
              <p className="lede">
                Browse {index.packageCount} indexed .NET tool packages. Select one to inspect its command structure.
              </p>
            </div>
            <button type="button" className="secondary-button" onClick={onBack}>
              <ArrowLeft aria-hidden="true" size={14} />
              Back
            </button>
          </div>

          <div className="browse-search">
            <Search aria-hidden="true" className="browse-search-icon" />
            <input
              ref={searchInputRef}
              type="search"
              placeholder="Search packages by name or command..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              autoFocus
            />
            <kbd className="kbd-hint browse-kbd">Ctrl F</kbd>
          </div>

          <div className="browse-toolbar">
            <span className="browse-stat">
              {results.length === index.packages.length
                ? `${index.packageCount} packages`
                : `${results.length} of ${index.packageCount} packages`}
              {hasMore && ` (showing first ${DISPLAY_LIMIT})`}
            </span>

            <label className="browse-order-control">
              <span className="browse-order-label">Order by</span>
              <select value={orderBy} onChange={(e) => setOrderBy(e.target.value as BrowseOrder)}>
                <option value="index">Suggested</option>
                <option value="updated">Recently updated</option>
                <option value="created">Recently created</option>
                <option value="downloads">Downloads</option>
                <option value="name">Name</option>
                <option value="commands">Commands</option>
                <option value="groups">Groups</option>
                <option value="versions">Versions</option>
              </select>
            </label>
          </div>
        </section>

        <div className="browse-grid">
          {displayedResults.map((pkg) => (
            <PackageCard key={pkg.packageId} pkg={pkg} />
          ))}
          {results.length === 0 && (
            <div className="browse-empty panel">
              <p>No packages match <strong>{deferredSearch}</strong></p>
            </div>
          )}
        </div>
      </main>
      {browsePalette}
    </>
  );
}

function PackageCard({ pkg }: { pkg: DiscoveryPackageSummary }) {
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

function handlePackageIconError(event: SyntheticEvent<HTMLImageElement>) {
  const img = event.currentTarget;
  if (img.src === DEFAULT_PACKAGE_ICON_URL) return;
  img.src = DEFAULT_PACKAGE_ICON_URL;
}

function formatCount(value: number): string {
  return new Intl.NumberFormat(undefined, { notation: "compact", maximumFractionDigits: 1 }).format(value);
}

function formatNumber(value: number): string {
  return new Intl.NumberFormat().format(value);
}

function formatRelativeAgeShort(iso: string): string {
  const diffMs = Date.now() - Date.parse(iso);
  if (!Number.isFinite(diffMs) || diffMs < 0) return "?";

  const hourMs = 60 * 60 * 1000;
  const dayMs = 24 * hourMs;
  const monthMs = 30 * dayMs;
  const yearMs = 365 * dayMs;

  if (diffMs >= yearMs) return `${Math.floor(diffMs / yearMs)}yr`;
  if (diffMs >= monthMs) return `${Math.floor(diffMs / monthMs)}mo`;
  if (diffMs >= dayMs) return `${Math.floor(diffMs / dayMs)}d`;
  if (diffMs >= hourMs) return `${Math.floor(diffMs / hourMs)}h`;
  return "<1h";
}

function formatRelativeAgeLong(iso: string): string {
  const diffMs = Date.now() - Date.parse(iso);
  if (!Number.isFinite(diffMs) || diffMs < 0) return "unknown";

  const hourMs = 60 * 60 * 1000;
  const dayMs = 24 * hourMs;
  const monthMs = 30 * dayMs;
  const yearMs = 365 * dayMs;

  if (diffMs >= yearMs) return `${Math.floor(diffMs / yearMs)} year${diffMs >= 2 * yearMs ? "s" : ""}`;
  if (diffMs >= monthMs) return `${Math.floor(diffMs / monthMs)} month${diffMs >= 2 * monthMs ? "s" : ""}`;
  if (diffMs >= dayMs) return `${Math.floor(diffMs / dayMs)} day${diffMs >= 2 * dayMs ? "s" : ""}`;
  if (diffMs >= hourMs) return `${Math.floor(diffMs / hourMs)} hour${diffMs >= 2 * hourMs ? "s" : ""}`;
  return "less than 1 hour";
}

function buildUpdatedTooltip(iso: string): string {
  return `Updated: ${formatRelativeAgeLong(iso)} ago (${formatAbsoluteDate(iso)})`;
}

function formatAbsoluteDate(iso: string): string {
  try {
    return new Date(iso).toLocaleString(undefined, {
      year: "numeric",
      month: "short",
      day: "numeric",
      hour: "numeric",
      minute: "2-digit",
    });
  } catch {
    return iso;
  }
}

function sortPackages(packages: DiscoveryPackageSummary[], orderBy: BrowseOrder): DiscoveryPackageSummary[] {
  const sorted = [...packages];

  switch (orderBy) {
    case "updated":
      return sorted.sort((left, right) =>
        compareIsoDatesDesc(left.updatedAt, right.updatedAt) || left.packageId.localeCompare(right.packageId),
      );
    case "created":
      return sorted.sort((left, right) =>
        compareIsoDatesDesc(left.createdAt, right.createdAt) || left.packageId.localeCompare(right.packageId),
      );
    case "downloads":
      return sorted.sort((left, right) =>
        right.totalDownloads - left.totalDownloads || left.packageId.localeCompare(right.packageId),
      );
    case "name":
      return sorted.sort((left, right) => left.packageId.localeCompare(right.packageId));
    case "commands":
      return sorted.sort((left, right) =>
        right.commandCount - left.commandCount || left.packageId.localeCompare(right.packageId),
      );
    case "groups":
      return sorted.sort((left, right) =>
        right.commandGroupCount - left.commandGroupCount || left.packageId.localeCompare(right.packageId),
      );
    case "versions":
      return sorted.sort((left, right) =>
        right.versionCount - left.versionCount || left.packageId.localeCompare(right.packageId),
      );
    case "index":
    default:
      return sorted;
  }
}

function compareIsoDatesDesc(left: string, right: string): number {
  return Date.parse(right) - Date.parse(left);
}
