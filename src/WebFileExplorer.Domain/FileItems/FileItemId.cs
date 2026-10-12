using WebFileExplorer.Domain.Paths;

namespace WebFileExplorer.Domain.FileItems;

public sealed record FileItemId
{
    public FileItemId(string value) => Value = RelativePathRules.Validate(value);

    public string Value { get; }
}
