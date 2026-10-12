using Microsoft.Extensions.DependencyInjection;
using WebFileExplorer.Application.Abstractions.Messaging;
using WebFileExplorer.Application.FileSystemManagement.Commands.UploadFile;
using WebFileExplorer.Application.FileSystemManagement.Queries.BrowseFileSystem;
using WebFileExplorer.Application.FileSystemManagement.Queries.DownloadFileItem;
using WebFileExplorer.Application.FileSystemManagement.Queries.GetFileItems;
using WebFileExplorer.Application.FileSystemManagement.Queries.GetFolderItems;
using WebFileExplorer.Application.FileSystemManagement.Queries.SearchFileSystem;
using WebFileExplorer.Application.Responses;

namespace WebFileExplorer.Application;

public static class ConfigureServices
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Commands
        services.AddScoped<ICommandHandler<UploadCommand>, UploadCommandHandler>();

        // Queries
        services.AddScoped<IQueryHandler<BrowseQuery, FileSystemItemsResponse>, BrowseQueryHandler>();
        services.AddScoped<IQueryHandler<DownloadQuery, DownloadResponse>, DownloadQueryHandler>();
        services.AddScoped<IQueryHandler<GetFolderItemsQuery, IReadOnlyList<FolderItemResponse>>, GetFolderItemsQueryHandler>();
        services.AddScoped<IQueryHandler<GetFileItemsQuery, IReadOnlyList<FileItemResponse>>, GetFileItemsQueryHandler>();
        services.AddScoped<IQueryHandler<SearchQuery, FileSystemItemsResponse>, SearchQueryHandler>();

        return services;
    }
}
