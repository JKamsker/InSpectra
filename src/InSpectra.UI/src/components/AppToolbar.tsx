import { Eye, EyeOff, FileCode2, FileUp, PanelRight, PanelRightClose, Search, Sparkles } from "lucide-react";
import { useRef } from "react";
import { ViewerOptions } from "../boot/contracts";
import { ThemeToggle } from "./ThemeToggle";

interface AppToolbarProps {
  composerOpen: boolean;
  hasDocument: boolean;
  onDownloadJson: () => void;
  onFilesSelected: (files: File[]) => void;
  onPaletteOpen: () => void;
  onResetToImport: () => void;
  onToggleComposer: () => void;
  onToggleOption: (option: keyof ViewerOptions) => void;
  viewerOptions: ViewerOptions;
}

export function AppToolbar({
  composerOpen,
  hasDocument,
  onDownloadJson,
  onFilesSelected,
  onPaletteOpen,
  onResetToImport,
  onToggleComposer,
  onToggleOption,
  viewerOptions,
}: AppToolbarProps) {
  const pickerRef = useRef<HTMLInputElement>(null);

  return (
    <div className="toolbar">
      <button type="button" className="toolbar-button" onClick={() => onToggleOption("includeHidden")}>
        {viewerOptions.includeHidden ? <EyeOff aria-hidden="true" /> : <Eye aria-hidden="true" />}
        <span>{viewerOptions.includeHidden ? "Hide hidden" : "Show hidden"}</span>
      </button>

      <button type="button" className="toolbar-button" onClick={() => onToggleOption("includeMetadata")}>
        <Sparkles aria-hidden="true" />
        <span>{viewerOptions.includeMetadata ? "Hide metadata" : "Show metadata"}</span>
      </button>

      <button type="button" className="toolbar-button" onClick={() => pickerRef.current?.click()}>
        <FileUp aria-hidden="true" />
        <span>Import files</span>
      </button>
      <input
        ref={pickerRef}
        className="visually-hidden"
        type="file"
        multiple
        accept=".json,.xml"
        onChange={(event) => {
          onFilesSelected(Array.from(event.target.files ?? []));
          event.target.value = "";
        }}
      />

      <button type="button" className="toolbar-button" onClick={onResetToImport}>
        <Search aria-hidden="true" />
        <span>Sources</span>
      </button>

      {hasDocument ? (
        <button type="button" className="toolbar-button" onClick={onDownloadJson}>
          <FileCode2 aria-hidden="true" />
          <span>Download JSON</span>
        </button>
      ) : null}

      <button type="button" className="toolbar-button" onClick={onPaletteOpen} title="Search commands (Ctrl+K)">
        <Search aria-hidden="true" />
        <span>Search</span>
        <kbd className="kbd-hint">Ctrl K</kbd>
      </button>

      <button
        type="button"
        className={`toolbar-button${composerOpen ? " active" : ""}`}
        onClick={onToggleComposer}
        title="Toggle Composer"
      >
        {composerOpen ? <PanelRightClose aria-hidden="true" /> : <PanelRight aria-hidden="true" />}
        <span>Composer</span>
      </button>

      <ThemeToggle />
    </div>
  );
}
