import { FileCode2, FileJson2, LoaderCircle, Upload } from "lucide-react";
import { useRef, useState } from "react";

interface ImportScreenProps {
  error?: string | null;
  loading: boolean;
  onFilesSelected: (files: File[]) => void;
}

export function ImportScreen({ error, loading, onFilesSelected }: ImportScreenProps) {
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
          Drop <code>opencli.json</code>, add <code>xmldoc.xml</code> if you have it, and explore the
          command graph locally with relocatable static assets.
        </p>

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
          <p>OpenCLI JSON stays the source of truth. The viewer enriches in-browser instead of relying on a custom schema.</p>
        </article>

        <article className="fact-card panel">
          <div className="fact-icon">
            <FileCode2 aria-hidden="true" />
          </div>
          <h2>Optional XML</h2>
          <p>XML descriptions only fill blank command, option, and argument descriptions. Existing JSON content wins.</p>
        </article>
      </section>
    </main>
  );
}
