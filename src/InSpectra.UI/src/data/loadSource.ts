import { ViewerOptions } from "../boot/contracts";
import { StartupRequest } from "../boot/bootstrap";
import { cloneOpenCliDocument, OpenCliDocument, parseOpenCliDocument } from "./openCli";
import { enrichDocumentFromXml } from "./xmlDoc";

export interface LoadedSource {
  document: OpenCliDocument;
  xmlDoc?: string;
  warnings: string[];
  options: ViewerOptions;
  label: string;
  mode: "inline" | "links" | "manual";
}

export async function loadFromStartupRequest(
  request: StartupRequest,
  signal?: AbortSignal,
): Promise<LoadedSource | null> {
  if (request.kind === "empty") {
    return null;
  }

  if (request.kind === "inline") {
    return buildLoadedSource({
      document: parseOpenCliDocument(JSON.stringify(request.openCli)),
      xmlDoc: request.xmlDoc,
      options: request.options,
      label: "Injected bootstrap",
      mode: "inline",
    });
  }

  const openCliText = await fetchRequiredText(request.links.openCliUrl, signal);
  const xmlDocText = request.links.xmlDocUrl
    ? await fetchText(request.links.xmlDocUrl, request.links.xmlDocIsOptional, signal)
    : undefined;

  return buildLoadedSource({
    document: parseOpenCliDocument(openCliText),
    xmlDoc: xmlDocText,
    options: request.options,
    label: request.source === "bootstrap" ? "Injected links" : "URL parameters",
    mode: "links",
  });
}

export async function loadFromFiles(files: File[], options: ViewerOptions): Promise<LoadedSource> {
  const validated = validateFiles(files);
  const openCliText = await validated.openCli.text();
  const xmlDocText = validated.xmlDoc ? await validated.xmlDoc.text() : undefined;

  return buildLoadedSource({
    document: parseOpenCliDocument(openCliText),
    xmlDoc: xmlDocText,
    options,
    label: "Manual import",
    mode: "manual",
  });
}

export async function loadFromUrls(
  opencliUrl: string,
  xmldocUrl: string,
  options: ViewerOptions,
  label: string,
): Promise<LoadedSource> {
  const openCliText = await fetchRequiredText(opencliUrl);
  const xmlDocText = await fetchText(xmldocUrl, true);

  return buildLoadedSource({
    document: parseOpenCliDocument(openCliText),
    xmlDoc: xmlDocText,
    options,
    label,
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
  label: string;
  mode: LoadedSource["mode"];
}): LoadedSource {
  const document = cloneOpenCliDocument(params.document);
  const warnings: string[] = [];

  if (params.xmlDoc) {
    const enrichment = enrichDocumentFromXml(document, params.xmlDoc);
    warnings.push(...enrichment.warnings);
  }

  return {
    document,
    xmlDoc: params.xmlDoc,
    warnings,
    options: params.options,
    label: params.label,
    mode: params.mode,
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
