import { FileCode2, FileJson2, LoaderCircle, Search, Upload } from "lucide-react";
import { useRef, useState } from "react";
import { NugetToolRequest } from "../data/loadNugetTool";
import { NugetToolPanel } from "./NugetToolPanel";

type ImportMode = "files" | "nuget";

interface ImportScreenProps {
  error?: string | null;
  loading: boolean;
  mode: ImportMode;
  onFilesSelected: (files: File[]) => void;
  onModeChange: (mode: ImportMode) => void;
  onToolInspect: (request: NugetToolRequest) => void;
}

export function ImportScreen({
  error,
  loading,
  mode,
  onFilesSelected,
  onModeChange,
  onToolInspect,
}: ImportScreenProps) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [isDragging, setIsDragging] = useState(false);

  function openPicker() {
    inputRef.current?.click();
  }

  function handleFiles(fileList: FileList | null) {
    if (!fileList) {
      return;
    }

    onFilesSelected(Array.from(fileList));
  }

  return (
    <main className="import-screen">
      <section className="import-hero panel">
        <div className="eyebrow">InSpectraUI</div>
        <h1>Inspect a CLI snapshot without rebuilding the viewer.</h1>
        <p className="lede">
          Switch between local OpenCLI snapshots and browser-side NuGet inspection without rebuilding the viewer or
          shipping a backend.
        </p>

        <div className="import-mode-row" role="tablist" aria-label="Source mode">
          <button
            type="button"
            role="tab"
            aria-selected={mode === "files"}
            className={`mode-pill${mode === "files" ? " active" : ""}`}
            onClick={() => onModeChange("files")}
          >
            <Upload aria-hidden="true" />
            <span>Files</span>
          </button>
          <button
            type="button"
            role="tab"
            aria-selected={mode === "nuget"}
            className={`mode-pill${mode === "nuget" ? " active" : ""}`}
            onClick={() => onModeChange("nuget")}
          >
            <Search aria-hidden="true" />
            <span>NuGet Tool</span>
          </button>
        </div>

        {mode === "files" ? (
          <div
            className={`import-dropzone ${isDragging ? "dragging" : ""}`}
            role="button"
            tabIndex={0}
            aria-label="Import OpenCLI snapshot"
            onClick={openPicker}
            onKeyDown={(event) => {
              if (event.key === "Enter" || event.key === " ") {
                event.preventDefault();
                openPicker();
              }
            }}
            onDragEnter={(event) => {
              event.preventDefault();
              setIsDragging(true);
            }}
            onDragOver={(event) => {
              event.preventDefault();
              setIsDragging(true);
            }}
            onDragLeave={(event) => {
              event.preventDefault();
              if (event.currentTarget === event.target) {
                setIsDragging(false);
              }
            }}
            onDrop={(event) => {
              event.preventDefault();
              setIsDragging(false);
              handleFiles(event.dataTransfer.files);
            }}
          >
            <div className="dropzone-icon">
              {loading ? <LoaderCircle className="spin" aria-hidden="true" /> : <Upload aria-hidden="true" />}
            </div>
            <div className="dropzone-copy">
              <strong>{loading ? "Loading snapshot" : "Drop your files here"}</strong>
              <span>
                {loading
                  ? "Parsing OpenCLI and applying XML enrichment."
                  : "Choose one or two files: opencli.json and optional xmldoc.xml."}
              </span>
            </div>
            <button type="button" className="secondary-button" disabled={loading}>
              {loading ? "Working…" : "Pick files"}
            </button>
            <input
              ref={inputRef}
              aria-label="OpenCLI files"
              className="visually-hidden"
              type="file"
              multiple
              accept=".json,.xml"
              onChange={(event) => {
                handleFiles(event.target.files);
                event.target.value = "";
              }}
            />
          </div>
        ) : (
          <NugetToolPanel loading={loading} onInspect={onToolInspect} />
        )}

        {error ? (
          <p className="inline-alert" role="alert">
            {error}
          </p>
        ) : null}
      </section>

      <section className="import-facts">
        <article className="fact-card panel">
          <div className="fact-icon">
            <FileJson2 aria-hidden="true" />
          </div>
          <h2>Canonical input</h2>
          <p>OpenCLI JSON stays the source of truth. File import and generated NuGet probes both end up in the same viewer model.</p>
        </article>

        <article className="fact-card panel">
          <div className="fact-icon">
            <FileCode2 aria-hidden="true" />
          </div>
          <h2>Static inspection</h2>
          <p>NuGet mode downloads the package in-browser and inspects Spectre.Console.Cli metadata without executing the tool on a server.</p>
        </article>
      </section>
    </main>
  );
}
