export interface OpenCliDocument {
  opencli: string;
  info: OpenCliInfo;
  conventions?: OpenCliConventions;
  arguments: OpenCliArgument[];
  options: OpenCliOption[];
  commands: OpenCliCommand[];
  exitCodes: OpenCliExitCode[];
  examples: string[];
  interactive: boolean;
  metadata: OpenCliMetadata[];
}

export interface OpenCliInfo {
  title: string;
  summary?: string | null;
  description?: string | null;
  contact?: OpenCliContact;
  license?: OpenCliLicense;
  version: string;
}

export interface OpenCliConventions {
  groupOptions?: boolean | null;
  optionSeparator?: string | null;
}

export interface OpenCliContact {
  name?: string | null;
  url?: string | null;
  email?: string | null;
}

export interface OpenCliLicense {
  name?: string | null;
  identifier?: string | null;
  url?: string | null;
}

export interface OpenCliCommand {
  name: string;
  aliases: string[];
  options: OpenCliOption[];
  arguments: OpenCliArgument[];
  commands: OpenCliCommand[];
  exitCodes: OpenCliExitCode[];
  description?: string | null;
  hidden: boolean;
  examples: string[];
  interactive: boolean;
  metadata: OpenCliMetadata[];
}

export interface OpenCliOption {
  name: string;
  required: boolean;
  aliases: string[];
  arguments: OpenCliArgument[];
  group?: string | null;
  description?: string | null;
  recursive: boolean;
  hidden: boolean;
  metadata: OpenCliMetadata[];
}

export interface OpenCliArgument {
  name: string;
  required: boolean;
  arity?: OpenCliArity;
  acceptedValues: string[];
  group?: string | null;
  description?: string | null;
  hidden: boolean;
  metadata: OpenCliMetadata[];
}

export interface OpenCliArity {
  minimum?: number | null;
  maximum?: number | null;
}

export interface OpenCliExitCode {
  code: number;
  description?: string | null;
}

export interface OpenCliMetadata {
  name: string;
  value?: unknown;
}

export function parseOpenCliDocument(json: string): OpenCliDocument {
  let parsed: unknown;

  try {
    parsed = JSON.parse(json);
  } catch (error) {
    throw new Error(`OpenCLI JSON is not valid: ${toMessage(error)}`);
  }

  return coerceDocument(parsed);
}

export function cloneOpenCliDocument(document: OpenCliDocument): OpenCliDocument {
  return structuredClone(document);
}

export function isBlank(value: string | null | undefined): boolean {
  return value === undefined || value === null || value.trim().length === 0;
}

function coerceDocument(value: unknown): OpenCliDocument {
  if (!isRecord(value)) {
    throw new Error("OpenCLI JSON must contain an object at the root.");
  }

  return {
    opencli: readString(value.opencli),
    info: coerceInfo(value.info),
    conventions: isRecord(value.conventions) ? coerceConventions(value.conventions) : undefined,
    arguments: readArray(value.arguments).map(coerceArgument),
    options: readArray(value.options).map(coerceOption),
    commands: readArray(value.commands).map(coerceCommand),
    exitCodes: readArray(value.exitCodes).map(coerceExitCode),
    examples: readArray(value.examples).map(readString),
    interactive: readBoolean(value.interactive),
    metadata: readArray(value.metadata).map(coerceMetadata),
  };
}

function coerceInfo(value: unknown): OpenCliInfo {
  const record = isRecord(value) ? value : {};
  return {
    title: readString(record.title),
    summary: readOptionalString(record.summary),
    description: readOptionalString(record.description),
    contact: isRecord(record.contact) ? coerceContact(record.contact) : undefined,
    license: isRecord(record.license) ? coerceLicense(record.license) : undefined,
    version: readString(record.version),
  };
}

function coerceConventions(value: Record<string, unknown>): OpenCliConventions {
  return {
    groupOptions: typeof value.groupOptions === "boolean" ? value.groupOptions : undefined,
    optionSeparator: readOptionalString(value.optionSeparator),
  };
}

function coerceContact(value: Record<string, unknown>): OpenCliContact {
  return {
    name: readOptionalString(value.name),
    url: readOptionalString(value.url),
    email: readOptionalString(value.email),
  };
}

function coerceLicense(value: Record<string, unknown>): OpenCliLicense {
  return {
    name: readOptionalString(value.name),
    identifier: readOptionalString(value.identifier),
    url: readOptionalString(value.url),
  };
}

function coerceCommand(value: unknown): OpenCliCommand {
  if (!isRecord(value)) {
    throw new Error("OpenCLI commands must be objects.");
  }

  return {
    name: readString(value.name),
    aliases: readStringArray(value.aliases),
    options: readArray(value.options).map(coerceOption),
    arguments: readArray(value.arguments).map(coerceArgument),
    commands: readArray(value.commands).map(coerceCommand),
    exitCodes: readArray(value.exitCodes).map(coerceExitCode),
    description: readOptionalString(value.description),
    hidden: readBoolean(value.hidden),
    examples: readStringArray(value.examples),
    interactive: readBoolean(value.interactive),
    metadata: readArray(value.metadata).map(coerceMetadata),
  };
}

function coerceOption(value: unknown): OpenCliOption {
  if (!isRecord(value)) {
    throw new Error("OpenCLI options must be objects.");
  }

  return {
    name: readString(value.name),
    required: readBoolean(value.required),
    aliases: readStringArray(value.aliases),
    arguments: readArray(value.arguments).map(coerceArgument),
    group: readOptionalString(value.group),
    description: readOptionalString(value.description),
    recursive: readBoolean(value.recursive),
    hidden: readBoolean(value.hidden),
    metadata: readArray(value.metadata).map(coerceMetadata),
  };
}

function coerceArgument(value: unknown): OpenCliArgument {
  if (!isRecord(value)) {
    throw new Error("OpenCLI arguments must be objects.");
  }

  return {
    name: readString(value.name),
    required: readBoolean(value.required),
    arity: isRecord(value.arity) ? coerceArity(value.arity) : undefined,
    acceptedValues: readStringArray(value.acceptedValues),
    group: readOptionalString(value.group),
    description: readOptionalString(value.description),
    hidden: readBoolean(value.hidden),
    metadata: readArray(value.metadata).map(coerceMetadata),
  };
}

function coerceArity(value: Record<string, unknown>): OpenCliArity {
  return {
    minimum: readOptionalNumber(value.minimum),
    maximum: readOptionalNumber(value.maximum),
  };
}

function coerceExitCode(value: unknown): OpenCliExitCode {
  if (!isRecord(value)) {
    throw new Error("OpenCLI exit codes must be objects.");
  }

  return {
    code: readNumber(value.code),
    description: readOptionalString(value.description),
  };
}

function coerceMetadata(value: unknown): OpenCliMetadata {
  if (!isRecord(value)) {
    throw new Error("OpenCLI metadata entries must be objects.");
  }

  return {
    name: readString(value.name),
    value: value.value,
  };
}

function readArray(value: unknown): unknown[] {
  return Array.isArray(value) ? value : [];
}

function readStringArray(value: unknown): string[] {
  if (typeof value === "string") {
    return [value];
  }

  return readArray(value)
    .filter((item): item is string => typeof item === "string");
}

function readString(value: unknown): string {
  return typeof value === "string" ? value : "";
}

function readOptionalString(value: unknown): string | null | undefined {
  return typeof value === "string" ? value : undefined;
}

function readBoolean(value: unknown): boolean {
  return value === true;
}

function readNumber(value: unknown): number {
  return typeof value === "number" ? value : 0;
}

function readOptionalNumber(value: unknown): number | undefined {
  return typeof value === "number" ? value : undefined;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function toMessage(error: unknown): string {
  return error instanceof Error ? error.message : "Unknown error.";
}
