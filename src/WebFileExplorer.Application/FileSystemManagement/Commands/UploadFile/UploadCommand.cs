using WebFileExplorer.Application.Abstractions.Messaging;

namespace WebFileExplorer.Application.FileSystemManagement.Commands.UploadFile;

public sealed record UploadCommand(
    string RelativeFolderPath,
    string FileName,
    Stream Content)
    : ICommand;
