using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using WebFileExplorer.WebApp.IntegrationTests.Fixtures;

namespace WebFileExplorer.WebApp.IntegrationTests.Middleware;

public class ApiRequestLoggingMiddlewareTests
{
    [Fact]
    public async Task ApiRequest_Should_LogOneSafeSummary_When_RequestSucceeds()
    {
        // Arrange
        using var factory = new FileExplorerWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Act
        using var response = await client.GetAsync(
            new Uri("/api/files?path=Photos&q=private-sample", UriKind.Relative),
            TestContext.Current.CancellationToken);
        var summaries = factory.Logs.Entries
            .Where(item => item.Category == "WebFileExplorer.WebApp.RequestSummary" &&
                item.Level == LogLevel.Information)
            .ToArray();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var summary = Assert.Single(summaries);
        Assert.Contains("GET", summary.Text);
        Assert.Contains("200", summary.Text);
        Assert.DoesNotContain("private-sample", summary.Text);
        Assert.DoesNotContain(factory.RootPath, summary.Text);
    }
}
