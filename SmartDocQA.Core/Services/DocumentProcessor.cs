using SmartDocQA.Core.Interfaces;
using SmartDocQA.Core.Models;

namespace SmartDocQA.Core.Services;

public class DocumentProcessor : IDocumentProcessor
{
    private readonly IPdfProcessor _pdfProcessor;
    private readonly IExcelProcessor _excelProcessor;
    private readonly IDocxProcessor _docxProcessor;
    private readonly IImageOcrProcessor _imageProcessor;
    private readonly ICsvProcessor _csvProcessor;

    public DocumentProcessor(
        IPdfProcessor pdfProcessor,
        IExcelProcessor excelProcessor,
        IDocxProcessor docxProcessor,
        IImageOcrProcessor imageProcessor,
        ICsvProcessor csvProcessor)
    {
        _pdfProcessor = pdfProcessor;
        _excelProcessor = excelProcessor;
        _docxProcessor = docxProcessor;
        _imageProcessor = imageProcessor;
        _csvProcessor = csvProcessor;
    }

    public async Task<Document> ProcessAsync(Stream stream, string fileName, string fileType)
    {
        var document = new Document
        {
            FileName = fileName,
            FileType = fileType
        };

        switch (fileType.ToLower())
        {
            case "pdf":
                var pdfResult = await _pdfProcessor.ProcessAsync(stream);
                document.Content = pdfResult.Text;
                document.PageCount = pdfResult.PageCount;
                break;

            case "xlsx":
            case "xls":
                var excelResult = await _excelProcessor.ProcessAsync(stream, fileType);
                document.Content = excelResult.Text;
                document.PageCount = excelResult.Rows.Count;
                document.TabularData = excelResult.Rows;
                break;

            case "csv":
                var csvResult = await _csvProcessor.ProcessAsync(stream);
                document.Content = csvResult.Text;
                document.PageCount = csvResult.Rows.Count;
                document.TabularData = csvResult.Rows;
                break;

            case "docx":
                var docxResult = await _docxProcessor.ProcessAsync(stream);
                document.Content = docxResult.Text;
                document.PageCount = docxResult.PageCount; // paragraph count
                break;

            case "png":
            case "jpg":
            case "jpeg":
                var imageResult = await _imageProcessor.ProcessAsync(stream);
                document.Content = imageResult;
                document.PageCount = imageResult.Length; // extracted text length
                break;

            case "txt":
                {
                    using var reader = new StreamReader(stream);
                    document.Content = await reader.ReadToEndAsync();
                    document.PageCount = document.Content.Length; // character count
                    break;
                }

            default:
                throw new NotSupportedException($"File type {fileType} not supported");
        }

        return document;
    }

    public bool IsSupported(string extension)
    {
        var supported = new[] { "pdf", "docx", "xlsx", "xls", "csv", "txt", "png", "jpg", "jpeg" };
        return supported.Contains(extension.ToLower().TrimStart('.'));
    }
}