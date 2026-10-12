using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using WebFileExplorer.Application;
using WebFileExplorer.Application.Configurations;
using WebFileExplorer.Application.Errors;
using WebFileExplorer.Infrastructure;
using WebFileExplorer.WebApp.Middleware;

namespace WebFileExplorer.WebApp;

internal static class Program
{
    private const int MULTIPART_OVERHEAD_BYTES = 1024 * 1024; // 1MiB

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Set up the AppConfiguration
        builder.RegisterAppConfigurationService();

        builder.ConfigureUploadLimitOptions();

        builder.Services.AddControllers();

        // Register Application and Infrastructure services
        builder.Services.AddApplication();
        builder.Services.AddInfrastructure();

        var app = builder.Build();

        // Resolve the AppConfiguration now so a missing RootPath fails startup,
        //  instead of waiting for the first request
        _ = app.Services.GetRequiredService<AppConfiguration>();

        app.UseRouting();

        app.UseMiddleware<ApiRequestLoggingMiddleware>();
        app.UseConfiguredFileSizeLimitationGuard();

        app.UseHttpsRedirection();
        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.MapControllers();

        app.Run();
    }

    /// <summary>
    /// Load the appsettings.json configuration values into the Application layer's <see cref="AppConfiguration"/>
    ///     and register it with the ServiceProvider.
    /// </summary>
    private static void RegisterAppConfigurationService(this WebApplicationBuilder builder)
    {
        string contentRoot = builder.Environment.ContentRootPath;
        builder.Services.AddSingleton(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>()
                                  .GetSection("FileBrowser")
                                  .Get<AppConfiguration>();

            if (string.IsNullOrWhiteSpace(configuration?.RootPath))
            {
                throw new InvalidOperationException("Configure FileBrowser:RootPath.");
            }

            // Get the absolute root path for the data from the relative root
            //  in the configuration and where the program is running
            configuration.RootPath = Path.GetFullPath(configuration.RootPath, contentRoot);

            if (!Directory.Exists(configuration.RootPath))
            {
                throw new InvalidOperationException("Configured FileBrowser:RootPath directory does not exist.");
            }

            return configuration;
        });
    }

    /// <summary>
    /// Configure the file upload limits for Kestrel and HTTP forms.
    /// </summary>
    private static void ConfigureUploadLimitOptions(this WebApplicationBuilder builder)
    {
        // IFormFile buffers multipart data before the repository sees it. Allow a little
        // more request data than the configured file-content limit for multipart headers.
        builder.Services.AddOptions<KestrelServerOptions>().Configure<AppConfiguration>(
            (options, configuration) =>
                options.Limits.MaxRequestBodySize = configuration.MaxUploadBytes + MULTIPART_OVERHEAD_BYTES);

        builder.Services.AddOptions<FormOptions>().Configure<AppConfiguration>(
            (options, configuration) =>
                options.MultipartBodyLengthLimit = configuration.MaxUploadBytes + MULTIPART_OVERHEAD_BYTES);
    }

    /// <summary>
    /// Reject oversized requests before MVC buffers and binds IFormFile.
    ///     The repository still checks the exact file-content size.
    /// </summary>
    private static void UseConfiguredFileSizeLimitationGuard(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            if (HttpMethods.IsPost(context.Request.Method) &&
                context.Request.Path.Equals("/api/files/upload", StringComparison.OrdinalIgnoreCase) &&
                context.Request.ContentLength is long length &&
                length > app.Services.GetRequiredService<AppConfiguration>().MaxUploadBytes + MULTIPART_OVERHEAD_BYTES)
            {
                context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;

                await context.Response.WriteAsJsonAsync(new
                {
                    error = FileSystemErrors.LimitExceeded.Description,
                    code = FileSystemErrors.LimitExceeded.Code,
                    maxUploadBytes = app.Services
                        .GetRequiredService<AppConfiguration>().MaxUploadBytes,
                    requestTooLarge = true
                });

                return;
            }

            await next(context);
        });
    }
}