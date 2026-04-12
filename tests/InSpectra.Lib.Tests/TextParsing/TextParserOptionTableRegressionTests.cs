namespace InSpectra.Lib.Tests.TextParsing;

using InSpectra.Lib.Modes.Help.Parsing;

public sealed class TextParserOptionTableRegressionTests
{
    [Fact]
    public void Parses_Flags_Sections_As_Options_Instead_Of_Commands()
    {
        var parser = new TextParser();

        var document = parser.Parse(
            """
            EthisysCore CLI v0.0.0+build

            Usage:
              cc generate feature <name>

            Commands:
              generate feature  Scaffold a new plugin project

            Flags:
              --version, -v  Print the CLI version
              --internal     Use internal SDK references
            """);

        Assert.Contains(document.Commands, command => command.Key == "generate feature");
        Assert.DoesNotContain(document.Commands, command => command.Key == "Flags");
        Assert.Contains(document.Options, option => option.Key == "--version, -v");
        Assert.Contains(document.Options, option => option.Key == "--internal");
    }

    [Fact]
    public void Parses_Markdown_Option_Tables_Without_Inferring_Fake_Commands()
    {
        var parser = new TextParser();

        var document = parser.Parse(
            """
            RegisterBot Version 2.0.20.0

            ```RegisterBot [--endpoint endpoint] [--name botName] [--resource-group groupName] [--help]```

            Creates or updates a bot registration for [botName] pointing to [endpoint] with teams channel and SSO enabled.

            | Argument                         | Description                                                                                   |
            | -------------------------------- | --------------------------------------------------------------------------------------------- |
            | -e, --endpoint endpoint          | (optional) If not specified the endpoint will stay the same as project settings               |
            | -n, --name botName               | (optional) If not specified the botname will be pulled from settings or interactively asked   |
            | -g, --resource-group groupName   | (optional) If not specified the groupname will be pulled from settings or interactively asked |
            | -v, --verbose                    | (optional) show all commands as they are executed                                             |
            | -h, --help                       | display help                                                                                  |

            If the endpoint host name is:

            | Host                 | Action                                                                               |
            | -------------------- | ------------------------------------------------------------------------------------ |
            | xx.azurewebsites.net | it modifies the remote web app settings to have correct settings/secrets             |
            | localhost            | it modifies the local project settings/user secrets to have correct settings/secrets |
            """);

        Assert.Contains(
            document.UsageLines,
            line => line == "RegisterBot [--endpoint endpoint] [--name botName] [--resource-group groupName] [--help]");
        Assert.Contains(document.Options, option => option.Key == "-e, --endpoint <ENDPOINT>");
        Assert.Contains(document.Options, option => option.Key == "-n, --name <BOTNAME>");
        Assert.Contains(document.Options, option => option.Key == "-g, --resource-group <GROUPNAME>");
        Assert.Contains(document.Options, option => option.Key == "-v, --verbose");
        Assert.Contains(document.Options, option => option.Key == "-h, --help");
        Assert.Empty(document.Commands);
    }

    [Fact]
    public void Parses_Box_Drawing_Option_Tables_Without_Inferring_Fake_Commands()
    {
        var parser = new TextParser();

        var document = parser.Parse(
            """
            dotnet-repl

             dotnet-repl [options]

            ┌───────────────────────┬───────────────────────────────┐
            │ Option                │ Description                   │
            ├───────────────────────┼───────────────────────────────┤
            │ -h, -?, --help        │ Show help and usage           │
            │ --log-path <PATH>     │ Enable file logging           │
            │                       │ to the specified directory    │
            └───────────────────────┴───────────────────────────────┘
            """);

        Assert.Contains(document.Options, option => option.Key == "-h, -?, --help");
        Assert.Contains(document.Options, option =>
            option.Key == "--log-path <PATH>"
            && option.Description!.Contains("specified directory", StringComparison.Ordinal));
        Assert.Empty(document.Commands);
    }

    [Fact]
    public void Reattaches_Split_Long_Aliases_And_Metavars_From_Separate_Columns()
    {
        var parser = new TextParser();

        var document = parser.Parse(
            """
            Demo

            Options:
              -f  --file  FILE    Path to the file.
              -h  -?, --help      Show help.
            """);

        Assert.Contains(document.Options, option =>
            option.Key == "-f | --file <FILE>"
            && option.Description == "Path to the file.");
        Assert.Contains(document.Options, option =>
            option.Key == "-h | -? | --help"
            && option.Description == "Show help.");
    }
}
