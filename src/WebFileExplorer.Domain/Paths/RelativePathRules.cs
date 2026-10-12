namespace WebFileExplorer.Domain.Paths;

public static class RelativePathRules
{
    /// <summary>
    /// Validate that relative paths in the file system adhere to the following path syntax:
    ///     relative to the configured root
    ///     forward slash separated without any embedded path traversal
    /// </summary>
    /// <returns>On success, the original string unchanged</returns>
    public static string Validate(string relativePath, bool allowRoot = false)
    {
        ArgumentNullException.ThrowIfNull(relativePath);

        // Rules Used:
        //  Empty is only valid for the root
        //  Use forward slash separated relative paths: no leading slash, backslash, colon, NUL, or empty,
        //      "." or ".." segments to avoid path traversal attempts
        if ((relativePath.Length == 0 && !allowRoot) ||
            relativePath.StartsWith('/') ||
            relativePath.Contains('\\') ||
            relativePath.Contains(':') ||
            relativePath.Contains('\0') ||
            (relativePath.Length > 0 && relativePath.Split('/').Any(part => part is "" or "." or "..")))
        {
            throw new ArgumentException("Path must be a normalized relative path.", nameof(relativePath));
        }

        return relativePath;
    }
}
