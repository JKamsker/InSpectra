import { ArrowLeft, LoaderCircle, Search } from "lucide-react";
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
  onLoadPackage: (opencliUrl: string, xmldocUrl: string, label: string, packageId: string, version: string | undefined) => void;
  onBack: () => void;
}

export function NugetBrowser({ packageId, version, onLoadPackage, onBack }: NugetBrowserProps) {
  const searchInputRef = useRef<HTMLInputElement>(null);
  const [index, setIndex] = useState<DiscoverySummaryIndex | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [packageLoading, setPackageLoading] = useState(false);
  const [packageError, setPackageError] = useState<string | null>(null);
  const [packageDetail, setPackageDetail] = useState<DiscoveryPackageDetail | null>(null);
  const [searchTerm, setSearchTerm] = useState("");
  const deferredSearch = useDeferredValue(searchTerm);
  const [paletteOpen, setPaletteOpen] = useState(false);

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

  const results = searchPackages(index, deferredSearch);

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

          <div className="browse-stats">
            <span className="browse-stat">
              {results.length === index.packages.length
                ? `${index.packageCount} packages`
                : `${results.length} of ${index.packageCount} packages`}
            </span>
          </div>
        </section>

        <div className="browse-grid">
          {results.map((pkg) => (
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
        <img
          className="browse-package-icon"
          src={iconUrl}
          alt=""
          loading="lazy"
          onError={handlePackageIconError}
        />
        <div className="browse-card-title">{pkg.packageId}</div>
        <StatusBadge status={getPackageStatus(pkg)} />
      </div>
      <div className="browse-card-meta">
        {pkg.commandName && (
          <span className="browse-card-command"><code>{pkg.commandName}</code></span>
        )}
        <span className="browse-card-version">v{pkg.latestVersion}</span>
        {pkg.versionCount > 1 && (
          <span className="browse-card-versions">{pkg.versionCount} versions</span>
        )}
      </div>
      <div className="browse-card-stats">
        <span>{pkg.commandCount} commands</span>
        <span>{pkg.commandGroupCount} groups</span>
      </div>
    </a>
  );
}

function handlePackageIconError(event: SyntheticEvent<HTMLImageElement>) {
  const img = event.currentTarget;
  if (img.src === DEFAULT_PACKAGE_ICON_URL) return;
  img.src = DEFAULT_PACKAGE_ICON_URL;
}
