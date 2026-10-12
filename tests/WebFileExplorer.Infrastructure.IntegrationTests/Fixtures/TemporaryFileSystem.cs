using WebFileExplorer.Application.Configurations;
using WebFileExplorer.Infrastructure.FileSystem;

namespace WebFileExplorer.Infrastructure.IntegrationTests.Fixtures;

// Each test instance owns an isolated directory and removes it after the test.
internal sealed class TemporaryFileSystem : IDisposable
{
    public string RootPath { get; } = Path.Combine(
        Path.GetTempPath(),
        "explorer-infrastructure-" + Guid.NewGuid().ToString("N"));

    public TemporaryFileSystem()
    {
        Directory.CreateDirectory(RootPath);
    }

    public AppConfiguration Configuration => new () { RootPath = RootPath };

    public PhysicalFileSystem CreateFileSystem() => new (Configuration);

    public void Dispose()
    {
        Directory.Delete(RootPath, recursive: true);
    }
}
