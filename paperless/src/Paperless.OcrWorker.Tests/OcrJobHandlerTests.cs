using log4net;
using log4net.Core;
using Paperless.OcrWorker;
using System.Text;

public class OcrJobHandlerTests
{
    private sealed class FakeStore : IObjectStore
    {
        public Task<Stream> GetDocumentAsync(Guid id, CancellationToken ct = default)
        {
            var ms = new MemoryStream(Encoding.UTF8.GetBytes("FAKE PDF BYTES"));
            return Task.FromResult<Stream>(ms);
        }
    }

    private sealed class FakeOcrEngine : IOcrEngine
    {
        public Task<string> ExtractTextAsync(Stream pdfStream, CancellationToken ct = default)
            => Task.FromResult("FAKE OCR TEXT");
    }

    [Fact]
    public async Task HandleAsync_Uses_OcrEngine_And_Returns_Summary()
    {

        var logger = new FakeLogger();
        var handler = new OcrJobHandler(new FakeStore(), new FakeOcrEngine(), logger);
        var id = Guid.NewGuid();

        var result = await handler.HandleAsync(id, "Title", CancellationToken.None);

        Assert.Equal(id, result.DocumentId);
        Assert.Equal("FAKE OCR TEXT", result.Summary);
    }
}
