namespace SmartDocQA.Core.Models;

public class Document
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public int PageCount { get; set; }
    public List<DataRow> TabularData { get; set; } = new();
}

public class DataRow
{
    public Dictionary<string, object> Columns { get; set; } = new();
}
