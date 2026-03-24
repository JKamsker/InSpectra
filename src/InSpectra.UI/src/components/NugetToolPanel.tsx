import { LoaderCircle, Search } from "lucide-react";
import { FormEvent, useEffect, useState } from "react";
import { ProbeDiagnostics } from "../data/toolProbe";
import { fetchNugetToolVersions, NugetSearchResult, searchNugetTools } from "../data/nugetTools";
import { NugetToolRequest } from "../data/loadNugetTool";
import { ProbeDiagnosticsCard } from "./ProbeDiagnosticsCard";

interface NugetToolPanelProps {
  diagnostics?: ProbeDiagnostics | null;
  loading: boolean;
  onInspect: (request: NugetToolRequest) => void;
}

export function NugetToolPanel({ diagnostics, loading, onInspect }: NugetToolPanelProps) {
  const [query, setQuery] = useState("");
  const [includePrerelease, setIncludePrerelease] = useState(false);
  const [searching, setSearching] = useState(false);
  const [searchError, setSearchError] = useState<string | null>(null);
  const [results, setResults] = useState<NugetSearchResult[]>([]);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [selectedVersion, setSelectedVersion] = useState("");
  const [versionError, setVersionError] = useState<string | null>(null);
  const [versionOptions, setVersionOptions] = useState<string[]>([]);
  const [versionsLoading, setVersionsLoading] = useState(false);

  const selected = results.find((item) => item.id === selectedId) ?? null;
  const versions = versionOptions.length ? versionOptions : selected?.versions.length ? selected.versions : selected ? [selected.version] : [];

  useEffect(() => {
    if (!selected) {
      setVersionOptions([]);
      setVersionError(null);
      setVersionsLoading(false);
      return;
    }

    const fallbackVersions = selected.versions.length ? selected.versions : [selected.version];
    let cancelled = false;
    setVersionOptions(fallbackVersions);
    setVersionError(null);
    setVersionsLoading(true);

    void fetchNugetToolVersions(selected.id, includePrerelease)
      .then((nextVersions) => {
        if (cancelled) {
          return;
        }

        const resolvedVersions = nextVersions.length ? nextVersions : fallbackVersions;
        setVersionOptions(resolvedVersions);
        setSelectedVersion((current) => (resolvedVersions.includes(current) ? current : resolvedVersions[0] ?? ""));
      })
      .catch((error) => {
        if (cancelled) {
          return;
        }

        setVersionError(toMessage(error));
        setVersionOptions(fallbackVersions);
        setSelectedVersion((current) => current || fallbackVersions[0] || "");
      })
      .finally(() => {
        if (!cancelled) {
          setVersionsLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [includePrerelease, selected]);

  async function handleSearch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSearching(true);
    setSearchError(null);

    try {
      const nextResults = await searchNugetTools(query, includePrerelease);
      setResults(nextResults);
      setVersionOptions([]);
      setVersionError(null);

      const nextSelected = nextResults[0] ?? null;
      setSelectedId(nextSelected?.id ?? null);
      setSelectedVersion(nextSelected?.version ?? "");
      if (nextResults.length === 0) {
        setSearchError("No matching .NET tool packages were found on NuGet.org.");
      }
    } catch (error) {
      setSearchError(toMessage(error));
    } finally {
      setSearching(false);
    }
  }

  return (
    <section className="nuget-panel panel">
      <div className="eyebrow">NuGet Tool</div>
      <h2>Inspect a published .NET tool without leaving the browser.</h2>
      <p className="lede">
        Search NuGet.org, download the package locally, and inspect it entirely in the browser. This mode only works
        when the package bundles <code>opencli.json</code> or exposes a statically recoverable
        <code> Spectre.Console.Cli </code> command graph.
      </p>
      <ul className="nuget-contract-list">
        <li>No backend and no remote execution.</li>
        <li>No tool code execution inside the browser.</li>
        <li>Best results come from packaged OpenCLI snapshots.</li>
      </ul>

      <form className="nuget-search" onSubmit={handleSearch}>
        <label className="nuget-field">
          <span>Package ID or query</span>
          <input
            aria-label="NuGet package query"
            type="text"
            value={query}
            placeholder="inspectra or JellyfinCli"
            onChange={(event) => setQuery(event.target.value)}
          />
        </label>

        <label className="nuget-toggle">
          <input
            checked={includePrerelease}
            type="checkbox"
            onChange={(event) => setIncludePrerelease(event.target.checked)}
          />
          <span>Include prerelease versions</span>
        </label>

        <button type="submit" className="secondary-button" disabled={searching || loading || !query.trim()}>
          {searching ? <LoaderCircle className="spin" aria-hidden="true" /> : <Search aria-hidden="true" />}
          <span>{searching ? "Searching…" : "Search NuGet"}</span>
        </button>
      </form>

      {searchError ? (
        <p className="inline-alert" role="alert">
          {searchError}
        </p>
      ) : null}

      {results.length > 0 ? (
        <div className="nuget-grid">
          <div className="nuget-results">
            {results.map((item) => (
              <button
                key={item.id}
                type="button"
                className={`nuget-result${item.id === selectedId ? " selected" : ""}`}
                onClick={() => {
                  setSelectedId(item.id);
                  setSelectedVersion(item.version);
                  setVersionOptions([]);
                  setVersionError(null);
                }}
              >
                <strong>{item.id}</strong>
                <span>{item.description || "No package description."}</span>
                <small>{item.version}</small>
              </button>
            ))}
          </div>

          {selected ? (
            <article className="nuget-summary panel">
              <div className="section-heading">
                <Search aria-hidden="true" />
                <h2>Package facts</h2>
              </div>
              <dl className="nuget-facts">
                <div>
                  <dt>ID</dt>
                  <dd>{selected.id}</dd>
                </div>
                <div>
                  <dt>Authors</dt>
                  <dd>{selected.authors || "Unknown"}</dd>
                </div>
                <div>
                  <dt>Latest</dt>
                  <dd>{selected.version}</dd>
                </div>
                <div>
                  <dt>Downloads</dt>
                  <dd>{selected.totalDownloads?.toLocaleString() || "Unknown"}</dd>
                </div>
              </dl>

              <label className="nuget-field compact">
                <span>Version</span>
                <select
                  aria-label="Package version"
                  value={selectedVersion}
                  disabled={versionsLoading && versions.length === 0}
                  onChange={(event) => setSelectedVersion(event.target.value)}
                >
                  {versions.map((version) => (
                    <option key={version} value={version}>
                      {version}
                    </option>
                  ))}
                </select>
              </label>

              <p className="nuget-version-note">
                {versionsLoading
                  ? "Loading published versions from NuGet..."
                  : versionError
                    ? versionError
                    : `Loaded ${versions.length} published version${versions.length === 1 ? "" : "s"}.`}
              </p>

              <button
                type="button"
                className="secondary-button"
                disabled={loading || !selectedVersion}
                onClick={() => onInspect({ id: selected.id, version: selectedVersion })}
              >
                {loading ? <LoaderCircle className="spin" aria-hidden="true" /> : <Search aria-hidden="true" />}
                <span>{loading ? "Inspecting…" : "Inspect tool"}</span>
              </button>
            </article>
          ) : null}
        </div>
      ) : null}

      {diagnostics ? <ProbeDiagnosticsCard diagnostics={diagnostics} /> : null}
    </section>
  );
}

function toMessage(error: unknown): string {
  return error instanceof Error ? error.message : "Unknown error.";
}
