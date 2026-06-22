using System;
using System.Collections.Generic;
using System.Data;

namespace SmartDocQA.Core.Models;

public class DocumentSessionState
{
    public List<Document> ExtractedDocuments { get; private set; } = new();
    public Document? TabularDocument { get; private set; }
    public DataTable? TabularData { get; private set; }
    public string? TabularFileType { get; private set; }
    public DateTime? LastUploadTime { get; private set; }

    // Image session state
    public Document? ImageDocument { get; private set; }
    public string? ImageBase64 { get; private set; }       // raw image bytes as base64
    public string? ImageMimeType { get; private set; }     // e.g. "image/png"
    public bool IsImageLoaded => ImageDocument != null && !string.IsNullOrEmpty(ImageBase64);

    // Selected AI Configuration
    public AIProvider SelectedProvider { get; set; } = AIProvider.Groq;
    public string SelectedModel { get; set; } = "llama-3.3-70b-versatile";
    public double Temperature { get; set; } = 0.0;

    public event Action? OnStateChanged;

    public void NotifyStateChanged() => OnStateChanged?.Invoke();

    public void SetTabularData(Document document, DataTable dataTable, string fileType)
    {
        TabularDocument = document;
        TabularData = dataTable;
        TabularFileType = fileType;
        LastUploadTime = DateTime.Now;
        NotifyStateChanged();
    }

    public void AddExtractedDocument(Document document)
    {
        ExtractedDocuments.Add(document);
        LastUploadTime = DateTime.Now;
        NotifyStateChanged();
    }

    public void SetImageDocument(Document document, string base64Data, string mimeType)
    {
        ImageDocument = document;
        ImageBase64 = base64Data;
        ImageMimeType = mimeType;
        LastUploadTime = DateTime.Now;
        NotifyStateChanged();
    }

    public void ClearAll()
    {
        ExtractedDocuments.Clear();
        TabularDocument = null;
        TabularData = null;
        TabularFileType = null;
        ImageDocument = null;
        ImageBase64 = null;
        ImageMimeType = null;
        LastUploadTime = null;
        NotifyStateChanged();
    }
}
