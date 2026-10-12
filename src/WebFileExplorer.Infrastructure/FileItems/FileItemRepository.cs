using WebFileExplorer.Application.Configurations;
using WebFileExplorer.Application.Exceptions;
using WebFileExplorer.Application.FileItems;
using WebFileExplorer.Application.Interfaces;
using WebFileExplorer.Domain.FileItems;
using WebFileExplorer.Domain.FolderItems;
using WebFileExplorer.Domain.Paths;
using WebFileExplorer.Infrastructure.FileSystem;

namespace WebFileExplorer.Infrastructure.FileItems;

internal class FileItemRepository : IFileItemRepository, IFileContentRepository
{
    private const int UPLOAD_BUFFER_SIZE = 80 * 1024; // 80KiB

    private readonly PhysicalFileSystem _fileSystem;
    private readonly AppConfiguration _configuration;

    public FileItemRepository(PhysicalFileSystem fileSystem, AppConfiguration configuration)
    {
        _fileSystem = fileSystem;
        _configuration = configuration;
    }

    public Task<IEnumerable<FileItem>> GetFileItemsFromFolder(FolderItemId folderItemId)
    {
        // Items yields immediate children, load into memory before returning.
        IEnumerable<FileItem> fileItems =
            _fileSystem.Items(folderItemId.Value, CancellationToken.None)
                       .Where(File.Exists)
                       .Select(path => new FileItem(_fileSystem.Relative(path), new FileInfo(path).Length))
                       .OrderBy(fileItem => fileItem.Name, StringComparer.OrdinalIgnoreCase)
                       .ToArray();

        return Task.FromResult(fileItems);
    }

    public Task<FileItem?> GetById(FileItemId fileItemId)
    {
        // Ensure given path is within the configured root and is valid.
        string path = _fileSystem.Resolve(fileItemId.Value);

        FileItem? fileItem = File.Exists(path)
            ? new FileItem(fileItemId.Value, new FileInfo(path).Length)
            : null;

        return Task.FromResult(fileItem);
    }

    public Stream OpenRead(FileItemId fileItemId)
    {
        // Ensure given path is within the configured root and is valid.
        string path = _fileSystem.Resolve(fileItemId.Value);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("File not found.");
        }

        return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    public async Task<FileItem> SaveNew(
        string directory,
        string fileName,
        Stream content,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("A file name cannot be blank.", nameof(fileName));
        }

        // A file upload name must not contain part of the path.
        RelativePathRules.Validate(fileName);
        if (fileName.Contains('/'))
        {
            throw new ArgumentException("A file name cannot contain a path.");
        }

        string folder = _fileSystem.Resolve(directory);
        if (!Directory.Exists(folder))
        {
            throw new DirectoryNotFoundException("Folder not found.");
        }

        string relativePath = directory.Length == 0 ? fileName : $"{directory}/{fileName}";
        string destination = _fileSystem.Resolve(relativePath);
        bool created = false;
        long written = 0;

        try
        {
            await using var output = OpenNewFile(destination);
            created = true;

            byte[] buffer = new byte[UPLOAD_BUFFER_SIZE];
            int read;
            while ((read = await content.ReadAsync(buffer, cancellationToken)) != 0)
            {
                written = checked(written + read);
                if (written > _configuration.MaxUploadBytes)
                {
                    throw new FileBrowserLimitExceededException("Upload exceeds the configured file-size limit.");
                }

                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }
        }
        catch
        {
            // FileMode.CreateNew will not overwrite an existing file, so if the file was
            //  partially created then we must remove the partial file.
            if (created)
            {
                File.Delete(destination);
            }

            throw;
        }

        return new FileItem(relativePath, written);
    }

    private static FileStream OpenNewFile(string destination)
    {
        try
        {
            return new FileStream(
                destination,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None);
        }
        catch (IOException) when (File.Exists(destination))
        {
            throw new FileAlreadyExistsException();
        }
    }
}
