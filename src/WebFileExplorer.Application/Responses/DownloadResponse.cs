namespace WebFileExplorer.Application.Responses;

public sealed record DownloadResponse(Stream Content, string Name);
