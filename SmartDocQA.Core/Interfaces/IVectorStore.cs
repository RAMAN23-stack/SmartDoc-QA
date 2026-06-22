using SmartDocQA.Core.Models;

namespace SmartDocQA.Core.Interfaces;

public interface IVectorStore
{
    Task AddDocumentAsync(Document document);
    Task<List<Document>> SearchAsync(string query, int topK = 5);
    Task ClearAsync();
}
