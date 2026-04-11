using System.Text;
using InSpectra.Gen.Rendering.Pipeline.Model;

namespace InSpectra.Gen.Rendering;

public sealed class CommandPathResolver
{
    public string CreateAnchorId(string value)
    {
        var builder = new StringBuilder();
        foreach (var character in value.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
            }
            else if (character is ' ' or '-' or '_')
            {
                builder.Append('-');
            }
        }

        return builder.ToString().Trim('-');
    }

    public string GetCommandRelativePath(NormalizedCommand command, string extension)
    {
        var parts = command.Path.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(SanitizePathSegment)
            .ToArray();

        if (command.Commands.Count > 0)
        {
            return Path.Combine(parts).Replace('\\', '/') + $"/index.{extension}";
        }

        var parent = parts.Length > 1 ? Path.Combine(parts[..^1]).Replace('\\', '/') : string.Empty;
        var fileName = $"{parts[^1]}.{extension}";
        return string.IsNullOrEmpty(parent) ? fileName : $"{parent}/{fileName}";
    }

    public string CreateRelativeLink(string currentPagePath, string targetPath)
    {
        var currentDirectory = Path.GetDirectoryName(currentPagePath)?.Replace('\\', '/');
        var baseDirectory = string.IsNullOrWhiteSpace(currentDirectory) ? "." : currentDirectory;
        return Path.GetRelativePath(baseDirectory, targetPath).Replace('\\', '/');
    }

    /// <summary>
    /// Builds the relative group-file path for a prefix of <paramref name="pathSegments"/>, sanitizing
    /// each segment the same way <see cref="GetCommandRelativePath"/> does for group commands. Used by
    /// the hybrid renderer to compute breadcrumb links to ancestor group files.
    /// </summary>
    public string BuildGroupFilePath(string[] pathSegments, int segmentCount, string extension)
    {
        if (segmentCount <= 0 || segmentCount > pathSegments.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(segmentCount));
        }

        var sanitized = pathSegments.Take(segmentCount).Select(SanitizePathSegment).ToArray();
        return Path.Combine(sanitized).Replace('\\', '/') + $"/index.{extension}";
    }

    public string? GetParentRelativePath(NormalizedCommand command, string extension)
    {
        var parts = command.Path.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
        {
            return $"index.{extension}";
        }

        var parentParts = parts[..^1];
        var leadingPath = parentParts.Length == 1
            ? string.Empty
            : Path.Combine(parentParts[..^1].Select(SanitizePathSegment).ToArray()).Replace('\\', '/');
        var lastParent = SanitizePathSegment(parentParts[^1]);
        return string.IsNullOrEmpty(leadingPath)
            ? $"{lastParent}/index.{extension}"
            : $"{leadingPath}/{lastParent}/index.{extension}";
    }

    public string GetParentDisplayName(NormalizedCommand command)
    {
        var parts = command.Path.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', parts[..^1]);
    }

    private static string SanitizePathSegment(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Select(character => invalid.Contains(character) ? '-' : character).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "command" : sanitized;
    }
}
