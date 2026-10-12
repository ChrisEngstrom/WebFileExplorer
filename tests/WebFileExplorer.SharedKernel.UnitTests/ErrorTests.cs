namespace WebFileExplorer.SharedKernel.UnitTests;

public class ErrorTests
{
    [Fact]
    public void Failure_Should_PreserveDetails_When_ErrorIsCreated()
    {
        // Arrange/Act
        var error = Error.Failure("Test.Failure", "An unexpected failure.");

        // Assert
        Assert.Equal("Test.Failure", error.Code);
        Assert.Equal("An unexpected failure.", error.Description);
        Assert.Equal(ErrorType.Failure, error.Type);
    }

    [Fact]
    public void Problem_Should_PreserveDetails_When_ErrorIsCreated()
    {
        // Arrange/Act
        var error = Error.Problem("Test.Problem", "A supplied problem.");

        // Assert
        Assert.Equal("Test.Problem", error.Code);
        Assert.Equal("A supplied problem.", error.Description);
        Assert.Equal(ErrorType.Problem, error.Type);
    }
}
