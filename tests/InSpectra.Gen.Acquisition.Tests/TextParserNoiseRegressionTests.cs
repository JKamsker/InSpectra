namespace InSpectra.Gen.Acquisition.Tests;

using InSpectra.Discovery.Tool.Help.Parsing;

public sealed class TextParserNoiseRegressionTests
{
    [Fact]
    public void Rejects_Oakton_Invalid_Usage_As_NonHelp()
    {
        var parser = new TextParser();

        var document = parser.Parse(
            """
            Invalid usage
            Unknown argument or flag for value --help

            awss3upload - AwsS3UploadCommand
            └── AwsS3UploadCommand
                └── dotnet run -- awss3upload <bucketname> <keyprefix> <localpath>
            """);

        Assert.False(document.HasContent);
    }

    [Fact]
    public void Ignores_CommandLineParser_Error_Preamble_And_Pseudo_Verbs()
    {
        var parser = new TextParser();

        var document = parser.Parse(
            """
            Error parsing
             CommandLine.HelpVerbRequestedError
            GSoft.CertificateTool 1.0.0+bb4d252c46ae13f3169853b02995b8cd77635ab6
            Copyright (C) 2026 GSoft.CertificateTool

              add        Installs a pfx certificate to selected store.
              remove     Removes a pfx certificate from selected store.
              version    Display version information.
            """);

        Assert.Equal("GSoft.CertificateTool", document.Title);
        Assert.Equal("1.0.0+bb4d252c46ae13f3169853b02995b8cd77635ab6", document.Version);
        Assert.Contains(document.Commands, command => command.Key == "add");
        Assert.Contains(document.Commands, command => command.Key == "remove");
        Assert.Contains(document.Commands, command => command.Key == "version");
        Assert.DoesNotContain(document.Commands, command => command.Key == "CommandLine.HelpVerbRequestedError");
    }

    [Fact]
    public void Rejects_Explicit_Help_Switch_Rejection_Preambles()
    {
        var parser = new TextParser();

        var document = parser.Parse(
            """
            --help is an unknown parameter
            Usage of the tool (argument names case insensitive, values case insensitive where marked, arguments can be given in any order):
            octo-ckc [-[shortTerm] or [/ or --][longTerm] [argument value]] ...
            """);

        Assert.False(document.HasContent);
        Assert.Null(document.Title);
        Assert.Empty(document.Options);
        Assert.Empty(document.Commands);
    }

    [Fact]
    public void Rejects_Unrecognized_Help_Switch_Preambles()
    {
        var parser = new TextParser();

        var document = parser.Parse(
            """
            Unrecognized option '--help'
            Usage: antlr4cg [options]
            """);

        Assert.False(document.HasContent);
        Assert.Null(document.Title);
        Assert.Empty(document.Options);
    }

    [Fact]
    public void Does_Not_Start_New_Command_From_Indented_Wrapped_Description_Or_Help_Hints()
    {
        var parser = new TextParser();

        var document = parser.Parse(
            """
            USAGE: Propulsion.Tool [--help] <subcommand> [<options>]

            SUBCOMMANDS:

                init <options>        Initialize auxiliary store (Supported for `cosmos`
                                      Only).
                initpg <options>      Initialize a postgres checkpoint store

                Use 'Propulsion.Tool <subcommand> --help' for additional information.
            """);

        var init = Assert.Single(document.Commands, command => command.Key == "init");
        Assert.Contains("Only).", init.Description, StringComparison.Ordinal);
        Assert.DoesNotContain(document.Commands, command => command.Key == "Only).");
        Assert.DoesNotContain(document.Commands, command => command.Key == "Use");
    }
}
