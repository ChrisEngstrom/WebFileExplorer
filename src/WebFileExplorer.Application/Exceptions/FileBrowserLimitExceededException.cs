namespace WebFileExplorer.Application.Exceptions;

public sealed class FileBrowserLimitExceededException : Exception
{
    public FileBrowserLimitExceededException(string message)
        : base(message)
    {
    }

    public FileBrowserLimitExceededException()
    {
    }

    public FileBrowserLimitExceededException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
