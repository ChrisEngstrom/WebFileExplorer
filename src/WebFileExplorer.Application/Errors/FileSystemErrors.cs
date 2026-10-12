using WebFileExplorer.SharedKernel;

namespace WebFileExplorer.Application.Errors;

public static class FileSystemErrors
{
    public static readonly Error InvalidPath = Error.Validation("Path.Invalid", "Invalid path or file name.");

    public static readonly Error SearchTooLong = Error.Validation("Browse.SearchTooLong", "Search query is too long.");

    public static readonly Error FolderNotFound = Error.NotFound("Folder.NotFound", "Folder not found.");

    public static readonly Error FileNotFound = Error.NotFound("File.NotFound", "File not found.");

    public static readonly Error FileExists = Error.Conflict("File.Exists", "File already exists.");

    public static readonly Error LimitExceeded = Error.LimitExceeded("FileSystem.LimitExceeded", "Directory or upload limit exceeded.");
}
