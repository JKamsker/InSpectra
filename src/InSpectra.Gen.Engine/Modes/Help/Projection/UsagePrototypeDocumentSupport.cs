namespace InSpectra.Gen.Engine.Modes.Help.Projection;

using InSpectra.Gen.Engine.Contracts.Documents;
using InSpectra.Gen.Engine.Modes.Help.Inference.Usage.Prototypes;

internal static class UsagePrototypeDocumentSupport
{
    public static Document? Create(
        string rootCommandName,
        string commandPath,
        IReadOnlyDictionary<string, Document> helpDocuments)
    {
        var prototypes = helpDocuments.Values
            .SelectMany(document => UsagePrototypeSupport.ExtractLeafCommandPrototypes(
                rootCommandName,
                commandPath,
                document.UsageLines))
            .ToArray();
        if (prototypes.Length == 0)
        {
            return null;
        }

        var description = prototypes
            .Select(prototype => prototype.Description)
            .Where(description => !string.IsNullOrWhiteSpace(description))
            .OrderByDescending(description => description!.Length)
            .FirstOrDefault();
        return new Document(
            Title: null,
            Version: null,
            ApplicationDescription: null,
            CommandDescription: description,
            UsageLines: prototypes.Select(prototype => prototype.Prototype).ToArray(),
            Arguments: [],
            Options: [],
            Commands: []);
    }
}
