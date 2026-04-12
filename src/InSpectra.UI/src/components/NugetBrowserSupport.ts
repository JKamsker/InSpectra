import type { SyntheticEvent } from "react";
import { DEFAULT_PACKAGE_ICON_URL, type DiscoveryPackageSummary } from "../data/nugetDiscovery";

export type BrowseOrder =
  | "index"
  | "updated"
  | "created"
  | "downloads"
  | "name"
  | "commands"
  | "groups"
  | "versions";

export const FULL_INDEX_HYDRATION_DELAY_MS = 3000;

export function handlePackageIconError(event: SyntheticEvent<HTMLImageElement>) {
  const image = event.currentTarget;
  if (image.src === DEFAULT_PACKAGE_ICON_URL) {
    return;
  }

  image.src = DEFAULT_PACKAGE_ICON_URL;
}

export function formatCount(value: number): string {
  return new Intl.NumberFormat(undefined, { notation: "compact", maximumFractionDigits: 1 }).format(value);
}

export function formatNumber(value: number): string {
  return new Intl.NumberFormat().format(value);
}

export function formatRelativeAgeShort(iso: string): string {
  const diffMs = Date.now() - Date.parse(iso);
  if (!Number.isFinite(diffMs) || diffMs < 0) {
    return "?";
  }

  const hourMs = 60 * 60 * 1000;
  const dayMs = 24 * hourMs;
  const monthMs = 30 * dayMs;
  const yearMs = 365 * dayMs;

  if (diffMs >= yearMs) return `${Math.floor(diffMs / yearMs)}yr`;
  if (diffMs >= monthMs) return `${Math.floor(diffMs / monthMs)}mo`;
  if (diffMs >= dayMs) return `${Math.floor(diffMs / dayMs)}d`;
  if (diffMs >= hourMs) return `${Math.floor(diffMs / hourMs)}h`;
  return "<1h";
}

export function formatRelativeAgeLong(iso: string): string {
  const diffMs = Date.now() - Date.parse(iso);
  if (!Number.isFinite(diffMs) || diffMs < 0) {
    return "unknown";
  }

  const hourMs = 60 * 60 * 1000;
  const dayMs = 24 * hourMs;
  const monthMs = 30 * dayMs;
  const yearMs = 365 * dayMs;

  if (diffMs >= yearMs) return `${Math.floor(diffMs / yearMs)} year${diffMs >= 2 * yearMs ? "s" : ""}`;
  if (diffMs >= monthMs) return `${Math.floor(diffMs / monthMs)} month${diffMs >= 2 * monthMs ? "s" : ""}`;
  if (diffMs >= dayMs) return `${Math.floor(diffMs / dayMs)} day${diffMs >= 2 * dayMs ? "s" : ""}`;
  if (diffMs >= hourMs) return `${Math.floor(diffMs / hourMs)} hour${diffMs >= 2 * hourMs ? "s" : ""}`;
  return "less than 1 hour";
}

export function buildUpdatedTooltip(iso: string): string {
  return `Updated: ${formatRelativeAgeLong(iso)} ago (${formatAbsoluteDate(iso)})`;
}

export function formatAbsoluteDate(iso: string): string {
  try {
    return new Date(iso).toLocaleString(undefined, {
      year: "numeric",
      month: "short",
      day: "numeric",
      hour: "numeric",
      minute: "2-digit",
    });
  } catch {
    return iso;
  }
}

export function sortPackages(packages: DiscoveryPackageSummary[], orderBy: BrowseOrder): DiscoveryPackageSummary[] {
  const sorted = [...packages];

  switch (orderBy) {
    case "updated":
      return sorted.sort((left, right) =>
        compareIsoDatesDesc(left.updatedAt, right.updatedAt) || left.packageId.localeCompare(right.packageId),
      );
    case "created":
      return sorted.sort((left, right) =>
        compareIsoDatesDesc(left.createdAt, right.createdAt) || left.packageId.localeCompare(right.packageId),
      );
    case "downloads":
      return sorted.sort((left, right) =>
        right.totalDownloads - left.totalDownloads || left.packageId.localeCompare(right.packageId),
      );
    case "name":
      return sorted.sort((left, right) => left.packageId.localeCompare(right.packageId));
    case "commands":
      return sorted.sort((left, right) =>
        right.commandCount - left.commandCount || left.packageId.localeCompare(right.packageId),
      );
    case "groups":
      return sorted.sort((left, right) =>
        right.commandGroupCount - left.commandGroupCount || left.packageId.localeCompare(right.packageId),
      );
    case "versions":
      return sorted.sort((left, right) =>
        right.versionCount - left.versionCount || left.packageId.localeCompare(right.packageId),
      );
    case "index":
    default:
      return sorted;
  }
}

export function splitFrameworks(value: string | undefined): string[] {
  if (!value) {
    return [];
  }

  return value.split("+").map((segment) => segment.trim()).filter(Boolean);
}

function compareIsoDatesDesc(left: string, right: string): number {
  return Date.parse(right) - Date.parse(left);
}
