using System.IO;
using System.Text;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using SmartDocQA.Core.Interfaces;

namespace SmartDocQA.Infrastructure.Documents;

public class PdfProcessor : IPdfProcessor
{
    public async Task<DocumentResult> ProcessAsync(Stream stream)
    {
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        return await Task.Run(() =>
        {
            var result = new DocumentResult();
            var textBuilder = new StringBuilder();

            using var reader = new PdfReader(memoryStream);
            using var pdfDoc = new PdfDocument(reader);
            var pageCount = pdfDoc.GetNumberOfPages();

            for (int i = 1; i <= pageCount; i++)
            {
                var page = pdfDoc.GetPage(i);
                var strategy = new SimpleTextExtractionStrategy();
                var text = PdfTextExtractor.GetTextFromPage(page, strategy);
                textBuilder.AppendLine(text);
            }

            result.Text = textBuilder.ToString();
            result.PageCount = pageCount;

            return result;
        });
    }
}