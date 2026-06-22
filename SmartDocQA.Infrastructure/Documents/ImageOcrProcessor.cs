using System;
using System.Diagnostics;
using System.IO;
using SmartDocQA.Core.Interfaces;

namespace SmartDocQA.Infrastructure.Documents;

public class ImageOcrProcessor : IImageOcrProcessor
{
    public async Task<string> ProcessAsync(Stream stream)
    {
        string tesseractPath = @"C:\Program Files\Tesseract-OCR\tesseract.exe";
        if (!File.Exists(tesseractPath))
        {
            tesseractPath = "tesseract.exe"; // Fallback to PATH
        }

        var tempInput = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".png");
        var tempOutputBase = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var tempOutputFile = tempOutputBase + ".txt";

        try
        {
            // Save stream to temp file
            using (var fileStream = File.Create(tempInput))
            {
                await stream.CopyToAsync(fileStream);
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = tesseractPath,
                Arguments = $"\"{tempInput}\" \"{tempOutputBase}\" -l eng",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                if (process != null)
                {
                    await process.WaitForExitAsync();
                }
            }

            if (File.Exists(tempOutputFile))
            {
                var text = await File.ReadAllTextAsync(tempOutputFile);
                return text;
            }

            return string.Empty;
        }
        catch (Exception ex)
        {
            return $"OCR processing failed: {ex.Message}";
        }
        finally
        {
            try { if (File.Exists(tempInput)) File.Delete(tempInput); } catch {}
            try { if (File.Exists(tempOutputFile)) File.Delete(tempOutputFile); } catch {}
        }
    }
}
