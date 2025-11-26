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
        public async Task<string> ExtractTextAsync(Stream pdfStream, CancellationToken ct = default)
        {
            using var pdfBytes = new MemoryStream();
            await pdfStream.CopyToAsync(pdfBytes, ct);

            var tempPdf = Path.GetTempFileName();
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var outputPattern = Path.Combine(tempDir, "page-%03d.png");

            try
            {
                await File.WriteAllBytesAsync(tempPdf, pdfBytes.ToArray(), ct);

                // Ghostscript: convert PDF to PNG (one image per page)
                var ghostScript = new ProcessStartInfo
                {
                    FileName = "gs",
                    Arguments = $"-dNOPAUSE -dBATCH -sDEVICE=png16m -r300 -sOutputFile={outputPattern} {tempPdf}",
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };

                using (var process = Process.Start(ghostScript))
                {
                    await process!.WaitForExitAsync();
                    if (process.ExitCode != 0)
                        throw new Exception("Ghostscript failed: " + await process.StandardError.ReadToEndAsync(ct));
                }

                // OCR each page using Tesseract and concatenate results
                var pngFiles = Directory.GetFiles(tempDir, "page-*.png").OrderBy(f => f).ToList();
                var fullTextBuilder = new System.Text.StringBuilder();

                using var engine = new TesseractEngine(_tessdataPath, "deu", EngineMode.Default);

                foreach (var pngFile in pngFiles)
                {
                    using var img = Pix.LoadFromFile(pngFile);
                    using var page = engine.Process(img);
                    fullTextBuilder.AppendLine(page.GetText());
                    fullTextBuilder.AppendLine(); // Page separator
                }

                return fullTextBuilder.ToString();
            }
            finally
            {
                // Clean up temporary files and directories
                if (File.Exists(tempPdf))
                    File.Delete(tempPdf);
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }
    }
}
