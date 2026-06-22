using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SmartDocQA.Core.Interfaces;
using SmartDocQA.Core.Models;

namespace SmartDocQA.Core.Services;

public class VectorStoreService : IVectorStore
{
    private readonly List<DocumentChunk> _chunks = new();
    private readonly IOpenAIService _openAIService;
    private readonly IGoogleAIService _googleAIService;
    private readonly IOllamaService _ollamaService;
    private readonly DocumentSessionState _sessionState;

    public VectorStoreService(
        IOpenAIService openAIService,
        IGoogleAIService googleAIService,
        IOllamaService ollamaService,
        DocumentSessionState sessionState)
    {
        _openAIService = openAIService;
        _googleAIService = googleAIService;
        _ollamaService = ollamaService;
        _sessionState = sessionState;
    }

    public async Task AddDocumentAsync(Document document)
    {
        // Split text into chunks
        var textChunks = SplitText(document.Content, chunkSize: 1000, chunkOverlap: 200);

        foreach (var text in textChunks)
        {
            var chunk = new DocumentChunk
            {
                DocumentId = document.Id,
                FileName = document.FileName,
                Content = text
            };

            // Attempt to compute embedding
            try
            {
                chunk.Embedding = await GetEmbeddingAsync(text);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating embedding for chunk: {ex.Message}");
                // If embedding fails, we still add the chunk to support TF-IDF search fallback
                chunk.Embedding = null;
            }

            _chunks.Add(chunk);
        }
    }

    public async Task<List<Document>> SearchAsync(string query, int topK = 5)
    {
        if (!_chunks.Any())
        {
            return new List<Document>();
        }

        float[]? queryEmbedding = null;
        try
        {
            queryEmbedding = await GetEmbeddingAsync(query);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Embedding generation failed, falling back to TF-IDF keyword search: {ex.Message}");
        }

        List<DocumentChunk> scoredChunks;

        if (queryEmbedding != null && _chunks.All(c => c.Embedding != null))
        {
            // Semantic Search using Cosine Similarity
            scoredChunks = _chunks
                .Select(chunk => new { Chunk = chunk, Score = CalculateCosineSimilarity(queryEmbedding, chunk.Embedding!) })
                .OrderByDescending(x => x.Score)
                .Take(topK)
                .Select(x => x.Chunk)
                .ToList();
        }
        else
        {
            // Fallback: TF-IDF Keyword Search
            scoredChunks = SearchTfIdf(query, topK);
        }

        // Return matched chunks wrapped as Document objects
        return scoredChunks.Select(c => new Document
        {
            Id = c.DocumentId,
            FileName = c.FileName,
            Content = c.Content,
            FileType = "chunk"
        }).ToList();
    }

    public Task ClearAsync()
    {
        _chunks.Clear();
        return Task.CompletedTask;
    }

    // ================= HELPER METHODS =================

    private async Task<float[]?> GetEmbeddingAsync(string text)
    {
        var provider = _sessionState.SelectedProvider;
        var model = _sessionState.SelectedModel;

        // If the provider is Groq (which doesn't have an embedding model),
        // try to find a configured provider that does have embeddings
        if (provider == AIProvider.Groq)
        {
            // Try Gemini first
            var geminiKey = Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
            if (!string.IsNullOrEmpty(geminiKey))
            {
                return await _googleAIService.GetEmbeddingAsync(text, "text-embedding-004");
            }

            // Try OpenAI
            var openaiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (!string.IsNullOrEmpty(openaiKey))
            {
                return await _openAIService.GetEmbeddingAsync(text, "text-embedding-3-small");
            }

            // Try Ollama
            try
            {
                return await _ollamaService.GetEmbeddingAsync(text, "llama3.2");
            }
            catch
            {
                // Ignore and throw
            }

            throw new InvalidOperationException("No embedding-capable provider keys found (.env) to generate embeddings for Groq session.");
        }

        return provider switch
        {
            AIProvider.OpenAI => await _openAIService.GetEmbeddingAsync(text, model),
            AIProvider.Google => await _googleAIService.GetEmbeddingAsync(text, model),
            AIProvider.Ollama => await _ollamaService.GetEmbeddingAsync(text, model),
            _ => throw new NotSupportedException($"Embeddings not supported for provider {provider}")
        };
    }

    private List<string> SplitText(string text, int chunkSize, int chunkOverlap)
    {
        var chunks = new List<string>();
        if (string.IsNullOrWhiteSpace(text)) return chunks;

        // Simple sentence/word boundaries splitter
        var words = text.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        var currentChunkWords = new List<string>();
        int currentLength = 0;

        for (int i = 0; i < words.Length; i++)
        {
            var word = words[i];
            currentChunkWords.Add(word);
            currentLength += word.Length + 1; // plus space

            if (currentLength >= chunkSize)
            {
                chunks.Add(string.Join(" ", currentChunkWords));
                
                // Backtrack for overlap (roughly 20% of words)
                int overlapWords = (int)(currentChunkWords.Count * ((double)chunkOverlap / chunkSize));
                if (overlapWords > 0 && overlapWords < currentChunkWords.Count)
                {
                    currentChunkWords = currentChunkWords.Skip(currentChunkWords.Count - overlapWords).ToList();
                    currentLength = currentChunkWords.Sum(w => w.Length + 1);
                }
                else
                {
                    currentChunkWords.Clear();
                    currentLength = 0;
                }
            }
        }

        if (currentChunkWords.Any())
        {
            chunks.Add(string.Join(" ", currentChunkWords));
        }

        return chunks;
    }

    private float CalculateCosineSimilarity(float[] vectorA, float[] vectorB)
    {
        if (vectorA.Length != vectorB.Length) return 0f;
        float dotProduct = 0f;
        float normA = 0f;
        float normB = 0f;
        for (int i = 0; i < vectorA.Length; i++)
        {
            dotProduct += vectorA[i] * vectorB[i];
            normA += vectorA[i] * vectorA[i];
            normB += vectorB[i] * vectorB[i];
        }
        if (normA == 0 || normB == 0) return 0f;
        return dotProduct / ((float)Math.Sqrt(normA) * (float)Math.Sqrt(normB));
    }

    // TF-IDF Implementation
    private List<DocumentChunk> SearchTfIdf(string query, int topK)
    {
        var queryTerms = Tokenize(query);
        if (!queryTerms.Any()) return _chunks.Take(topK).ToList();

        // Calculate doc term frequencies
        var docTermFreqs = new List<Dictionary<string, int>>();
        var allTerms = new HashSet<string>();

        foreach (var chunk in _chunks)
        {
            var freqs = new Dictionary<string, int>();
            foreach (var term in Tokenize(chunk.Content))
            {
                freqs[term] = freqs.GetValueOrDefault(term, 0) + 1;
                allTerms.Add(term);
            }
            docTermFreqs.Add(freqs);
        }

        // Calculate Document Frequency (DF) and IDF
        int N = _chunks.Count;
        var idfs = new Dictionary<string, double>();
        foreach (var term in allTerms)
        {
            int df = docTermFreqs.Count(freq => freq.ContainsKey(term));
            idfs[term] = Math.Log((double)N / (1 + df)) + 1;
        }

        // Calculate scores
        var scoredList = new List<(DocumentChunk Chunk, double Score)>();
        for (int i = 0; i < _chunks.Count; i++)
        {
            var chunk = _chunks[i];
            var freqs = docTermFreqs[i];
            double dotProduct = 0;
            double normQuery = 0;
            double normDoc = 0;

            foreach (var term in queryTerms)
            {
                double qWeight = idfs.GetValueOrDefault(term, 0);
                normQuery += qWeight * qWeight;

                if (freqs.TryGetValue(term, out int tf))
                {
                    double dWeight = tf * idfs[term];
                    dotProduct += qWeight * dWeight;
                }
            }

            foreach (var kvp in freqs)
            {
                double dWeight = kvp.Value * idfs.GetValueOrDefault(kvp.Key, 0);
                normDoc += dWeight * dWeight;
            }

            double similarity = 0;
            if (normQuery > 0 && normDoc > 0)
            {
                similarity = dotProduct / (Math.Sqrt(normQuery) * Math.Sqrt(normDoc));
            }

            scoredList.Add((chunk, similarity));
        }

        return scoredList
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .Select(x => x.Chunk)
            .ToList();
    }

    private List<string> Tokenize(string text)
    {
        if (string.IsNullOrEmpty(text)) return new List<string>();
        return Regex.Matches(text.ToLower(), @"\w+")
            .Cast<Match>()
            .Select(m => m.Value)
            .Where(v => v.Length > 2) // ignore small words
            .ToList();
    }

    private class DocumentChunk
    {
        public string DocumentId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public float[]? Embedding { get; set; }
    }
}
