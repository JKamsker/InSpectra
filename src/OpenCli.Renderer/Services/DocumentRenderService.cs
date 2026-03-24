using OpenCli.Renderer.Models;
using OpenCli.Renderer.Runtime;

namespace OpenCli.Renderer.Services;

public sealed class AcquiredRenderDocument
{
    public required OpenCliDocument RawDocument { get; init; }

    public required OpenCliDocument RenderDocument { get; init; }

    public string? XmlDocument { get; init; }

    public required RenderSourceInfo Source { get; init; }

    public required IReadOnlyList<string> Warnings { get; init; }
}

public sealed class DocumentRenderService(
    OpenCliDocumentLoader documentLoader,
    OpenCliDocumentCloner documentCloner,
    OpenCliXmlEnricher xmlEnricher,
    ExecutableResolver executableResolver,
    ProcessRunner processRunner)
{
    public async Task<AcquiredRenderDocument> LoadFromFileAsync(
        FileRenderRequest request,
        CancellationToken cancellationToken)
    {
        var rawDocument = await documentLoader.LoadFromFileAsync(request.OpenCliJsonPath, cancellationToken);
        var xmlDocument = await LoadXmlDocumentAsync(request.XmlDocPath, cancellationToken);

        return CreatePreparedDocument(
            rawDocument,
            xmlDocument,
            new RenderSourceInfo(
                "file",
                Path.GetFullPath(request.OpenCliJsonPath),
                request.XmlDocPath is null ? null : Path.GetFullPath(request.XmlDocPath),
                null));
    }

    public async Task<AcquiredRenderDocument> LoadFromExecAsync(
        ExecRenderRequest request,
        CancellationToken cancellationToken)
    {
        var executablePath = executableResolver.Resolve(request.Source, request.WorkingDirectory);
        var openCliArguments = request.SourceArguments.Concat(request.OpenCliArguments).ToArray();
        var openCliResult = await processRunner.RunAsync(
            executablePath,
            request.WorkingDirectory,
            openCliArguments,
            request.TimeoutSeconds,
            cancellationToken);

        var rawDocument = documentLoader.LoadFromJson(openCliResult.StandardOutput, executablePath);
        string? xmlDocument = null;
        string? xmlOrigin = null;

        if (request.IncludeXmlDoc)
        {
            var xmlArguments = request.SourceArguments.Concat(request.XmlDocArguments).ToArray();
            var xmlResult = await processRunner.RunAsync(
                executablePath,
                request.WorkingDirectory,
                xmlArguments,
                request.TimeoutSeconds,
                cancellationToken);

            xmlDocument = xmlResult.StandardOutput;
            xmlOrigin = executablePath;
        }

        return CreatePreparedDocument(
            rawDocument,
            xmlDocument,
            new RenderSourceInfo("exec", executablePath, xmlOrigin, executablePath));
    }

    private AcquiredRenderDocument CreatePreparedDocument(
        OpenCliDocument rawDocument,
        string? xmlDocument,
        RenderSourceInfo source)
    {
        var renderDocument = documentCloner.Clone(rawDocument);
        var warnings = new List<string>();

        if (!string.IsNullOrWhiteSpace(xmlDocument))
        {
            var enrichment = xmlEnricher.EnrichFromXml(
                renderDocument,
                xmlDocument,
                source.XmlDocOrigin ?? source.OpenCliOrigin);
            warnings.AddRange(enrichment.Warnings);
        }

        return new AcquiredRenderDocument
        {
            RawDocument = rawDocument,
            RenderDocument = renderDocument,
            XmlDocument = xmlDocument,
            Source = source,
            Warnings = warnings,
        };
    }

    private static async Task<string?> LoadXmlDocumentAsync(string? path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var resolvedPath = Path.GetFullPath(path);
        if (!File.Exists(resolvedPath))
        {
            throw new CliUsageException($"XML enrichment file `{resolvedPath}` does not exist.");
        }

        return await File.ReadAllTextAsync(resolvedPath, cancellationToken);
    }
}
