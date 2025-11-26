using Minio.DataModel.Args;
using Minio;

namespace Paperless.OcrWorker;

public interface IObjectStore
{
    Task<Stream> GetDocumentAsync(Guid id, CancellationToken ct = default);
}

public sealed class MinioObjectStore : IObjectStore
{
    private readonly IMinioClient _minio;

    public MinioObjectStore(IMinioClient minio)
    {
        _minio = minio;
    }

    public async Task<Stream> GetDocumentAsync(Guid id, CancellationToken ct = default)
    {
        var ms = new MemoryStream();

        await _minio.GetObjectAsync(
            new GetObjectArgs()
                .WithBucket("documents")
                .WithObject(id.ToString())
                .WithCallbackStream(stream => stream.CopyTo(ms)),
            ct);

        ms.Position = 0;
        return ms;
    }
}
