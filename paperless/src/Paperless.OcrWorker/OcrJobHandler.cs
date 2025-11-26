using log4net;

namespace Paperless.OcrWorker;

public sealed class OcrJobHandler
{
    private readonly IObjectStore _store;
    private readonly IOcrEngine _ocr;
    private readonly ILog _logger;

    public OcrJobHandler(IObjectStore store, IOcrEngine ocr, ILog logger)
    {
        _store = store;
        _ocr = ocr;
        _logger = logger;
    }

    public OcrJobHandler(IObjectStore store, IOcrEngine ocr)
    {
        _store = store;
        _ocr = ocr;
    }

    public async Task<(Guid DocumentId, string Summary)> HandleAsync(
        Guid documentId,
        string? title,
        CancellationToken ct)
    {
        if (documentId == Guid.Empty)
        {
            _logger.Warn("Received message without valid DocumentId.");
            return (Guid.Empty, string.Empty);
        }

        await using var pdfStream = await _store.GetDocumentAsync(documentId, ct);

        var text = await _ocr.ExtractTextAsync(pdfStream, ct);
        var summary = BuildSummary(text, title);

        _logger.Info($"OCR result created for document {documentId}");

        return (documentId, summary);
    }

    // currently does nothing, later Gemini
    private static string BuildSummary(string text, string? title) => text;
}
