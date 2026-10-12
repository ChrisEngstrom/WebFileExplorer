using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using WebFileExplorer.WebApp.Controllers;

namespace WebFileExplorer.WebApp.IntegrationTests.Startup;

public sealed class AppConfigurationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Startup_Should_Throw_When_RootPathIsMissingOrWhitespace(string? rootPath)
    {
        // Arrange
        using var webApplicationFactory = new WebApplicationFactory<FilesController>();
        using var factory = webApplicationFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FileBrowser:RootPath"] = rootPath
                });
            });
        });

        // Act
        var actual = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());

        // Assert
        Assert.Equal("Configure FileBrowser:RootPath.", actual.Message);
    }

    [Fact]
    public void Startup_Should_Throw_When_RootDirectoryDoesNotExist()
    {
        // Arrange
        string rootPath = Path.Combine(
            Path.GetTempPath(),
            "WebFileExplorer.StartupTests",
            Guid.NewGuid().ToString("N"));

        using var webApplicationFactory = new WebApplicationFactory<FilesController>();
        using var factory = webApplicationFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FileBrowser:RootPath"] = rootPath
                });
            });
        });

        // Act/Assert
        var actual = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());
        Assert.False(Directory.Exists(rootPath));
        Assert.Equal(
            "Configured FileBrowser:RootPath directory does not exist.",
            actual.Message);
    }
}
