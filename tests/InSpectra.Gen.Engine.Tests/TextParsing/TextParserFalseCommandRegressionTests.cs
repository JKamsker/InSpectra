namespace InSpectra.Gen.Engine.Tests.TextParsing;

using InSpectra.Gen.Engine.Modes.Help.Parsing;

public sealed class TextParserFalseCommandRegressionTests
{
    [Fact]
    public void UnoDevServer_Non_Standard_Section_Headers_Do_Not_Produce_Phantom_Commands()
    {
        var parser = new TextParser();

        var document = parser.Parse(
            """
            Usage: dnx -y uno.devserver [options] [command]

            Global options:
              --help, -h                       Show this help message and exit
              --log-level, -l <level>          Set the log level (Trace, Debug, Information, Warning, Error, Critical, None). Default is Information.
              --file-log, -fl <path>           Enable file logging to the provided file path (supports {Date} token). Required path argument.
              --solution-dir <path>            Explicit solution root when priming tools without client-provided roots

            Disco options:
              --json                           Emit JSON output
              --addins-only                    Output only resolved add-in paths (semicolon-separated, or JSON array with --json)

            MCP options:
              --mcp-app                        Start in App MCP STDIO mode
              --mcp-wait-tools-list            Wait for upstream server tools before responding to list_tools (MCP mode only)
              --force-roots-fallback           This mode can be used when the MCP client does not support the roots feature
              --force-generate-tool-cache      Deprecated (no-op). Kept for backward compatibility.

            Commands:
              start            Start the DevServer for the current folder
              stop             Stop the DevServer for the current folder
              list             List active DevServer instances
              disco            Discover environment and SDK details
              health           Report Uno DevServer health for the current workspace

            MCP setup commands:
              mcp serve        Start the MCP STDIO proxy (alias: --mcp-app)
              mcp status       Report installation state of MCP servers across clients
              mcp install      Register MCP servers in client config files
              mcp uninstall    Remove MCP servers from client config files

            MCP setup options:
              <client>                         Target client (positional, or use --all-ides): copilot-vscode, copilot-vs, copilot-cli, cursor, windsurf, kiro, gemini-antigravity, gemini-cli, junie-rider, claude-code, claude-desktop, codex-cli, jetbrains-air, opencode, unknown
              --workspace <path>               Workspace root (default: current directory)
              --channel <stable|prerelease>    Select the Uno MCP definition channel
              --tool-version <ver>             Pin the Uno MCP tool definition to a specific version
              --servers <list>                 Comma-separated server names (default: all)
              --all-scopes                     For mcp uninstall, remove matching registrations from every configured scope
              --all-ides                       For mcp install/uninstall without <client>, target all detected clients
              --dry-run                        Show what would be done without modifying any files
              --json                           Emit JSON output
              --ide-definitions <path>         Override embedded MCP client profiles
              --server-definitions <path>      Override embedded server definitions

            Note: MCP setup options choose the expected Uno MCP definition.
                  Any dnx --prerelease or dnx --version written to config files is derived output.
            """);

        // Real commands: start, stop, list, disco, health, mcp (with subcommands serve/status/install/uninstall)
        Assert.True(document.Commands.Count <= 10,
            $"Expected <=10 real commands, got {document.Commands.Count}: [{string.Join(", ", document.Commands.Select(c => c.Key))}]");
        Assert.Contains(document.Commands, c => c.Key == "start");
        Assert.Contains(document.Commands, c => c.Key == "stop");
        Assert.Contains(document.Commands, c => c.Key == "disco");
        Assert.Contains(document.Commands, c => c.Key == "health");
    }

    [Fact]
    public void DotnetSkills_Table_Format_With_Box_Drawing_Does_Not_Produce_Phantom_Commands()
    {
        var parser = new TextParser();

        var document = parser.Parse(
            """
            dotnet skills v0.0.53
            .NET skill catalog for AI-assisted development


            Getting started
            dotnet skills                                      Launch the interactive
                                                               shell
            dotnet skills help                                 This command reference
            dotnet skills version                              Version and update check

            Catalog
            dotnet skills list                                 Inventory with scope
                                                               comparison
            dotnet skills package list                         Curated skill stacks
            dotnet skills recommend                            Scan .csproj and propose
                                                               skills

            Install
            dotnet skills install aspire orleans               Install by alias
            dotnet skills install --auto                       Auto-install from project
                                                               signals
            dotnet skills install --auto --prune               Reconcile stale
                                                               auto-managed skills
            dotnet skills install package ai                   Install a multi-skill
                                                               skill stack
            dotnet skills remove --all                         Remove all installed
                                                               skills
            dotnet skills update                               Update to latest catalog
                                                               version
            dotnet skills sync --force                         Refresh cached catalog
            dotnet skills where                                Print resolved install
                                                               path

            Agents
            dotnet skills agent list                           List orchestration agents
            dotnet skills agent install router ai              Install agents by name
            dotnet skills agent install --all --auto           All agents to detected
                                                               platforms
            dotnet skills agent install router --target /path  Explicit target path

            ╭─notes────────────────────────────────────────────────────────────────────────╮
            │ Bare dotnet skills opens the interactive shell.                              │
            │ Short aliases work everywhere: aspire resolves to dotnet-aspire.             │
            │ --bundled skips the network. --catalog-version pins a release. --refresh     │
            │ redownloads.                                                                 │
            │ Auto-detect probes .codex, .claude, .github, .gemini, .junie; falls back to  │
            │ .agents/skills.                                                              │
            │ Set DOTNET_SKILLS_SKIP_UPDATE_CHECK=1 to suppress update notices.            │
            ╰──────────────────────────────────────────────────────────────────────────────╯
            """);

        // Box-drawing notes section should not produce commands
        Assert.DoesNotContain(document.Commands, c => c.Key.Contains("notes"));
        Assert.DoesNotContain(document.Commands, c => c.Key.Contains("Bare"));
        Assert.DoesNotContain(document.Commands, c => c.Key.Contains("Short"));
        // The parser should not explode the inline examples into dozens of phantom commands
        Assert.True(document.Commands.Count <= 15,
            $"Expected <=15 commands from dotnet-skills help, got {document.Commands.Count}: [{string.Join(", ", document.Commands.Select(c => c.Key))}]");
    }

    [Fact]
    public void SimpleAcme_Flat_Option_List_Does_Not_Infer_Commands()
    {
        var parser = new TextParser();

        var document = parser.Parse(
            """
             A simple cross platform ACME client (WACS)
             Software version 2.3.6.2257 (release, pluggable, dotnet, 64-bit)

             ---------------------
             Main
             ---------------------

                 --baseuri
              Address of the ACME server to use. The default endpoint can be modified in
              settings.json.

                 --test
              Enables testing behaviours in the program which may help with troubleshooting.

                 --verbose
              Print additional log messages to console for troubleshooting and bug reports.

                 --help
              Show information about all available command line options.

                 --version
              Show version information.

                 --renew
              Renew any certificates that are due. This argument is used by the scheduled
              task.

                 --force
              Always execute the renewal, disregarding the validity of the current
              certificates and the prefered schedule.

                 --list
              List all created renewals in unattended mode.

                 --cancel
              Cancel renewal specified by the --friendlyname or --id arguments.
            """);

        // "Main" appears between dashed separators and is a section header, not a command.
        // The parser now recognizes bare words preceded by decorative banner lines as section
        // headers and skips them.
        Assert.True(document.Commands.Count == 0,
            $"Expected 0 commands (section headers between dashed separators should not be inferred as commands), got {document.Commands.Count}: [{string.Join(", ", document.Commands.Select(c => c.Key))}]");
        Assert.True(document.Options.Count > 0, "Should parse options from the flat list");
    }

    [Fact]
    public void MultiagentSetup_Nested_Options_Under_Commands_Do_Not_Produce_Phantom_Commands()
    {
        var parser = new TextParser();

        var document = parser.Parse(
            """
            Usage: multiagent-setup <command> [options]

            Commands:
              new <project-name> [github-org]    Create a new multi-agent workspace
                --provider <name>                 Provider: claude (default), or:
                                                   claude, nessy, codex, qwen, cursor, windsurf, copilot, gemini, cline, aider, continue, roo, all
              init [dir]                          Inject workspace files into an existing git repo
                dir                               Target directory (default: current directory)
                --provider <name>                 Provider: claude (default), or:
                                                   claude, nessy, codex, qwen, cursor, windsurf, copilot, gemini, cline, aider, continue, roo, all
                --force                           Overwrite existing files
              add-provider <name>                 Add a provider to an existing workspace
                <name>: nessy, codex, qwen, cursor, windsurf, copilot, gemini, cline, aider, continue, roo, all
                --force                           Overwrite existing provider config
              update                              Update workspace templates to latest version
                --force                           Overwrite all files (CLAUDE.md preserved by default)
              sync-roles [--clone|--pull]         Sync agent roles to ~/.claude/commands/
                --agency-dir <path>               Override agency-agents directory
              install-mcps [options]              Install age-mcp and o-brien MCP servers
                --docker                          Use local Docker (default, interactive)
                --manual                          Enter connection strings manually
              hook <name>                         Run a hook (cross-platform)
                block-dangerous | enforce-commit-msg | auto-lint | log-agent | stop-guard
              doctor                              Check workspace health (tools, files, hooks)

            Options:
              -h, --help                          Show this help
              -v, --version                       Show version
            """);

        // Real commands: new, init, add-provider, update, sync-roles, install-mcps, hook, doctor
        Assert.True(document.Commands.Count <= 10,
            $"Expected <=10 commands, got {document.Commands.Count}: [{string.Join(", ", document.Commands.Select(c => c.Key))}]");
        Assert.Contains(document.Commands, c => c.Key == "new");
        Assert.Contains(document.Commands, c => c.Key == "init");
        Assert.Contains(document.Commands, c => c.Key == "update");
        Assert.Contains(document.Commands, c => c.Key == "doctor");
        // Indented --provider, --force etc. should NOT be commands
        Assert.DoesNotContain(document.Commands, c => c.Key.StartsWith("--"));
    }
}
