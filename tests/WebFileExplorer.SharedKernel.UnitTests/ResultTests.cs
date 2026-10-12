namespace WebFileExplorer.SharedKernel.UnitTests;

public sealed class ResultTests
{
    [Fact]
    public void ImplicitConversion_Should_ExposeValue_When_ValueIsNotNull()
    {
        // Arrange
        string value = "file.txt";

        // Act
        Result<string> result = value;

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(Error.None, result.Error);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void Failure_Should_PreserveError_When_ResultHasNoValue()
    {
        // Arrange
        var error = Error.Failure("Test.Failure", "A supplied failure.");

        // Act
        Result result = Result.Failure(error);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void Success_Should_HaveNoError()
    {
        // Arrange/Act
        Result result = Result.Success();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Success_WithValue_Should_HaveNoErrorAndExposeValue()
    {
        // Arrange/Act
        Result<int> result = Result.Success(42);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Failure_Should_KeepErrorAndRejectValueAccess()
    {
        // Arrange
        Error error = Error.NotFound("MissingFile", "Missing file");

        // Act
        Result<int> result = Result.Failure<int>(error);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Failure_Should_RejectNoneError()
    {
        // Arrange/Act/Assert
        Assert.Throws<ArgumentException>(() => Result.Failure(Error.None));
    }

    [Fact]
    public void ValidationFailure_Should_ReturnValidationErrorProvided()
    {
        // Arrange
        var expectedErrorCode = "Path.Invalid";
        var expectedErrorDescription = "Invalid path or file name.";
        var validationError = Error.Validation(expectedErrorCode, expectedErrorDescription);

        // Act
        Result<int> result = Result<int>.ValidationFailure(validationError);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(validationError.Type, result.Error.Type);
        Assert.Equal(validationError.Code, result.Error.Code);
        Assert.Equal(validationError.Description, result.Error.Description);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void ImplicitConversion_Should_ReturnNullValueFailure_When_ValueIsNull()
    {
        // Arrange/Act
        Result<string> result = (string?)null;

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(Error.NullValue, result.Error);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }
}
