import { ArrowLeft, LoaderCircle, Package, Search } from "lucide-react";
import { useEffect, useDeferredValue, useRef, useState } from "react";
import {
  DiscoveryIndex,
  DiscoveryPackage,
  fetchDiscoveryIndex,
  findPackageById,
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
  const [index, setIndex] = useState<DiscoveryIndex | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState("");
  const deferredSearch = useDeferredValue(searchTerm);
  const [paletteOpen, setPaletteOpen] = useState(false);

  useEffect(() => {
    const controller = new AbortController();
    setLoading(true);
    setError(null);
    fetchDiscoveryIndex(controller.signal)
      .then((data) => { setIndex(data); setLoading(false); })
      .catch((err) => {
        if (err instanceof DOMException && err.name === "AbortError") return;
        setError(err instanceof Error ? err.message : "Failed to load index.");
        setLoading(false);
      });
    return () => controller.abort();
  }, []);

  useEffect(() => {
    function handleKeyDown(e: KeyboardEvent) {
      const mod = e.ctrlKey || e.metaKey;
      if (mod && e.key === "f") {
        e.preventDefault();
        if (searchInputRef.current) {
          searchInputRef.current.focus({ preventScroll: true });
          window.scrollTo({ top: 0, behavior: "smooth" });
        } else {
          setPaletteOpen((o) => !o);
        }
      }
    }
    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, []);

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

  const browsePalette = (
    <BrowsePalette
      packages={index.packages}
      open={paletteOpen}
      onClose={() => setPaletteOpen(false)}
      onSelect={(pkgId) => { window.location.hash = buildBrowseHash(pkgId); }}
    />
  );

  if (packageId) {
    const pkg = findPackageById(index, packageId);
    if (!pkg) {
      return (
        <>
          <main className="import-screen">
            <section className="import-hero panel">
              <div className="eyebrow">NuGet Browser</div>
              <h1>Package not found</h1>
              <p className="lede">
                No package matching <code>{packageId}</code> was found in the index.
              </p>
              <button type="button" className="secondary-button" onClick={() => history.back()} style={{ marginTop: "1rem" }}>
                Back to browser
              </button>
            </section>
          </main>
          {browsePalette}
        </>
      );
    }
    return (
      <>
        <PackageDetail pkg={pkg} selectedVersion={version} onLoadPackage={onLoadPackage} />
        {browsePalette}
      </>
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

function PackageCard({ pkg }: { pkg: DiscoveryPackage }) {
  const latestVer = pkg.versions[0];
  return (
    <a className="browse-card panel" href={buildBrowseHash(pkg.packageId)}>
      <div className="browse-card-header">
        <Package aria-hidden="true" className="browse-card-icon" />
        <div className="browse-card-title">{pkg.packageId}</div>
        <StatusBadge status={pkg.latestStatus} />
      </div>
      <div className="browse-card-meta">
        {latestVer?.command && (
          <span className="browse-card-command"><code>{latestVer.command}</code></span>
        )}
        <span className="browse-card-version">v{pkg.latestVersion}</span>
        {pkg.versions.length > 1 && (
          <span className="browse-card-versions">{pkg.versions.length} versions</span>
        )}
      </div>
    </a>
  );
}
