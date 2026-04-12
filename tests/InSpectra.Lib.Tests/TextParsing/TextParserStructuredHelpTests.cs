namespace InSpectra.Lib.Tests.TextParsing;

using InSpectra.Lib.Modes.Help.Parsing;

public sealed class TextParserStructuredHelpTests
{
    [Fact]
    public void Parses_CliFx_Style_Help_Text()
    {
        var parser = new TextParser();

        var document = parser.Parse(
            """
            husky v0.9.1

            USAGE
              husky [options]
              husky [command] [...]

            OPTIONS
              -h|--help         Shows help text.
              --version         Shows version information.

            COMMANDS
              add               Add husky hook
              install           Install Husky hooks
            """);

        Assert.Equal("husky", document.Title);
        Assert.Equal("v0.9.1", document.Version);
        Assert.Equal(2, document.UsageLines.Count);
        Assert.Contains(document.Options, option => option.Key == "-h|--help");
        Assert.Contains(document.Commands, command => command.Key == "add");
        Assert.Contains(document.Commands, command => command.Key == "install");
    }

    [Fact]
    public void Extracts_CommandScoped_Sections_From_Single_Page_Command_Help()
    {
        var parser = new TextParser();

        var document = parser.Parse(
            """
            SqlDatabase Command Line Tools (v6.0.2)
            Project on https://github.com/Apps72/Dev.Data
            Usage: DbCmd <command> [options]

            Commands:
              GenerateEntities   | ge     Generate a file with entities.
              Merge              | mg     Merge all script files.
              Run                | rn     Run all script files.

            'GenerateEntities' options:
              --ConnectionString | -cs    Required. Connection string to the database server.
              --Output           | -o     File name where class will be written.

            'Merge' options:
              --Source           | -s     Source directory pattern containing all files to merge.

            Example:
              DbCmd GenerateEntities -cs="Server=localhost;Database=Scott;" -o=Entities.cs
            """);

        Assert.Empty(document.Options);
        Assert.Contains(document.Commands, command => command.Key == "GenerateEntities");
        Assert.Contains(document.Commands, command => command.Key == "Merge");
        Assert.DoesNotContain(document.Commands, command => command.Key == "Example");

        Assert.True(document.EmbeddedCommandDocuments.ContainsKey("GenerateEntities"));
        var generateEntities = document.EmbeddedCommandDocuments["GenerateEntities"];
        Assert.Contains(generateEntities.Options, option => option.Key == "--ConnectionString | -cs");
        Assert.Contains(generateEntities.Options, option => option.Key == "--Output | -o");

        Assert.True(document.EmbeddedCommandDocuments.ContainsKey("Merge"));
        var merge = document.EmbeddedCommandDocuments["Merge"];
        Assert.Contains(merge.Options, option => option.Key == "--Source | -s");
    }

    [Fact]
    public void Infers_Commands_From_Multiline_Usage_Inventories_Without_A_Commands_Section()
    {
        var parser = new TextParser();

        var document = parser.Parse(
            """
            mcpdebugger - AI-controlled cooperative debugger via MCP

            Usage:
              mcpdebugger serve [--port <port>]   Start the HTTP debug server (default port: 5200)
              mcpdebugger mcp [--port <port>]     Start the MCP server (talks to debug server)
              mcpdebugger --help                  Show this help message

            Typical usage:
              1. Run 'mcpdebugger serve' in one terminal
            """);

        var commandKeys = document.Commands
            .Select(command => command.Key)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["mcp", "serve"], commandKeys);
        Assert.Contains(document.Commands, command =>
            command.Key == "serve"
            && command.Description == "Start the HTTP debug server (default port: 5200)");
        Assert.Contains(
            "mcpdebugger serve [--port <port>]   Start the HTTP debug server (default port: 5200)",
            document.UsageLines);
    }

    [Fact]
    public void Parses_Localized_Section_Headers()
    {
        var parser = new TextParser();

        var document = parser.Parse(
            """
            VERWENDUNG:
                dotnet cake [SCRIPT] [OPTIONEN]

            ARGUMENTE:
                [SCRIPT]    The Cake script. Defaults to build.cake

            OPTIONEN:
                -v, --verbosity <VERBOSITY>  Specifies the amount of information to be displayed.
            """);

        Assert.Contains("dotnet cake [SCRIPT] [OPTIONEN]", document.UsageLines);
        Assert.Single(document.Arguments);
        Assert.Equal("SCRIPT", document.Arguments[0].Key);
        Assert.Single(document.Options);
        Assert.Equal("-v, --verbosity <VERBOSITY>", document.Options[0].Key);
    }
}
