using WebFileExplorer.Application.Configurations;
using WebFileExplorer.Application.Exceptions;
using WebFileExplorer.Domain.Paths;

namespace WebFileExplorer.Infrastructure.FileSystem;

internal sealed class PhysicalFileSystem
{
    private readonly string _root;
    private readonly int _maxScannedItems;

    public PhysicalFileSystem(AppConfiguration configuration)
    {
        // All paths handled by the repositories are anchored to this configured directory
        _root = Path.GetFullPath(configuration.RootPath);
        _maxScannedItems = configuration.MaxScannedItems;

        if (!Directory.Exists(_root))
        {
            throw new DirectoryNotFoundException("Configured root not found.");
        }
    }

    /// <summary>
    /// Method to ensure the relative path provided is valid and allowed in the system (under the configured root)
    /// </summary>
    /// <returns></returns>
    public string Resolve(string relativePath)
    {
        RelativePathRules.Validate(relativePath, allowRoot: true);

        string current = _root;
        foreach (string part in relativePath.Split('/'))
        {
            current = Path.Combine(current, part);

            if (IsLink(current))
            {
                throw new ArgumentException("Symbolic links are not browsable.");
            }
        }

        current = Path.GetFullPath(current);

        if (current != _root &&
            !current.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new ArgumentException("Invalid relative path.");
        }

        return current;
    }

    // Convert a path already obtained under the root to the slash-separated path used by the app
    public string Relative(string absolute) =>
        Path.GetRelativePath(_root, absolute)
            .Replace(Path.DirectorySeparatorChar, '/');

    public IEnumerable<string> Items(string directory, CancellationToken cancellationToken)
    {
        string resolved = Resolve(directory);
        if (!Directory.Exists(resolved))
        {
            throw new DirectoryNotFoundException("Folder not found.");
        }

        int count = 0;
        foreach (string child in Directory.EnumerateFileSystemEntries(resolved))
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Every child is counted towards the limit, even if it is a skipped link
            if (++count > _maxScannedItems)
            {
                throw new FileBrowserLimitExceededException($"Directory scan exceeds {_maxScannedItems} items.");
            }

            // Skip symlinks
            if (IsLink(child))
            {
                continue;
            }

            yield return child;
        }
    }

    /// <summary>
    /// Checks for reparse points (symlinks), which should not be traversed or returned
    /// </summary>
    private static bool IsLink(string path)
    {
        bool result;

        try
        {
            result = (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
        }
        catch (FileNotFoundException)
        {
            result = false;
        }
        catch (DirectoryNotFoundException)
        {
            result = false;
        }

        return result;
    }
}
