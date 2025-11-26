using System.Diagnostics;

namespace Paperless.OcrWorker
{
    public sealed class TesseractOcrEngine : IOcrEngine
    {
        public async Task<string> ExtractTextAsync(Stream pdfStream, CancellationToken ct = default)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "paperless-ocr", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            var pdfPath = Path.Combine(tempDir, "input.pdf");
            var imgPath = Path.Combine(tempDir, "page.png");

            try
            {
                // Write PDF to disk
                await using (var fs = File.Create(pdfPath))
                    await pdfStream.CopyToAsync(fs, ct);

                // Ghostscript: PDF -> PNG (first page only)
                var gs = new ProcessStartInfo
                {
                    FileName = "gs",
                    Arguments = $"-dSAFER -dBATCH -dNOPAUSE -sDEVICE=png16m -dFirstPage=1 -dLastPage=1 -r200 -sOutputFile=\"{imgPath}\" \"{pdfPath}\"",
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };

                using (var gsProc = Process.Start(gs)!)
                {
                    await gsProc.WaitForExitAsync(ct);
                    if (gsProc.ExitCode != 0)
                        throw new InvalidOperationException("Ghostscript failed.");
                }

                // Tesseract: PNG -> text
                var tess = new ProcessStartInfo
                {
                    FileName = "tesseract",
                    Arguments = $"\"{imgPath}\" stdout -l eng",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };

                using var tessProc = Process.Start(tess)!;
                var text = await tessProc.StandardOutput.ReadToEndAsync(ct);
                await tessProc.WaitForExitAsync(ct);

                if (tessProc.ExitCode != 0)
                    throw new InvalidOperationException("Tesseract failed.");

                return text;
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch { /* ignore */ }
            }
        }
    }
}
