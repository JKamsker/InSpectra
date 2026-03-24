using System.Text;
using OpenCli.Renderer.Models;

namespace OpenCli.Renderer.Services;

public sealed class HtmlShellRenderer(
    HtmlAssetProvider assetProvider,
    HtmlContentFormatter contentFormatter,
    CommandPathResolver pathResolver)
{
    public string BuildSingleSidebar(NormalizedCliDocument document)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<nav class=\"sidebar-nav\"><a class=\"nav-link active\" href=\"#overview\">Overview</a>");
        if (document.RootArguments.Count > 0) builder.AppendLine("<a class=\"nav-link\" href=\"#root-arguments\">Root arguments</a>");
        if (document.RootOptions.Count > 0) builder.AppendLine("<a class=\"nav-link\" href=\"#root-options\">Root options</a>");
        if (document.Source.Examples.Count > 0) builder.AppendLine("<a class=\"nav-link\" href=\"#root-examples\">Examples</a>");
        if (document.Source.ExitCodes.Count > 0) builder.AppendLine("<a class=\"nav-link\" href=\"#root-exit-codes\">Exit codes</a>");
        if (document.Source.Metadata.Count > 0) builder.AppendLine("<a class=\"nav-link\" href=\"#root-metadata\">Metadata</a>");
        AppendCommandLinks(document.Commands, builder, null, string.Empty, true);
        builder.AppendLine("</nav>");
        return builder.ToString();
    }

    public string BuildTreeSidebar(NormalizedCliDocument document, string currentPagePath, string? currentPath)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"<nav class=\"sidebar-nav\"><a class=\"nav-link{(currentPath is null ? " active" : string.Empty)}\" href=\"{contentFormatter.Encode(pathResolver.CreateRelativeLink(currentPagePath, "index.html"))}\">Overview</a>");
        AppendCommandLinks(document.Commands, builder, currentPath, currentPagePath, false);
        builder.AppendLine("</nav>");
        return builder.ToString();
    }

    public string RenderShell(NormalizedCliDocument document, string pageTitle, string sidebar, string content)
    {
        return $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="utf-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1" />
              <title>{{contentFormatter.Encode(document.Source.Info.Title)}} · {{contentFormatter.Encode(pageTitle)}}</title>
              <link rel="preconnect" href="https://fonts.googleapis.com">
              <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
              <link rel="preload" as="style" href="https://fonts.googleapis.com/css2?family=JetBrains+Mono:wght@400;500;600;700&family=Plus+Jakarta+Sans:wght@400;500;600;700&display=swap" onload="this.rel='stylesheet'">
              <noscript><link href="https://fonts.googleapis.com/css2?family=JetBrains+Mono:wght@400;500;600;700&family=Plus+Jakarta+Sans:wght@400;500;600;700&display=swap" rel="stylesheet"></noscript>
              <style>{{assetProvider.GetStyles()}}</style>
            </head>
            <body>
              <div class="shell">
                <header class="topbar">
                  <div class="brand"><span class="brand-mark">&gt;_</span><div><strong>{{contentFormatter.Encode(document.Source.Info.Title)}}</strong><span>v{{contentFormatter.Encode(document.Source.Info.Version)}} · OpenCLI {{contentFormatter.Encode(document.Source.OpenCliVersion)}}</span></div></div>
                  <div class="topbar-actions"><div class="topbar-context">{{contentFormatter.Encode(pageTitle)}}</div><button class="composer-toggle" data-composer-toggle title="Toggle Composer"><svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="3" width="18" height="18" rx="2"/><path d="M15 3v18"/></svg><span>Composer</span></button><button class="theme-toggle" data-theme-toggle title="Toggle theme"><svg class="icon-sun" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="5"/><line x1="12" y1="1" x2="12" y2="3"/><line x1="12" y1="21" x2="12" y2="23"/><line x1="4.22" y1="4.22" x2="5.64" y2="5.64"/><line x1="18.36" y1="18.36" x2="19.78" y2="19.78"/><line x1="1" y1="12" x2="3" y2="12"/><line x1="21" y1="12" x2="23" y2="12"/><line x1="4.22" y1="19.78" x2="5.64" y2="18.36"/><line x1="18.36" y1="5.64" x2="19.78" y2="4.22"/></svg><svg class="icon-moon" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/></svg></button></div>
                </header>
                <div class="layout">
                  <aside class="sidebar">
                    <label class="search"><span>Filter commands</span><input type="search" placeholder="Search command tree" data-nav-search /></label>
                    {{sidebar}}
                  </aside>
                  <main class="content"><div class="content-inner">{{content}}</div></main>
                  <aside class="composer" data-composer hidden><div class="composer-resize" data-composer-resize></div><div class="composer-header"><svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="m4 17 6-5-6-5"/><path d="M12 19h8"/></svg><span>Composer</span></div><div class="composer-body"></div><div class="composer-footer"><span class="composer-label">Generated Command</span><div class="composer-output-wrap"><pre class="composer-output">...</pre><button class="composer-copy" data-composer-copy title="Copy to clipboard"><svg class="copy-icon" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="9" y="9" width="13" height="13" rx="2"/><path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"/></svg><svg class="check-icon" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M20 6 9 17l-5-5"/></svg></button></div></div></aside>
                </div>
              </div>
              <script>{{assetProvider.GetScript()}}</script>
            </body>
            </html>
            """;
    }

    private void AppendCommandLinks(IEnumerable<NormalizedCommand> commands, StringBuilder builder, string? currentPath, string currentPagePath, bool isSinglePage)
    {
        var commandList = commands.ToList();
        if (commandList.Count == 0)
        {
            return;
        }

        builder.AppendLine("<div class=\"nav-label\">Commands</div><ul class=\"nav-tree\">");
        foreach (var command in commandList)
        {
            AppendSidebarNode(builder, command, currentPath, currentPagePath, isSinglePage);
        }

        builder.AppendLine("</ul>");
    }

    private void AppendSidebarNode(StringBuilder builder, NormalizedCommand command, string? currentPath, string currentPagePath, bool isSinglePage)
    {
        var href = isSinglePage
            ? $"#command-{pathResolver.CreateAnchorId(command.Path)}"
            : pathResolver.CreateRelativeLink(currentPagePath, pathResolver.GetCommandRelativePath(command, "html"));
        var hasChildren = command.Commands.Count > 0;

        var collapsedClass = hasChildren ? " collapsed" : string.Empty;
        builder.AppendLine($"<li class=\"nav-item{collapsedClass}\" data-nav-item data-label=\"{contentFormatter.Encode(command.Path.ToLowerInvariant())}\">");
        builder.Append("<div class=\"nav-row\">");

        if (hasChildren)
        {
            builder.Append("<span class=\"nav-chevron\" data-nav-toggle><svg width=\"14\" height=\"14\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2.5\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"m9 18 6-6-6-6\"/></svg></span>");
        }
        else
        {
            builder.Append("<span class=\"nav-icon\"><svg width=\"12\" height=\"12\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"m4 17 6-5-6-5\"/><path d=\"M12 19h8\"/></svg></span>");
        }

        builder.Append($"<a class=\"nav-link{(command.Path == currentPath ? " active" : string.Empty)}\" href=\"{contentFormatter.Encode(href)}\">{contentFormatter.Encode(command.Command.Name)}</a>");
        builder.AppendLine("</div>");

        if (hasChildren)
        {
            builder.AppendLine("<ul class=\"nav-tree\">");
            foreach (var child in command.Commands)
            {
                AppendSidebarNode(builder, child, currentPath, currentPagePath, isSinglePage);
            }

            builder.AppendLine("</ul>");
        }

        builder.AppendLine("</li>");
    }
}
