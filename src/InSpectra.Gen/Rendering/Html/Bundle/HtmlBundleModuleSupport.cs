using System.Text.RegularExpressions;

namespace InSpectra.Gen.Rendering.Html.Bundle;

internal static class HtmlBundleModuleSupport
{
    public static string BundleModulesAsIife(string entryCode, string entryDirectory)
    {
        var importPattern = new Regex(@"import\{([^}]+)\}from""\./([\w.=-]+\.js)"";?");
        var importMatch = importPattern.Match(entryCode);
        if (!importMatch.Success)
        {
            return $"(function(){{{entryCode}}})();";
        }

        var chunkPath = Path.Combine(entryDirectory, importMatch.Groups[2].Value);
        if (!File.Exists(chunkPath))
        {
            return $"(function(){{{entryCode}}})();";
        }

        var chunkCode = File.ReadAllText(chunkPath);
        var exportPattern = new Regex(@"export\{([^}]+)\};?\s*$");
        var exportMatch = exportPattern.Match(chunkCode);
        if (!exportMatch.Success)
        {
            return $"(function(){{{chunkCode}\n{entryCode}}})();";
        }

        var exportMap = CreateExportMap(exportMatch.Groups[1].Value);
        var importBindings = ParseImportBindings(importMatch.Groups[1].Value);
        var cleanedChunk = exportPattern.Replace(chunkCode, string.Empty);
        var cleanedEntry = importPattern.Replace(entryCode, string.Empty, 1);
        var exportedMembers = string.Join(",", importBindings.Select(binding =>
            exportMap.TryGetValue(binding.ExportedName, out var chunkLocal)
                ? $"{binding.ExportedName}:{chunkLocal}"
                : string.Empty));
        var aliases = string.Join(string.Empty, importBindings.Select(binding => $"var {binding.LocalAlias}=__M.{binding.ExportedName};"));

        return $"(function(){{var __M=(function(){{{cleanedChunk}return{{{exportedMembers}}}}})();{aliases}{cleanedEntry}}})();";
    }

    private static Dictionary<string, string> CreateExportMap(string exportList)
    {
        var exportMap = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in exportList.Split(','))
        {
            var parts = pair.Trim().Split(" as ", 2, StringSplitOptions.TrimEntries);
            exportMap[parts[^1]] = parts[0];
        }

        return exportMap;
    }

    private static List<(string ExportedName, string LocalAlias)> ParseImportBindings(string bindingList)
    {
        var bindings = new List<(string ExportedName, string LocalAlias)>();
        foreach (var pair in bindingList.Split(','))
        {
            var parts = pair.Trim().Split(" as ", 2, StringSplitOptions.TrimEntries);
            bindings.Add(parts.Length == 2 ? (parts[0], parts[1]) : (parts[0], parts[0]));
        }

        return bindings;
    }
}

