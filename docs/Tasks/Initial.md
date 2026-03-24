  # OpenCLI Renderer v1

  ## Status

  - Historical planning note for the initial implementation.
  - The current HTML renderer no longer follows the single-file or tree-page model described below.
  - HTML now emits an InSpectreUI app bundle directory, requires `--out-dir`, rejects `--out` and `--layout`, and reports layout `app`.
  - Use `README.md` and `docs/opencli-renderer/` for the current public contract.

  ## Summary

  - Build a new local-only .NET 10 Spectre.Console.Cli tool that renders documentation from OpenCLI exports, with Markdown and HTML shipped on top of the same normalized pipeline.
  - Support two explicit source modes for both formats: render file <FORMAT> <OPENCLI_JSON> for saved exports and render exec <FORMAT> <SOURCE> for live CLIs that expose cli opencli; optional XML
    enrichment is additive in both modes.
  - Treat OpenCLI JSON as the source of truth, validate it offline against an embedded snapshot of the published schema, then optionally merge structured XML data to fill missing prose.

  ## Command Surface

  - Group commands by source mode first so help stays short and flags stay relevant:
      - render file markdown <OPENCLI_JSON>
      - render file html <OPENCLI_JSON>
      - render exec markdown <SOURCE>
      - render exec html <SOURCE>
  - render file markdown/html flags:
      - --layout single|tree (single default)
      - --out <FILE> for single
      - --out-dir <DIR> for tree
      - --xmldoc <PATH> for optional XML enrichment
      - --include-hidden, --include-metadata, --overwrite
  - render exec markdown/html flags:
      - --layout single|tree, --out <FILE>, --out-dir <DIR>, --include-hidden, --include-metadata, --overwrite
      - --source-arg <ARG> repeatable; inserted before the export tail so wrappers like dotnet run --project ... -- work
      - --opencli-arg <ARG> repeatable; when present it replaces the default export tail cli opencli
      - --with-xmldoc to call the default XML tail cli xmldoc
      - --xmldoc-arg <ARG> repeatable; when present it replaces the default XML tail and enables XML enrichment
      - --cwd <PATH> for the source process working directory
      - --timeout <SECONDS> (30 default)
  - Shared globals:
      - --json / --output json
      - --output human
      - --quiet, --verbose, --no-color, --dry-run
      - --help, --version
  - Source executable resolution for render exec:
      - rooted/full path if provided
      - exact file if present in cwd
      - bare token via PATH + PATHEXT lookup (cmd, cmd.exe, full path all work)
      - otherwise exit 3 with an actionable “not found” error
  - Precedence:
      - flags > env > defaults
      - envs: OPENCLI_RENDERER_OUTPUT, OPENCLI_RENDERER_VERBOSE, OPENCLI_RENDERER_QUIET, OPENCLI_RENDERER_TIMEOUT, NO_COLOR
      - no config file/profile layer in v1

  ## Implementation

  - Use a single normalized pipeline: load source → validate schema → normalize model → optionally enrich from XML → render layout → write/report results.
  - Normalize the full OpenCLI document object, not just commands:
      - info, conventions, root arguments, root options, commands, examples, exitCodes, metadata
      - compute declared vs inherited recursive options separately so child docs can show inherited flags without duplicating source data
      - parse xmldoc.xml as a second structured command tree, not as raw .NET member docs
      - match commands by command path, then parameters by long/short name; use ClrType/Settings attributes as tie-breakers
      - fill missing descriptions and appendix-only technical details
      - never overwrite non-empty OpenCLI JSON descriptions or alter public command names/order
  - Markdown and HTML layouts:
      - single: one document with title/version, summary/description when present, conventions/contact/license when present, TOC, branch/command sections, subcommand lists, argument/option tables,
        examples, exit codes, and optional metadata appendix
      - tree: format-specific index pages plus nested pages mirroring the command tree (`index.md` / `index.html`, `auth/index.md` / `auth/index.html`, etc.) with relative links and slug-safe file
        names
      - option/argument tables include name, aliases, value shape/arity, required, accepted values, group, description, and inherited marker
      - HTML uses a viewer-inspired shell based on `assets/poc/Viewer.jsx`, including a persistent sidebar, search box, cards, badges, and standalone styling
  - Overwrite/non-interactive policy:
      - no interactive prompts in v1
      - existing output file or non-empty output dir refuses with exit 2 unless --overwrite
      - --dry-run prints the resolved source, merge summary, and write plan and performs no writes
      - render exec runs non-interactively with stdin closed; child stderr is captured and surfaced only as diagnostic context on failure
          - single + no --out writes Markdown body to stdout
          - otherwise writes files and prints a concise summary to stdout
          - warnings/diagnostics go to stderr
          - --quiet suppresses summaries/diagnostics, never the document body
      - JSON mode:
          - never emits rendered document bodies on stdout
          - requires --out or --out-dir
          - returns envelope v1 on stdout: ok, data, error, meta.schemaVersion
          - data includes format, layout, source, output, stats, and structured warnings
          - expected failures use ok: false with stable error.kind values such as usage, validation, source_exec, overwrite_refused
  - Exit codes:
      - 0 success
      - 2 usage/refusal/invalid combination/overwrite refusal
      - 3 source executable resolution or child-process failure
      - 4 parse/schema/merge validation failure
      - 1 unexpected internal failure
      - 10 reserved for future explicit user cancellation

  ## Test Plan

  - Help contract: root help, render, render file markdown/html, and render exec markdown/html all show the intended branch grouping, examples, output rules, and overwrite behavior.
  - File source happy path: render file markdown assets/testfiles/opencli.json:1 emits a single Markdown document to stdout, preserves command order, and omits metadata/hidden items by default.
  - Tree + XML enrichment: render file markdown ... --layout tree --out-dir <tmp> --xmldoc assets/testfiles/xmldoc.xml:1 --include-metadata creates index.md plus nested docs, fills missing prose from
    XML, and confines CLR/Settings details to the appendix.
  - HTML happy path: render file html assets/testfiles/opencli.json:1 emits a standalone viewer-style HTML page or tree with linked `.html` pages, sidebar filtering, command cards, and metadata
    appendices when enabled.
  - Exec source behavior: a fixture CLI exposing cli opencli and cli xmldoc succeeds with bare executable name, explicit .exe, and full path; default export tails and --source-arg / --opencli-arg /
    --xmldoc-arg overrides behave as documented.
  - Automation/destructive checks: --json without an explicit output destination fails with exit 2; --json with an output destination returns a valid v1 envelope; existing targets refuse without
    --overwrite; --dry-run writes nothing; malformed OpenCLI JSON exits 4 with schema guidance.

  ## Assumptions & Checklist

  - Assumptions/defaults:
      - Markdown and HTML ship in this slice; both formats use the same source/load/normalize pipeline and the HTML shell is visually guided by `assets/poc/Viewer.jsx`.
      - The tool stays stateless in v1; env vars are the only non-CLI override layer.
      - The schema contract comes from the published OpenCLI spec (https://opencli.org/) and an embedded snapshot of the published draft schema (https://opencli.org/draft.json); current fixtures remain
        assets/testfiles/opencli.json:1 and assets/testfiles/xmldoc.xml:1.
  - [x] CLI classification is stated: local-only.
  - [x] Command tree is proposed and justified in user-facing terms.
  - [x] Flag/env/config/default precedence is explicit.
  - [x] Output contract is explicit: human-first default, opt-in/versioned --json, no JSON-by-default commands, and no list-style public commands in v1 that would require a separate table branch.
  - [x] Output regression checks are included for both default human output and --json behavior.
  - [x] Non-interactive behavior is defined, including stdin rules, --quiet, and --dry-run.
  - [x] Destructive action policy is defined, including overwrite refusal and explicit opt-in.
  - [x] Exit codes and machine-mode error strategy are defined.
  - [x] Validation checks cover help, source resolution, non-interactive behavior, destructive flows, and the JSON contract.

# Tasks
- [x] Bootstrap `.NET 10` Spectre CLI solution
- [x] Implement source loading pipeline
- [x] Implement Markdown rendering
- [x] Add HTML rendering
- [x] Wire Spectre commands and output contracts
- [x] Add focused validation tests
