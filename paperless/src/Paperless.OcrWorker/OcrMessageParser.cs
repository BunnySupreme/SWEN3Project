using System.Text.Json;
using log4net;

namespace Paperless.OcrWorker;

public static class OcrMessageParser
{
    public static (Guid DocumentId, string? Title) Parse(string message, ILog logger)
    {
        Guid docId = Guid.Empty;
        string? title = null;

        try
        {
            using var doc = JsonDocument.Parse(message);
            var root = doc.RootElement;

            if (root.TryGetProperty("DocumentId", out var idProp) &&
                idProp.ValueKind == JsonValueKind.String &&
                Guid.TryParse(idProp.GetString(), out var parsed))
            {
                docId = parsed;
            }

            if (root.TryGetProperty("DocumentTitle", out var titleProp))
            {
                title = titleProp.GetString();
            }
        }
        catch (JsonException ex)
        {
            logger.Warn($"OCR worker received non-JSON message: {ex.Message}");
        }

        return (docId, title);
    }
}
