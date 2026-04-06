import { useEffect, useRef, useState, useDeferredValue, type MutableRefObject } from "react";

function readBool(key: string, fallback: boolean): boolean {
  const v = localStorage.getItem(key);
  return v === null ? fallback : v === "true";
}

function readNumber(key: string, fallback: number): number {
  const v = localStorage.getItem(key);
  if (v === null) return fallback;
  const n = Number(v);
  return Number.isFinite(n) ? n : fallback;
}

export function useViewerInteraction() {
  const searchInputRef = useRef<HTMLInputElement>(null);
  const mobileSearchInputRef = useRef<HTMLInputElement>(null);

  const [searchTerm, setSearchTerm] = useState("");
  const deferredSearch = useDeferredValue(searchTerm);
  const [paletteOpen, setPaletteOpen] = useState(false);
  const [composerWidth, setComposerWidth] = useState(() => readNumber("inspectra-composer-width", 304));
  const composerOpenedByUser = useRef(false);
  const [composerOpen, setComposerOpen] = useState(() => {
    if (window.innerWidth <= 768) return false;
    const saved = readBool("inspectra-composer-open", true);
    if (!saved) return false;
    // If default-open would cause floating, start collapsed
    const rem = parseFloat(getComputedStyle(document.documentElement).fontSize);
    const sidebarW = 17 * rem;
    return window.innerWidth - sidebarW - readNumber("inspectra-composer-width", 304) >= 490;
  });
  const [mobileSidebarOpen, setMobileSidebarOpen] = useState(false);
  const [mobileSidebarSearch, setMobileSidebarSearch] = useState(false);

  useEffect(() => {
    function handleKeyDown(e: KeyboardEvent) {
      const mod = e.ctrlKey || e.metaKey;
      if (mod && e.key === "f") {
        e.preventDefault();
        searchInputRef.current?.focus();
      }
      if (mod && e.key === "k") {
        e.preventDefault();
        setPaletteOpen((o) => !o);
      }
    }
    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, []);

  function toggleComposer() {
    setComposerOpen((prev) => {
      const next = !prev;
      composerOpenedByUser.current = next;
      localStorage.setItem("inspectra-composer-open", String(next));
      if (next) setMobileSidebarOpen(false);
      return next;
    });
  }

  function handleComposerResize(width: number) {
    setComposerWidth(width);
    localStorage.setItem("inspectra-composer-width", String(width));
  }

  function handleMobileCommandSelect(path: string, onNavigate: (path: string) => void) {
    onNavigate(path);
    setMobileSidebarOpen(false);
  }

  return {
    searchInputRef,
    mobileSearchInputRef,
    searchTerm,
    deferredSearch,
    paletteOpen,
    composerOpen,
    composerOpenedByUser,
    composerWidth,
    mobileSidebarOpen,
    mobileSidebarSearch,
    setSearchTerm,
    setPaletteOpen,
    setComposerOpen,
    setMobileSidebarOpen,
    setMobileSidebarSearch,
    toggleComposer,
    handleComposerResize,
    handleMobileCommandSelect,
  };
}
