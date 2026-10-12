namespace WebFileExplorer.Application.Configurations;

public class AppConfiguration
{
    public string RootPath { get; set; } = null!;

    // These defaults are used when tests construct this class directly.
    public long MaxUploadBytes { get; set; } = 25L * 1024 * 1024; // 25MiB

    public int MaxScannedItems { get; set; } = 10_000;
}
