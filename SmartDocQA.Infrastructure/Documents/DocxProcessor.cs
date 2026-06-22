using System.IO;
using Xceed.Words.NET;
using SmartDocQA.Core.Interfaces;

namespace SmartDocQA.Infrastructure.Documents;

public class DocxProcessor : IDocxProcessor
{
    public async Task<DocumentResult> ProcessAsync(Stream stream)
    {
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        return await Task.Run(() =>
        {
            using var document = DocX.Load(memoryStream);
            return new DocumentResult
            {
                Text = document.Text,
                PageCount = document.Paragraphs.Count
            };
        });
    }
}
