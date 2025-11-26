using System.Diagnostics;
using Tesseract;

namespace Paperless.OcrWorker
{
    public sealed class TesseractOcrEngine : IOcrEngine
    {
        private readonly string _tessdataPath;

        public TesseractOcrEngine()
        {
            _tessdataPath = Path.Combine(AppContext.BaseDirectory, "tessdata");
        }
        public Task<string> ExtractTextAsync(Stream pdfStream, CancellationToken ct = default)
        {
            using var pdfBytes = new MemoryStream();
            pdfStream.CopyTo(pdfBytes);

            // Ghostscript: convert PDF → PNG
            var tempPdf = Path.GetTempFileName();
            var tempPng = tempPdf + ".png";

            File.WriteAllBytes(tempPdf, pdfBytes.ToArray());

            // Ghostscript
            var ghostScript = new ProcessStartInfo
            {
                FileName = "gs",
                Arguments = $"-dNOPAUSE -dBATCH -sDEVICE=png16m -r300 -sOutputFile={tempPng} {tempPdf}",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            using (var process = Process.Start(ghostScript))
            {
                process!.WaitForExit();
                if (process.ExitCode != 0)
                    throw new Exception("Ghostscript failed: " + process.StandardError.ReadToEnd());
            }

            // OCR using Tesseract native engine
            using var engine = new TesseractEngine(_tessdataPath, "deu", EngineMode.Default);
            using var img = Pix.LoadFromFile(tempPng);
            using var page = engine.Process(img);

            var text = page.GetText();

            File.Delete(tempPdf);
            File.Delete(tempPng);

            return Task.FromResult(text);
        }
    }
}
