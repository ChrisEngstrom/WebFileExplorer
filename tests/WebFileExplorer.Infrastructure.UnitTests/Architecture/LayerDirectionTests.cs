using WebFileExplorer.Application.FileItems;
using WebFileExplorer.Domain.FileItems;

namespace WebFileExplorer.Infrastructure.UnitTests.Architecture;

public sealed class LayerDirectionTests
{
    [Fact]
    public void InnerLayers_Should_NotReferenceOuterLayers()
    {
        // Act
        var domainReferences = typeof(FileItem).Assembly
            .GetReferencedAssemblies()
            .Select(x => x.Name)
            .ToArray();
        var applicationReferences = typeof(IFileItemRepository).Assembly
            .GetReferencedAssemblies()
            .Select(x => x.Name)
            .ToArray();
        var infrastructureReferences = typeof(ConfigureServices).Assembly
            .GetReferencedAssemblies()
            .Select(x => x.Name)
            .ToArray();

        // Assert
        Assert.DoesNotContain("WebFileExplorer.Application", domainReferences);
        Assert.DoesNotContain("WebFileExplorer.Infrastructure", domainReferences);
        Assert.DoesNotContain("WebFileExplorer.WebApp", domainReferences);
        Assert.DoesNotContain("WebFileExplorer.Infrastructure", applicationReferences);
        Assert.DoesNotContain("WebFileExplorer.WebApp", applicationReferences);
        Assert.DoesNotContain("WebFileExplorer.WebApp", infrastructureReferences);
    }
}
