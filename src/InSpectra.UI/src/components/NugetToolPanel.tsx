import { LoaderCircle, Search } from "lucide-react";
import { FormEvent, useState } from "react";
import { NugetSearchResult, searchNugetTools } from "../data/nugetTools";
import { NugetToolRequest } from "../data/loadNugetTool";

interface NugetToolPanelProps {
  loading: boolean;
  onInspect: (request: NugetToolRequest) => void;
}

export function NugetToolPanel({ loading, onInspect }: NugetToolPanelProps) {
  const [query, setQuery] = useState("");
  const [includePrerelease, setIncludePrerelease] = useState(false);
  const [searching, setSearching] = useState(false);
  const [searchError, setSearchError] = useState<string | null>(null);
  const [results, setResults] = useState<NugetSearchResult[]>([]);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [selectedVersion, setSelectedVersion] = useState("");

  const selected = results.find((item) => item.id === selectedId) ?? null;
  const versions = selected?.versions.length ? selected.versions : selected ? [selected.version] : [];

  async function handleSearch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSearching(true);
    setSearchError(null);

    try {
      const nextResults = await searchNugetTools(query, includePrerelease);
      setResults(nextResults);

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
        Search NuGet.org, download the package locally, and build a best-effort UI from static Spectre.Console.Cli
        metadata. No server-side execution is involved.
      </p>

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
                  onChange={(event) => setSelectedVersion(event.target.value)}
                >
                  {versions.map((version) => (
                    <option key={version} value={version}>
                      {version}
                    </option>
                  ))}
                </select>
              </label>

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
    </section>
  );
}

function toMessage(error: unknown): string {
  return error instanceof Error ? error.message : "Unknown error.";
}
