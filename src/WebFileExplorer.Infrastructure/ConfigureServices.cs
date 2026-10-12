using Microsoft.Extensions.DependencyInjection;
using WebFileExplorer.Application.FileItems;
using WebFileExplorer.Application.Interfaces;
using WebFileExplorer.Infrastructure.FileItems;
using WebFileExplorer.Infrastructure.FileSystem;
using WebFileExplorer.Infrastructure.FolderItems;

namespace WebFileExplorer.Infrastructure;

public static class ConfigureServices
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // One configured root and path policy shared by the filesystem repositories.
        services.AddSingleton<PhysicalFileSystem>();

        // Repositories read and write the configured file system.
        services.AddScoped<IFolderItemRepository, FolderItemRepository>();
        services.AddScoped<IFileItemRepository, FileItemRepository>();
        services.AddScoped<IFileContentRepository, FileItemRepository>();

        // Services translate Domain entities into Application responses.
        services.AddTransient<IFolderItemService, FolderItemService>();
        services.AddTransient<IFileItemService, FileItemService>();

        return services;
    }
}
