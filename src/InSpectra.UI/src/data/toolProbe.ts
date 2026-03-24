import { OpenCliDocument } from "./openCli";

export interface ProbePackageSummary {
  id: string;
  version: string;
  isDotnetTool: boolean;
  isSpectreCli: boolean;
  commandName?: string;
  runner?: string;
  entryPoint?: string;
  targetFramework?: string;
  hasPackagedOpenCli: boolean;
  confidence: string;
}

export interface ProbePackageResult {
  status: string;
  confidence: string;
  error?: string;
  warnings: string[];
  package?: {
    id: string;
    version: string;
    isDotnetTool: boolean;
    isSpectreCli: boolean;
    commandName?: string;
    runner?: string;
    entryPoint?: string;
    targetFramework?: string;
    hasPackagedOpenCli: boolean;
  };
  document?: OpenCliDocument;
}

interface ProbeModule {
  analyzePackage(base64Package: string): Promise<string> | string;
}

let probeModulePromise: Promise<ProbeModule> | null = null;

export async function probePackage(packageBytes: Uint8Array): Promise<ProbePackageResult> {
  const module = await loadProbeModule();
  const raw = await module.analyzePackage(bytesToBase64(packageBytes));
  const result = JSON.parse(raw) as ProbePackageResult;
  return {
    ...result,
    warnings: result.warnings ?? [],
  };
}

export function toProbeSummary(result: ProbePackageResult): ProbePackageSummary | undefined {
  if (!result.package) {
    return undefined;
  }

  return {
    ...result.package,
    confidence: result.confidence,
  };
}

async function loadProbeModule(): Promise<ProbeModule> {
  if (!probeModulePromise) {
    const url = new URL("../probe/main.js", import.meta.url).toString();
    probeModulePromise = import(/* @vite-ignore */ url) as Promise<ProbeModule>;
  }

  return probeModulePromise;
}

function bytesToBase64(bytes: Uint8Array): string {
  let encoded = "";
  for (let index = 0; index < bytes.length; index += 0x8000) {
    const slice = bytes.subarray(index, Math.min(index + 0x8000, bytes.length));
    encoded += String.fromCharCode(...slice);
  }

  return btoa(encoded);
}
