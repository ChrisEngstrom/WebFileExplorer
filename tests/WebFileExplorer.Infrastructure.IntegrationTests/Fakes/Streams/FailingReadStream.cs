namespace WebFileExplorer.Infrastructure.IntegrationTests.Fakes.Streams;

// Returns one chunk before failing, so cleanup is checked after bytes were written.
internal sealed class FailingReadStream : MemoryStream
{
    private bool _hasRead;

    public FailingReadStream() : base([7, 8, 9])
    {
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (_hasRead)
        {
            throw new IOException("Simulated source stream failure.");
        }

        _hasRead = true;
        return base.ReadAsync(buffer, cancellationToken);
    }
}
