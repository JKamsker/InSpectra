using System.Text;
using OpenCli.Renderer.Models;
using OpenCli.Renderer.Runtime;

namespace OpenCli.Renderer.Services;

public sealed class HtmlRenderer(
    HtmlSectionRenderer sectionRenderer,
    HtmlShellRenderer shellRenderer,
    HtmlContentFormatter contentFormatter,
    CommandPathResolver pathResolver) : IDocumentRenderer
{
    public DocumentFormat Format => DocumentFormat.Html;

    public string RenderSingle(NormalizedCliDocument document, bool includeMetadata)
    {
        var content = new StringBuilder();

        content.AppendLine("<div class=\"page active\" data-page=\"overview\">");
        sectionRenderer.AppendRootHero(document, content);
        sectionRenderer.AppendRootDetails(
            document,
            content,
            includeMetadata,
            command => $"#command-{pathResolver.CreateAnchorId(command.Path)}",
            includeCommandCards: false);
        content.AppendLine("</div>");

        if (document.Commands.Count > 0)
        {
            AppendCommandPages(document, document.Commands, content, includeMetadata);
        }

        return shellRenderer.RenderShell(document, "Overview", shellRenderer.BuildSingleSidebar(document), content.ToString());
    }

    private void AppendCommandPages(
        NormalizedCliDocument document,
        IReadOnlyList<NormalizedCommand> commands,
        StringBuilder content,
        bool includeMetadata)
    {
        foreach (var command in commands)
        {
            var anchorId = pathResolver.CreateAnchorId(command.Path);
            content.AppendLine($"<template data-page=\"command-{anchorId}\">");
            sectionRenderer.AppendCommandBreadcrumb(document, command, content);
            sectionRenderer.AppendCommandBody(command, content, includeMetadata,
                child => $"#command-{pathResolver.CreateAnchorId(child.Path)}", includeWrapper: true);
            content.AppendLine("</template>");

            AppendCommandPages(document, command.Commands, content, includeMetadata);
        }
    }

    public IReadOnlyList<RelativeRenderedFile> RenderTree(NormalizedCliDocument document, bool includeMetadata)
    {
        var files = new List<RelativeRenderedFile>
        {
            new("index.html", RenderRootPage(document, includeMetadata)),
        };

        foreach (var command in document.Commands)
        {
            AppendCommandPages(document, command, includeMetadata, files);
        }

        return files;
    }

    private string RenderRootPage(NormalizedCliDocument document, bool includeMetadata)
    {
        var content = new StringBuilder();
        sectionRenderer.AppendRootHero(document, content);
        sectionRenderer.AppendRootDetails(
            document,
            content,
            includeMetadata,
            command => pathResolver.GetCommandRelativePath(command, "html"));

        return shellRenderer.RenderShell(document, "Overview", shellRenderer.BuildTreeSidebar(document, "index.html", null), content.ToString());
    }

    private void AppendCommandPages(
        NormalizedCliDocument document,
        NormalizedCommand command,
        bool includeMetadata,
        ICollection<RelativeRenderedFile> files)
    {
        var relativePath = pathResolver.GetCommandRelativePath(command, "html");
        files.Add(new RelativeRenderedFile(relativePath, RenderCommandPage(document, command, relativePath, includeMetadata)));

        foreach (var child in command.Commands)
        {
            AppendCommandPages(document, child, includeMetadata, files);
        }
    }

    private string RenderCommandPage(
        NormalizedCliDocument document,
        NormalizedCommand command,
        string currentPagePath,
        bool includeMetadata)
    {
        var content = new StringBuilder();
        content.AppendLine("<section class=\"panel breadcrumb\">");
        content.AppendLine($"<a href=\"{contentFormatter.Encode(pathResolver.CreateRelativeLink(currentPagePath, "index.html"))}\">{contentFormatter.Encode(document.Source.Info.Title)}</a>");
        foreach (var segment in command.Path.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            content.AppendLine($"<span class=\"crumb-sep\">›</span><span>{contentFormatter.Encode(segment)}</span>");
        }

        content.AppendLine("</section>");
        sectionRenderer.AppendCommandBody(
            command,
            content,
            includeMetadata,
            child => pathResolver.CreateRelativeLink(currentPagePath, pathResolver.GetCommandRelativePath(child, "html")),
            includeWrapper: true);

        return shellRenderer.RenderShell(document, command.Path, shellRenderer.BuildTreeSidebar(document, currentPagePath, command.Path), content.ToString());
    }
}
