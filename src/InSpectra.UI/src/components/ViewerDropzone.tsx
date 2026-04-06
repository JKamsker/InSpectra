import { Upload, FileJson2, FileCode2 } from "lucide-react";
import { useCallback, useEffect, useRef, useState } from "react";

interface ViewerDropzoneProps {
  onFilesSelected: (files: File[]) => void;
}

export function ViewerDropzone({ onFilesSelected }: ViewerDropzoneProps) {
  const [mounted, setMounted] = useState(false);
  const [active, setActive] = useState(false);
  const [hovering, setHovering] = useState(false);
  const hideTimer = useRef<number>(undefined);
  const unmountTimer = useRef<number>(undefined);

  function show() {
    clearTimeout(hideTimer.current);
    clearTimeout(unmountTimer.current);
    setMounted(true);
    // Flush layout so the enter animation triggers after mount.
    requestAnimationFrame(() => setActive(true));
  }

  function hide() {
    clearTimeout(hideTimer.current);
    setActive(false);
    setHovering(false);
    // Keep in DOM until the CSS fade-out finishes.
    clearTimeout(unmountTimer.current);
    unmountTimer.current = window.setTimeout(() => setMounted(false), 250);
  }

  // `dragover` fires continuously (~50-350ms) while a drag is inside the
  // window and stops the moment it leaves. A short timeout after the last
  // `dragover` reliably hides the overlay — no depth counters needed.
  useEffect(() => {
    function scheduleHide() {
      clearTimeout(hideTimer.current);
      hideTimer.current = window.setTimeout(hide, 80);
    }

    function handleDragOver(e: DragEvent) {
      if (!e.dataTransfer?.types.includes("Files")) return;
      e.preventDefault();
      clearTimeout(hideTimer.current);
      show();
      scheduleHide();
    }

    function handleDrop(e: DragEvent) {
      e.preventDefault();
      hide();
    }

    window.addEventListener("dragover", handleDragOver);
    window.addEventListener("drop", handleDrop);

    return () => {
      clearTimeout(hideTimer.current);
      clearTimeout(unmountTimer.current);
      window.removeEventListener("dragover", handleDragOver);
      window.removeEventListener("drop", handleDrop);
    };
  }, []);

  const handleOverlayDragOver = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    setHovering(true);
  }, []);

  const handleOverlayDragLeave = useCallback((e: React.DragEvent) => {
    if (e.currentTarget === e.target) {
      setHovering(false);
    }
  }, []);

  const handleOverlayDrop = useCallback(
    (e: React.DragEvent) => {
      e.preventDefault();
      e.stopPropagation();
      hide();
      if (e.dataTransfer.files.length > 0) {
        onFilesSelected(Array.from(e.dataTransfer.files));
      }
    },
    [onFilesSelected],
  );

  if (!mounted) return null;

  const cls =
    "viewer-dropzone-overlay" +
    (active ? " active" : "") +
    (hovering ? " hovering" : "");

  return (
    <div
      className={cls}
      onDragOver={handleOverlayDragOver}
      onDragLeave={handleOverlayDragLeave}
      onDrop={handleOverlayDrop}
    >
      <div className="viewer-dropzone-content">
        <div className="viewer-dropzone-icon">
          <Upload aria-hidden="true" />
        </div>
        <div className="viewer-dropzone-text">
          <strong>Drop to inspect</strong>
          <span>Load an OpenCLI snapshot into the viewer</span>
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
      </div>
    </div>
  );
}
