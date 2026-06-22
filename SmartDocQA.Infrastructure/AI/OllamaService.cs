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

public class OllamaService : IOllamaService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public OllamaService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    private string GetBaseUrl()
    {
        var baseUrl = _configuration["AI:Ollama:BaseUrl"];
        if (string.IsNullOrEmpty(baseUrl))
        {
            baseUrl = "http://localhost:11434";
        }
        return baseUrl.TrimEnd('/');
    }

    public async Task<string> GetAnswerAsync(string question, string context, string model, double temperature = 0.0)
    {
        var baseUrl = GetBaseUrl();
        var modelName = string.IsNullOrEmpty(model) ? "llama3.2" : model;

        var requestBody = new
        {
            model = modelName,
            messages = new[]
            {
                new { role = "system", content = string.IsNullOrWhiteSpace(context)
                    ? "You are a helpful assistant. Answer based on the provided context."
                    : context },
                new { role = "user", content = question }
            },
            options = new
            {
                temperature = temperature
            },
            stream = false
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var url = $"{baseUrl}/api/chat";

        try
        {
            var response = await _httpClient.PostAsync(url, content);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                return $"Ollama API Error: {response.StatusCode} - {err}. Ensure Ollama is running locally!";
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseContent);
            var root = doc.RootElement;

            if (root.TryGetProperty("message", out var message) && message.TryGetProperty("content", out var text))
            {
                return text.GetString() ?? "No content returned.";
            }

            return "Error parsing Ollama response.";
        }
        catch (Exception ex)
        {
            return $"Error calling Ollama API: {ex.Message}. Make sure Ollama is running on {baseUrl}!";
        }
    }

    public async Task<float[]> GetEmbeddingAsync(string text, string model)
    {
        var baseUrl = GetBaseUrl();
        var modelName = string.IsNullOrEmpty(model) ? "llama3.2" : model;

        var requestBody = new
        {
            model = modelName,
            prompt = text
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var url = $"{baseUrl}/api/embeddings";

        try
        {
            var response = await _httpClient.PostAsync(url, content);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                throw new Exception($"Ollama Embedding API Error: {response.StatusCode} - {err}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseContent);
            var root = doc.RootElement;

            if (root.TryGetProperty("embedding", out var embedding))
            {
                var list = new List<float>();
                foreach (var element in embedding.EnumerateArray())
                {
                    list.Add(element.GetSingle());
                }
                return list.ToArray();
            }

            throw new Exception("Error parsing Ollama embedding response.");
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to compute Ollama embedding: {ex.Message}");
        }
    }
}
