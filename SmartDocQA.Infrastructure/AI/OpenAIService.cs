using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using SmartDocQA.Core.Interfaces;

namespace SmartDocQA.Infrastructure.AI;

public class OpenAIService : IOpenAIService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private const string ChatUrl = "https://api.openai.com/v1/chat/completions";
    private const string EmbeddingUrl = "https://api.openai.com/v1/embeddings";

    public OpenAIService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    private string GetApiKey()
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            apiKey = _configuration["AI:OpenAI:ApiKey"];
        }
        return apiKey ?? string.Empty;
    }

    public async Task<string> GetAnswerAsync(string question, string context, string model, double temperature = 0.0)
    {
        var apiKey = GetApiKey();
        if (string.IsNullOrEmpty(apiKey))
        {
            return "Error: OPENAI_API_KEY is not configured. Please set it in your .env file or appsettings.json.";
        }

        var requestBody = new
        {
            model = model,
            messages = new[]
            {
                new { role = "system", content = string.IsNullOrWhiteSpace(context)
                    ? "You are a helpful assistant. Answer based on the provided context."
                    : context },
                new { role = "user", content = question }
            },
            temperature = temperature
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, ChatUrl)
        {
            Content = content,
            Headers = { { "Authorization", $"Bearer {apiKey}" } }
        };

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            return $"OpenAI Chat API Error: {response.StatusCode} - {err}";
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseContent);
        var root = doc.RootElement;
        
        if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
        {
            var choice = choices[0];
            if (choice.TryGetProperty("message", out var message) && message.TryGetProperty("content", out var text))
            {
                return text.GetString() ?? "No content returned.";
            }
        }

        return "Error parsing OpenAI response.";
    }

    public async Task<float[]> GetEmbeddingAsync(string text, string model)
    {
        var apiKey = GetApiKey();
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("OPENAI_API_KEY is not configured.");
        }

        // Default embedding model for OpenAI if none specified or if the model looks like a chat model
        var embeddingModel = "text-embedding-3-small";
        if (model.Contains("embed"))
        {
            embeddingModel = model;
        }

        var requestBody = new
        {
            model = embeddingModel,
            input = text
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, EmbeddingUrl)
        {
            Content = content,
            Headers = { { "Authorization", $"Bearer {apiKey}" } }
        };

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            throw new Exception($"OpenAI Embedding API Error: {response.StatusCode} - {err}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseContent);
        var root = doc.RootElement;

        if (root.TryGetProperty("data", out var data) && data.GetArrayLength() > 0)
        {
            var first = data[0];
            if (first.TryGetProperty("embedding", out var embedding))
            {
                var list = new List<float>();
                foreach (var element in embedding.EnumerateArray())
                {
                    list.Add(element.GetSingle());
                }
                return list.ToArray();
            }
        }

        throw new Exception("Error parsing OpenAI embedding response.");
    }
}
