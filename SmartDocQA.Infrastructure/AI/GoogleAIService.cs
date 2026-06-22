using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using SmartDocQA.Core.Interfaces;

namespace SmartDocQA.Infrastructure.AI;

public class GoogleAIService : IGoogleAIService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public GoogleAIService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    private string GetApiKey()
    {
        var apiKey = Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            apiKey = _configuration["AI:Google:ApiKey"];
        }
        return apiKey ?? string.Empty;
    }

    public async Task<string> GetAnswerAsync(string question, string context, string model, double temperature = 0.0)
    {
        var apiKey = GetApiKey();
        if (string.IsNullOrEmpty(apiKey))
        {
            return "Error: GOOGLE_API_KEY is not configured. Please set it in your .env file or appsettings.json.";
        }

        // Clean model name
        var modelName = string.IsNullOrEmpty(model) ? "gemini-2.0-flash-exp" : model;
        
        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = $"You are a helpful assistant. Answer based on the provided context.\n\nContext:\n{context}\n\nQuestion: {question}" }
                    }
                }
            },
            generationConfig = new
            {
                temperature = temperature
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={apiKey}";

        var response = await _httpClient.PostAsync(url, content);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            return $"Google Gemini API Error: {response.StatusCode} - {err}";
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseContent);
        var root = doc.RootElement;

        if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
        {
            var candidate = candidates[0];
            if (candidate.TryGetProperty("content", out var contentElem))
            {
                if (contentElem.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                {
                    var part = parts[0];
                    if (part.TryGetProperty("text", out var text))
                    {
                        return text.GetString() ?? "No content returned.";
                    }
                }
            }
        }

        return "Error parsing Google Gemini response.";
    }

    public async Task<float[]> GetEmbeddingAsync(string text, string model)
    {
        var apiKey = GetApiKey();
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("GOOGLE_API_KEY is not configured.");
        }

        var embeddingModel = "text-embedding-004";
        if (model.Contains("embed"))
        {
            embeddingModel = model;
        }

        var requestBody = new
        {
            content = new
            {
                parts = new[]
                {
                    new { text = text }
                }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{embeddingModel}:embedContent?key={apiKey}";

        var response = await _httpClient.PostAsync(url, content);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            throw new Exception($"Google Gemini Embedding API Error: {response.StatusCode} - {err}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseContent);
        var root = doc.RootElement;

        if (root.TryGetProperty("embedding", out var embedding) && embedding.TryGetProperty("values", out var values))
        {
            var list = new List<float>();
            foreach (var element in values.EnumerateArray())
            {
                list.Add(element.GetSingle());
            }
            return list.ToArray();
        }

        throw new Exception("Error parsing Google Gemini embedding response.");
    }
}
