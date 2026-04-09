import { defaultFeatureFlags, FeatureFlags, ViewerOptions } from "../boot/contracts";
import { StartupRequest } from "../boot/bootstrap";
import { cloneOpenCliDocument, OpenCliDocument, parseOpenCliDocument } from "./openCli";
import { enrichDocumentFromXml } from "./xmlDoc";

export interface LoadedSource {
  document: OpenCliDocument;
  xmlDoc?: string;
  warnings: string[];
  options: ViewerOptions;
  features: FeatureFlags;
  label: string;
  commandPrefix?: string;
  mode: "inline" | "links" | "manual";
}

interface SourceIdentity {
  title?: string;
  commandPrefix?: string;
}

export async function loadFromStartupRequest(
  request: StartupRequest,
  signal?: AbortSignal,
): Promise<LoadedSource | null> {
  if (request.kind === "empty") {
    return null;
  }

  if (request.kind === "inline") {
    const doc = parseOpenCliDocument(JSON.stringify(request.openCli));
    const label = request.options.label || (doc?.info?.version ? `v${doc.info.version}` : "");
    return buildLoadedSource({
      document: doc,
      xmlDoc: request.xmlDoc,
      options: request.options,
      features: request.features,
      label,
      identity: {
        title: request.options.title,
        commandPrefix: request.options.commandPrefix,
      },
      mode: "inline",
    });
  }

  const openCliText = await fetchRequiredText(request.links.openCliUrl, signal);
  const xmlDocText = request.links.xmlDocUrl
    ? await fetchText(request.links.xmlDocUrl, request.links.xmlDocIsOptional, signal)
    : undefined;
  const identity = await resolveLinkedSourceIdentity(request.links.openCliUrl, signal, {
    title: request.options.title,
    commandPrefix: request.options.commandPrefix,
  });
  const resolvedOptions = {
    ...request.options,
    title: identity.title,
    commandPrefix: identity.commandPrefix,
  };

  return buildLoadedSource({
    document: parseOpenCliDocument(openCliText),
    xmlDoc: xmlDocText,
    options: resolvedOptions,
    features: request.features,
    label: request.source === "bootstrap" ? "Injected links" : "URL parameters",
    identity,
    mode: "links",
  });
}

export async function loadFromFiles(files: File[], options: ViewerOptions, features: FeatureFlags): Promise<LoadedSource> {
  const validated = validateFiles(files);
  const openCliText = await validated.openCli.text();
  const xmlDocText = validated.xmlDoc ? await validated.xmlDoc.text() : undefined;

  return buildLoadedSource({
    document: parseOpenCliDocument(openCliText),
    xmlDoc: xmlDocText,
    options: clearSourceIdentity(options),
    features,
    label: "Manual import",
    identity: {},
    mode: "manual",
  });
}

export async function loadFromUrls(
  opencliUrl: string,
  xmldocUrl: string | undefined,
  options: ViewerOptions,
  label: string,
  features: FeatureFlags,
  identity?: SourceIdentity,
): Promise<LoadedSource> {
  const openCliText = await fetchRequiredText(opencliUrl);
  const xmlDocText = xmldocUrl
    ? await fetchText(xmldocUrl, true)
    : undefined;
  const resolvedIdentity = await resolveLinkedSourceIdentity(opencliUrl, undefined, {
    title: identity?.title ?? options.title,
    commandPrefix: identity?.commandPrefix ?? options.commandPrefix,
  });

  return buildLoadedSource({
    document: parseOpenCliDocument(openCliText),
    xmlDoc: xmlDocText,
    options: {
      ...options,
      title: resolvedIdentity.title,
      commandPrefix: resolvedIdentity.commandPrefix,
    },
    features,
    label,
    identity: resolvedIdentity,
    mode: "links",
  });
}

export function validateFiles(files: File[]): { openCli: File; xmlDoc?: File } {
  if (files.length === 0) {
    throw new Error("Choose opencli.json, with optional xmldoc.xml.");
  }

  if (files.length > 2) {
    throw new Error("Import accepts one or two files: opencli.json and optional xmldoc.xml.");
  }

  let openCli: File | undefined;
  let xmlDoc: File | undefined;

  for (const file of files) {
    const name = file.name.toLowerCase();
    if (name === "opencli.json") {
      openCli = file;
      continue;
    }

    if (name === "xmldoc.xml") {
      xmlDoc = file;
      continue;
    }

    throw new Error(`Unsupported file "${file.name}". Use opencli.json and optional xmldoc.xml.`);
  }

  if (!openCli) {
    throw new Error("opencli.json is required.");
  }

  return { openCli, xmlDoc };
}

function buildLoadedSource(params: {
  document: OpenCliDocument;
  xmlDoc?: string;
  options: ViewerOptions;
  features: FeatureFlags;
  label: string;
  identity: SourceIdentity;
  mode: LoadedSource["mode"];
}): LoadedSource {
  const document = cloneOpenCliDocument(params.document);
  const warnings: string[] = [];

  applyTitleOverride(document, params.identity.title);

  if (params.xmlDoc) {
    const enrichment = enrichDocumentFromXml(document, params.xmlDoc);
    warnings.push(...enrichment.warnings);
  }

  return {
    document,
    xmlDoc: params.xmlDoc,
    warnings,
    options: params.options,
    features: params.features,
    label: params.label,
    commandPrefix: params.identity.commandPrefix,
    mode: params.mode,
  };
}

function applyTitleOverride(document: OpenCliDocument, title: string | undefined) {
  const trimmedTitle = title?.trim();
  if (!trimmedTitle) {
    return;
  }

  document.info = {
    ...document.info,
    title: trimmedTitle,
  };
}

async function fetchRequiredText(url: string, signal?: AbortSignal): Promise<string> {
  const response = await fetch(url, { signal });
  if (!response.ok) {
    throw new Error(`Failed to load ${url}: ${response.status} ${response.statusText}`);
  }

  return response.text();
}

async function fetchText(url: string, optional: boolean, signal?: AbortSignal): Promise<string | undefined> {
  try {
    return await fetchRequiredText(url, signal);
  } catch (error) {
    if (error instanceof DOMException && error.name === "AbortError") {
      throw error;
    }

    if (optional) {
      return undefined;
    }

    throw error;
  }
}

async function resolveLinkedSourceIdentity(
  openCliUrl: string,
  signal: AbortSignal | undefined,
  preferred: SourceIdentity,
): Promise<SourceIdentity> {
  if (preferred.title?.trim() && preferred.commandPrefix?.trim()) {
    return normalizeIdentity(preferred);
  }

  const metadataUrl = buildSiblingMetadataUrl(openCliUrl);
  if (!metadataUrl) {
    return normalizeIdentity(preferred);
  }

  const metadataText = await fetchText(metadataUrl, true, signal);
  if (!metadataText) {
    return normalizeIdentity(preferred);
  }

  return normalizeIdentity({
    title: preferred.title ?? readMetadataString(metadataText, "packageId"),
    commandPrefix: preferred.commandPrefix ?? readMetadataString(metadataText, "command"),
  });
}

function buildSiblingMetadataUrl(openCliUrl: string): string | undefined {
  try {
    const url = new URL(openCliUrl);
    if (!url.pathname.toLowerCase().endsWith("/opencli.json")) {
      return undefined;
    }

    url.pathname = url.pathname.slice(0, -"/opencli.json".length) + "/metadata.json";
    return url.toString();
  } catch {
    return undefined;
  }
}

function readMetadataString(metadataText: string, key: "packageId" | "command"): string | undefined {
  try {
    const parsed = JSON.parse(metadataText) as Record<string, unknown>;
    const value = parsed[key];
    return typeof value === "string" ? value : undefined;
  } catch {
    return undefined;
  }
}

function normalizeIdentity(identity: SourceIdentity): SourceIdentity {
  return {
    title: normalizeOverride(identity.title),
    commandPrefix: normalizeOverride(identity.commandPrefix),
  };
}

function clearSourceIdentity(options: ViewerOptions): ViewerOptions {
  return {
    ...options,
    title: undefined,
    commandPrefix: undefined,
  };
}

function normalizeOverride(value: string | undefined): string | undefined {
  const trimmed = value?.trim();
  return trimmed ? trimmed : undefined;
}
