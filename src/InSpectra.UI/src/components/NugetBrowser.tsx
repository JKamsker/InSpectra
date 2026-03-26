import { ArrowLeft, CheckCircle2, AlertTriangle, LoaderCircle, Package, Search, ExternalLink } from "lucide-react";
import { useEffect, useDeferredValue, useRef, useState } from "react";
import {
  DiscoveryIndex,
  DiscoveryPackage,
  fetchDiscoveryIndex,
  findPackageById,
  resolvePackageUrls,
  searchPackages,
} from "../data/nugetDiscovery";
import { buildBrowseHash } from "../data/navigation";

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

  // Ctrl+F: focus search input when visible, otherwise open browse palette
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
      onSelect={(pkgId) => {
        window.location.hash = buildBrowseHash(pkgId);
      }}
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
              <button
                type="button"
                className="secondary-button"
                onClick={() => history.back()}
                style={{ marginTop: "1rem" }}
              >
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
  const command = latestVer?.command;

  return (
    <a
      className="browse-card panel"
      href={buildBrowseHash(pkg.packageId)}
    >
      <div className="browse-card-header">
        <Package aria-hidden="true" className="browse-card-icon" />
        <div className="browse-card-title">{pkg.packageId}</div>
        <StatusBadge status={pkg.latestStatus} />
      </div>
      <div className="browse-card-meta">
        {command && (
          <span className="browse-card-command">
            <code>{command}</code>
          </span>
        )}
        <span className="browse-card-version">v{pkg.latestVersion}</span>
        {pkg.versions.length > 1 && (
          <span className="browse-card-versions">{pkg.versions.length} versions</span>
        )}
      </div>
    </a>
  );
}

function StatusBadge({ status }: { status: "ok" | "partial" }) {
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

function PackageDetail({
  pkg,
  selectedVersion,
  onLoadPackage,
}: {
  pkg: DiscoveryPackage;
  selectedVersion?: string;
  onLoadPackage: (opencliUrl: string, xmldocUrl: string, label: string, packageId: string, version: string | undefined) => void;
}) {
  const [loadingSpec, setLoadingSpec] = useState(false);
  const activeVersion = selectedVersion || pkg.latestVersion;
  const versionInfo = pkg.versions.find((v) => v.version === activeVersion) || pkg.versions[0];

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
          <div>
            <div className="eyebrow">NuGet Browser</div>
            <h1>{pkg.packageId}</h1>
          </div>
          <button
            type="button"
            className="secondary-button"
            onClick={() => history.back()}
          >
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
            <span className="browse-detail-label">NuGet</span>
            <a
              href={`https://www.nuget.org/packages/${pkg.packageId}`}
              target="_blank"
              rel="noopener noreferrer"
              className="browse-nuget-link"
            >
              View on nuget.org <ExternalLink aria-hidden="true" size={12} />
            </a>
          </div>
        </div>
      </section>

      <section className="browse-versions panel section-card">
        <div className="section-heading">
          <Package aria-hidden="true" />
          <h2>Versions</h2>
        </div>

        <div className="browse-version-list">
          {pkg.versions.map((ver) => (
            <div
              key={ver.version}
              className={`browse-version-row ${ver.version === activeVersion ? "active" : ""}`}
            >
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

function formatDate(iso: string): string {
  try {
    return new Date(iso).toLocaleDateString(undefined, {
      year: "numeric",
      month: "short",
      day: "numeric",
    });
  } catch {
    return iso;
  }
}

/* ── Browse palette (Ctrl+F when search field is not visible) ── */

interface BrowsePaletteProps {
  packages: DiscoveryPackage[];
  open: boolean;
  onClose: () => void;
  onSelect: (packageId: string) => void;
}

function BrowsePalette({ packages, open, onClose, onSelect }: BrowsePaletteProps) {
  const [query, setQuery] = useState("");
  const [activeIndex, setActiveIndex] = useState(0);
  const inputRef = useRef<HTMLInputElement>(null);
  const resultsRef = useRef<HTMLDivElement>(null);

  const filtered = query
    ? packages.filter(
        (pkg) =>
          pkg.packageId.toLowerCase().includes(query.toLowerCase()) ||
          (pkg.versions[0]?.command ?? "").toLowerCase().includes(query.toLowerCase()),
      )
    : packages;

  useEffect(() => {
    if (open) {
      setQuery("");
      setActiveIndex(0);
      requestAnimationFrame(() => inputRef.current?.focus());
    }
  }, [open]);

  useEffect(() => {
    setActiveIndex(0);
  }, [query]);

  useEffect(() => {
    const active = resultsRef.current?.querySelector(".cmd-item.active");
    active?.scrollIntoView({ block: "nearest" });
  }, [activeIndex]);

  function handleKeyDown(e: React.KeyboardEvent) {
    if (e.key === "ArrowDown") {
      e.preventDefault();
      setActiveIndex((i) => (filtered.length === 0 ? 0 : Math.min(i + 1, filtered.length - 1)));
    } else if (e.key === "ArrowUp") {
      e.preventDefault();
      setActiveIndex((i) => Math.max(i - 1, 0));
    } else if (e.key === "Enter") {
      e.preventDefault();
      if (filtered[activeIndex]) {
        onSelect(filtered[activeIndex].packageId);
        onClose();
      }
    } else if (e.key === "Escape") {
      e.preventDefault();
      onClose();
    }
  }

  if (!open) return null;

  return (
    <div className="cmd-palette" onKeyDown={handleKeyDown}>
      <div className="cmd-backdrop" onClick={onClose} />
      <div className="cmd-dialog" role="dialog" aria-modal="true" aria-label="Package search">
        <div className="cmd-header">
          <Search width={16} height={16} />
          <input
            ref={inputRef}
            className="cmd-input"
            type="text"
            placeholder="Search packages…"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
          />
          <kbd>Esc</kbd>
        </div>
        <div className="cmd-results" ref={resultsRef}>
          {filtered.length === 0 ? (
            <div className="cmd-empty">No packages found.</div>
          ) : (
            filtered.map((pkg, i) => (
              <button
                key={pkg.packageId}
                type="button"
                className={`cmd-item${i === activeIndex ? " active" : ""}`}
                onMouseEnter={() => setActiveIndex(i)}
                onClick={() => {
                  onSelect(pkg.packageId);
                  onClose();
                }}
              >
                <span className="cmd-path">{highlightMatch(pkg.packageId, query)}</span>
                {pkg.versions[0]?.command && (
                  <span className="cmd-desc">{highlightMatch(pkg.versions[0].command, query)}</span>
                )}
              </button>
            ))
          )}
        </div>
        <div className="cmd-footer">
          <span>
            <kbd>&uarr;</kbd>
            <kbd>&darr;</kbd> Navigate
          </span>
          <span>
            <kbd>&crarr;</kbd> Open
          </span>
          <span>
            <kbd>Esc</kbd> Close
          </span>
        </div>
      </div>
    </div>
  );
}

function highlightMatch(text: string, query: string): React.ReactNode {
  if (!query) return text;
  const lowerText = text.toLowerCase();
  const lowerQuery = query.toLowerCase();
  const idx = lowerText.indexOf(lowerQuery);
  if (idx === -1) return text;

  return (
    <>
      {text.slice(0, idx)}
      <mark>{text.slice(idx, idx + query.length)}</mark>
      {text.slice(idx + query.length)}
    </>
  );
}
