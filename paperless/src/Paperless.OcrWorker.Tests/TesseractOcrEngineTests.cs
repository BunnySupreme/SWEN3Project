using System.IO;
using System.Threading.Tasks;
using Xunit;
using Paperless.OcrWorker;
using System.Runtime.InteropServices;

public class TesseractOcrEngineTests
{
    [Fact]
    public async Task ExtractTextAsync_ReturnsText_ForSimplePdf()
    {
        var pdfPath = Path.Combine("TestData", "Hallo.pdf");
        await using var pdf = File.OpenRead(pdfPath);

        var engine = new TesseractOcrEngine();
        var text = await engine.ExtractTextAsync(pdf);

        Assert.False(string.IsNullOrWhiteSpace(text));
        Assert.Contains("Hallo", text, StringComparison.OrdinalIgnoreCase);
    }
}