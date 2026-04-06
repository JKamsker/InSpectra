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
    if (fileList) onFilesSelected(Array.from(fileList));
  }

  return (
    <main
      className={`import-page${isDragging ? " dragging" : ""}`}
      onDragEnter={(e) => { e.preventDefault(); setIsDragging(true); }}
      onDragOver={(e) => { e.preventDefault(); setIsDragging(true); }}
      onDragLeave={(e) => { e.preventDefault(); if (e.currentTarget === e.target) setIsDragging(false); }}
      onDrop={(e) => { e.preventDefault(); setIsDragging(false); handleFiles(e.dataTransfer.files); }}
      onClick={openPicker}
      role="button"
      tabIndex={0}
      onKeyDown={(e) => { if (e.key === "Enter" || e.key === " ") { e.preventDefault(); openPicker(); } }}
      aria-label="Import OpenCLI snapshot"
    >
      <div className="import-page-card">
        <div className="viewer-dropzone-icon">
          {loading
            ? <LoaderCircle className="spin" aria-hidden="true" />
            : <Upload aria-hidden="true" />}
        </div>

        <div className="viewer-dropzone-text">
          <strong>{loading ? "Importing snapshot" : "Drop files or click to browse"}</strong>
          <span>
            {loading
              ? "Parsing OpenCLI and applying XML enrichment."
              : "Load an OpenCLI snapshot into the viewer"}
          </span>
        </div>

        <div className="viewer-dropzone-files">
          <div className="viewer-dropzone-file-tag">
            <FileJson2 aria-hidden="true" />
            <span>opencli.json</span>
          </div>
          <span className="viewer-dropzone-plus">+</span>
          <div className="viewer-dropzone-file-tag optional">
            <FileCode2 aria-hidden="true" />
            <span>xmldoc.xml</span>
            <em>optional</em>
          </div>
        </div>

        {error && <p className="import-page-error" role="alert">{error}</p>}
      </div>

      <input
        ref={inputRef}
        aria-label="OpenCLI files"
        className="visually-hidden"
        type="file"
        multiple
        accept=".json,.xml"
        onChange={(e) => { handleFiles(e.target.files); e.target.value = ""; }}
      />
    </main>
  );
}
