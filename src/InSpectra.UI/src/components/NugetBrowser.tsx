import { LoaderCircle, Search } from "lucide-react";
import { useDeferredValue, useEffect, useMemo, useRef, useState } from "react";
import {
  DiscoveryPackageDetail,
  DiscoverySummaryIndex,
  fetchDiscoveryIndex,
  fetchDiscoveryIndexPreview,
  fetchDiscoveryPackage,
  findPackageSummaryById,
  searchPackages,
} from "../data/nugetDiscovery";
import { buildBrowseHash } from "../data/navigation";
import { BrowsePalette } from "./BrowsePalette";
import { PackageDetail } from "./PackageDetail";
import { PackageCard } from "./NugetPackageCard";
import { BrowseOrder, FULL_INDEX_HYDRATION_DELAY_MS, sortPackages, splitFrameworks } from "./NugetBrowserSupport";

interface NugetBrowserProps {
  packageId?: string;
  version?: string;
  inspectError?: string | null;
  onBackToBrowse: () => void;
  onLoadPackage: (
    opencliUrl: string,
    xmldocUrl: string | undefined,
    label: string,
    packageId: string,
    version: string | undefined,
    command: string | undefined,
  ) => Promise<void>;
  onBack: () => void;
}

export function NugetBrowser({ packageId, version, inspectError, onBackToBrowse, onLoadPackage, onBack }: NugetBrowserProps) {
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
  const [frameworkFilter, setFrameworkFilter] = useState(() => sessionStorage.getItem("browse:framework") ?? "");
  const [paletteOpen, setPaletteOpen] = useState(false);

  useEffect(() => { sessionStorage.setItem("browse:search", searchTerm); }, [searchTerm]);
  useEffect(() => { sessionStorage.setItem("browse:order", orderBy); }, [orderBy]);
  useEffect(() => { sessionStorage.setItem("browse:framework", frameworkFilter); }, [frameworkFilter]);

  const frameworkOptions = useMemo(() => {
    if (!index) return [];
    const counts = new Map<string, number>();
    for (const pkg of index.packages) {
      for (const fw of splitFrameworks(pkg.cliFramework)) {
        counts.set(fw, (counts.get(fw) ?? 0) + 1);
      }
    }
    return [...counts.entries()]
      .sort((a, b) => b[1] - a[1])
      .map(([name, count]) => ({ name, count }));
  }, [index]);

  useEffect(() => {
    const controller = new AbortController();
    let previewLoaded = false;
    let fullIndexTimer = 0;

    async function loadIndex() {
      setLoading(true);
      setError(null);

      try {
        const preview = await fetchDiscoveryIndexPreview(controller.signal);
        if (controller.signal.aborted) return;

        previewLoaded = true;
        setIndex(preview);
        setLoading(false);

        fullIndexTimer = window.setTimeout(() => {
          void fetchDiscoveryIndex()
            .then((fullIndex) => {
              if (!controller.signal.aborted) {
                setIndex(fullIndex);
              }
            })
            .catch((err) => {
              if (!controller.signal.aborted && !previewLoaded) {
                setError(err instanceof Error ? err.message : "Failed to load index.");
                setLoading(false);
              }
            });
        }, FULL_INDEX_HYDRATION_DELAY_MS);
      } catch (err) {
        if (err instanceof DOMException && err.name === "AbortError") return;
        setError(err instanceof Error ? err.message : "Failed to load index.");
        setLoading(false);
      }
    }

    void loadIndex();
    return () => {
      controller.abort();
      window.clearTimeout(fullIndexTimer);
    };
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
          <main className="ds-content-screen">
            <section className="ds-hero-panel panel">
              <div className="eyebrow">NuGet Browser</div>
              <h1>Package not found</h1>
              <p className="ds-inline-alert" role="alert">{packageError}</p>
              <button type="button" className="secondary-button" onClick={onBackToBrowse} style={{ marginTop: "1rem" }}>
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
          <main className="ds-content-screen">
            <section className="ds-hero-panel panel">
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
          loadError={inspectError}
          onBack={onBackToBrowse}
          onLoadPackage={onLoadPackage}
        />
        {browsePalette}
      </>
    );
  }

  if (loading) {
    return (
      <main className="ds-content-screen">
        <section className="ds-hero-panel panel">
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
      <main className="ds-content-screen">
        <section className="ds-hero-panel panel">
          <div className="eyebrow">NuGet Browser</div>
          <h1>Failed to load package index</h1>
          <p className="ds-inline-alert" role="alert">{error}</p>
          <button type="button" className="secondary-button" onClick={onBack} style={{ marginTop: "1rem" }}>
            Go back
          </button>
        </section>
      </main>
    );
  }

  const searched = searchPackages(index, deferredSearch);
  const filtered = frameworkFilter ? searched.filter((pkg) => splitFrameworks(pkg.cliFramework).includes(frameworkFilter)) : searched;
  const results = sortPackages(filtered, orderBy);
  const DISPLAY_LIMIT = 200;
  const displayedResults = results.slice(0, DISPLAY_LIMIT);
  const previewIsTruncated =
    typeof index.includedPackageCount === "number" &&
    index.packageCount > index.includedPackageCount &&
    results.length === index.packages.length;
  const hasMore = results.length > DISPLAY_LIMIT || previewIsTruncated;

  return (
    <>
      <main className="ds-content-screen">
        <section className="ds-hero-panel panel">
          <div className="browse-header-row">
            <div>
              <div className="eyebrow">NuGet Browser</div>
              <h1>Explore .NET CLI tools</h1>
              <p className="lede">
                Browse {index.packageCount} indexed .NET tool packages. Select one to inspect its command structure.
              </p>
            </div>
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
            <label className="browse-order-control">
              <span className="browse-order-label">Framework</span>
              <select value={frameworkFilter} onChange={(e) => setFrameworkFilter(e.target.value)}>
                <option value="">All</option>
                {frameworkOptions.map(({ name, count }) => (
                  <option key={name} value={name}>{name} ({count})</option>
                ))}
              </select>
            </label>

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

          <div className="browse-stat-row">
            <span className="browse-stat">
              {results.length === index.packages.length
                ? `${index.packageCount} packages`
                : `${results.length} of ${index.packageCount} packages`}
              {hasMore && ` (showing first ${DISPLAY_LIMIT})`}
            </span>
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
