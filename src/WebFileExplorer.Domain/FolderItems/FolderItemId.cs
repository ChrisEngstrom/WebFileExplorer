using WebFileExplorer.Domain.Paths;

namespace WebFileExplorer.Domain.FolderItems;

public sealed record FolderItemId
{
    public FolderItemId(string value) => Value = RelativePathRules.Validate(value, true);

    public string Value { get; }
}
