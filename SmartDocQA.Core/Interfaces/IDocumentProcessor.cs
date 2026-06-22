using SmartDocQA.Core.Models;

namespace SmartDocQA.Core.Interfaces;

public interface IDocumentProcessor
{
    Task<Document> ProcessAsync(Stream stream, string fileName, string fileType);
    bool IsSupported(string extension);
}

public interface IPdfProcessor
{
    Task<DocumentResult> ProcessAsync(Stream stream);
}

public interface IExcelProcessor
{
    Task<DocumentResult> ProcessAsync(Stream stream, string extension);
}

public interface IDocxProcessor
{
    Task<DocumentResult> ProcessAsync(Stream stream);
}

public interface ICsvProcessor
{
    Task<DocumentResult> ProcessAsync(Stream stream);
}

public interface IImageOcrProcessor
{
    Task<string> ProcessAsync(Stream stream);
}

public class DocumentResult
{
    public string Text { get; set; } = string.Empty;
    public List<DataRow> Rows { get; set; } = new();
    public int PageCount { get; set; }
}
