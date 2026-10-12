namespace WebFileExplorer.Application.Exceptions;

public sealed class FileAlreadyExistsException : IOException
{
    public FileAlreadyExistsException()
        : base("File already exists.")
    {
    }

    public FileAlreadyExistsException(string message) : base(message)
    {
    }

    public FileAlreadyExistsException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
