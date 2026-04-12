import { Check, Copy } from "lucide-react";
import { useEffect, useRef, useState } from "react";

interface CopyButtonProps {
  text: string;
  className?: string;
  title?: string;
  copiedTitle?: string;
  label?: string;
  copiedLabel?: string;
  iconSize?: number;
  resetDelayMs?: number;
}

export function CopyButton({
  text,
  className = "example-copy",
  title = "Copy",
  copiedTitle,
  label,
  copiedLabel,
  iconSize,
  resetDelayMs = 1500,
}: CopyButtonProps) {
  const [copied, setCopied] = useState(false);
  const resetTimerRef = useRef<number | null>(null);

  useEffect(() => () => {
    if (resetTimerRef.current !== null) {
      window.clearTimeout(resetTimerRef.current);
    }
  }, []);

  async function copy() {
    try {
      await navigator.clipboard.writeText(text);
      setCopied(true);
      if (resetTimerRef.current !== null) {
        window.clearTimeout(resetTimerRef.current);
      }

      resetTimerRef.current = window.setTimeout(() => {
        setCopied(false);
        resetTimerRef.current = null;
      }, resetDelayMs);
    } catch {
      /* clipboard unavailable */
    }
  }

  const buttonTitle = copied ? copiedTitle ?? title : title;
  const buttonLabel = copied ? copiedLabel ?? label : label;
  const buttonClassName = copied ? `${className} copied` : className;

  return (
    <button type="button" className={buttonClassName} onClick={copy} title={buttonTitle} aria-label={buttonTitle}>
      {copied ? <Check size={iconSize} /> : <Copy size={iconSize} />}
      {buttonLabel ? <span>{buttonLabel}</span> : null}
    </button>
  );
}
