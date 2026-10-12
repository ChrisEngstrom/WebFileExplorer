using System.Globalization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WebFileExplorer.WebApp.Controllers;

namespace WebFileExplorer.WebApp.IntegrationTests.Fixtures;

internal sealed class FileExplorerWebApplicationFactory : WebApplicationFactory<FilesController>
{
    private readonly Dictionary<string, string?> _settings;

    public string RootPath { get; } = Path.Combine(
        Path.GetTempPath(),
        "explorer-http-" + Guid.NewGuid().ToString("N"));

    public CaptureLoggerProvider Logs { get; } = new ();

    public FileExplorerWebApplicationFactory(int? maxScannedItems = null, long? maxUploadBytes = null)
    {
        Directory.CreateDirectory(Path.Combine(RootPath, "Photos"));
        File.WriteAllBytes(Path.Combine(RootPath, "sample.txt"), new byte[13]);
        File.WriteAllBytes(
            Path.Combine(
                RootPath,
                "Photos",
                "sample2.txt"),
            new byte[58]);
        _settings = new Dictionary<string, string?> { ["FileBrowser:RootPath"] = RootPath };

        if (maxScannedItems is not null)
        {
            _settings["FileBrowser:MaxScannedItems"] = maxScannedItems.Value.ToString(CultureInfo.InvariantCulture);
        }

        if (maxUploadBytes is not null)
        {
            _settings["FileBrowser:MaxUploadBytes"] = maxUploadBytes.Value.ToString(CultureInfo.InvariantCulture);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(_settings));
        builder.ConfigureLogging(logging => logging.AddProvider(Logs));
    }

    protected override void Dispose(bool disposing)
    {
        // Stop the host before deleting the data directory it was using.
        base.Dispose(disposing);
        if (disposing && Directory.Exists(RootPath))
        {
            Directory.Delete(RootPath, recursive: true);
        }
    }
}
