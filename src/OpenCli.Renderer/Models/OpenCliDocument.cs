using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace OpenCli.Renderer.Models;

public sealed class OpenCliDocument
{
    [JsonPropertyName("opencli")]
    public string OpenCliVersion { get; init; } = string.Empty;

    [JsonPropertyName("info")]
    public OpenCliInfo Info { get; init; } = new();

    [JsonPropertyName("conventions")]
    public OpenCliConventions? Conventions { get; init; }

    [JsonPropertyName("arguments")]
    public List<OpenCliArgument> Arguments { get; init; } = [];

    [JsonPropertyName("options")]
    public List<OpenCliOption> Options { get; init; } = [];

    [JsonPropertyName("commands")]
    public List<OpenCliCommand> Commands { get; init; } = [];

    [JsonPropertyName("exitCodes")]
    public List<OpenCliExitCode> ExitCodes { get; init; } = [];

    [JsonPropertyName("examples")]
    public List<string> Examples { get; init; } = [];

    [JsonPropertyName("interactive")]
    public bool Interactive { get; init; }

    [JsonPropertyName("metadata")]
    public List<OpenCliMetadata> Metadata { get; init; } = [];
}

public sealed class OpenCliInfo
{
    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("contact")]
    public OpenCliContact? Contact { get; init; }

    [JsonPropertyName("license")]
    public OpenCliLicense? License { get; init; }

    [JsonPropertyName("version")]
    public string Version { get; init; } = string.Empty;
}

public sealed class OpenCliConventions
{
    [JsonPropertyName("groupOptions")]
    public bool? GroupOptions { get; init; }

    [JsonPropertyName("optionSeparator")]
    public string? OptionSeparator { get; init; }
}

public sealed class OpenCliContact
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("url")]
    public string? Url { get; init; }

    [JsonPropertyName("email")]
    public string? Email { get; init; }
}

public sealed class OpenCliLicense
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("identifier")]
    public string? Identifier { get; init; }

    [JsonPropertyName("url")]
    public string? Url { get; init; }
}

public sealed class OpenCliCommand
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("aliases")]
    public List<string> Aliases { get; init; } = [];

    [JsonPropertyName("options")]
    public List<OpenCliOption> Options { get; init; } = [];

    [JsonPropertyName("arguments")]
    public List<OpenCliArgument> Arguments { get; init; } = [];

    [JsonPropertyName("commands")]
    public List<OpenCliCommand> Commands { get; init; } = [];

    [JsonPropertyName("exitCodes")]
    public List<OpenCliExitCode> ExitCodes { get; init; } = [];

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("hidden")]
    public bool Hidden { get; init; }

    [JsonPropertyName("examples")]
    public List<string> Examples { get; init; } = [];

    [JsonPropertyName("interactive")]
    public bool Interactive { get; init; }

    [JsonPropertyName("metadata")]
    public List<OpenCliMetadata> Metadata { get; init; } = [];
}

public sealed class OpenCliOption
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("required")]
    public bool Required { get; init; }

    [JsonPropertyName("aliases")]
    public List<string> Aliases { get; init; } = [];

    [JsonPropertyName("arguments")]
    public List<OpenCliArgument> Arguments { get; init; } = [];

    [JsonPropertyName("group")]
    public string? Group { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("recursive")]
    public bool Recursive { get; init; }

    [JsonPropertyName("hidden")]
    public bool Hidden { get; init; }

    [JsonPropertyName("metadata")]
    public List<OpenCliMetadata> Metadata { get; init; } = [];
}

public sealed class OpenCliArgument
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("required")]
    public bool Required { get; init; }

    [JsonPropertyName("arity")]
    public OpenCliArity? Arity { get; init; }

    [JsonPropertyName("acceptedValues")]
    public List<string> AcceptedValues { get; init; } = [];

    [JsonPropertyName("group")]
    public string? Group { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("hidden")]
    public bool Hidden { get; init; }

    [JsonPropertyName("metadata")]
    public List<OpenCliMetadata> Metadata { get; init; } = [];
}

public sealed class OpenCliArity
{
    [JsonPropertyName("minimum")]
    public int? Minimum { get; init; }

    [JsonPropertyName("maximum")]
    public int? Maximum { get; init; }
}

public sealed class OpenCliExitCode
{
    [JsonPropertyName("code")]
    public int Code { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }
}

public sealed class OpenCliMetadata
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("value")]
    public JsonNode? Value { get; init; }
}
