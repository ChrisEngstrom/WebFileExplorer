using Microsoft.Extensions.DependencyInjection;
using WebFileExplorer.Application;
using WebFileExplorer.Application.Abstractions.Messaging;
using WebFileExplorer.Application.Configurations;
using WebFileExplorer.Application.FileSystemManagement.Commands.UploadFile;
using WebFileExplorer.SharedKernel;

namespace WebFileExplorer.Infrastructure.IntegrationTests.FileSystemManagement;

public sealed class UploadFileTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "explorer-reference-" + Guid.NewGuid().ToString("N"));

    private ServiceProvider Provider => new ServiceCollection()
        .AddLogging()
        .AddSingleton(new AppConfiguration { RootPath = _root, MaxUploadBytes = 4 })
        .AddApplication()
        .AddInfrastructure()
        .BuildServiceProvider();

    public UploadFileTests()
    {
        Directory.CreateDirectory(Path.Combine(
            _root,
            "Photos",
            "Again"));

        File.WriteAllBytes(
            Path.Combine(
                _root,
                "sample.txt"),
            new byte[13]);

        File.WriteAllBytes(
            Path.Combine(
                _root,
                "Photos",
                "Again",
                "sample2.txt"),
            new byte[58]);
    }

    [Fact]
    public async Task Handle_Should_RemovePartialFile_When_UploadSizeExceedsLimit()
    {
        // Arrange
        using var provider = Provider;
        using var content = new MemoryStream(new byte[5]);

        var uploadCommandHandler = provider.GetRequiredService<ICommandHandler<UploadCommand>>();

        // Act
        var result = await uploadCommandHandler.Handle(
            new UploadCommand(
                "Photos",
                "oversized.bin",
                content),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.LimitExceeded, result.Error.Type);
        Assert.False(File.Exists(Path.Combine(
            _root,
            "Photos",
            "oversized.bin")));
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);
}
