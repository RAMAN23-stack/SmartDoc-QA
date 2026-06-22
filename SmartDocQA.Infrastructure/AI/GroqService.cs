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

public class GroqService : IGroqService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private const string GroqApiUrl = "https://api.groq.com/openai/v1/chat/completions";

    public GroqService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<string> GetAnswerAsync(string question, string context, string model, double temperature = 0.0)
    {
        var apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            apiKey = _configuration["AI:Groq:ApiKey"];
        }

        if (string.IsNullOrEmpty(apiKey))
        {
            return "Error: GROQ_API_KEY is not configured. Please set it in your .env file or appsettings.json.";
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
            temperature = temperature,
            max_tokens = 2000
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, GroqApiUrl)
        {
            Content = content,
            Headers = { { "Authorization", $"Bearer {apiKey}" } }
        };

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            return $"Groq API Error: {response.StatusCode} - {err}";
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<GroqResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return result?.Choices?.FirstOrDefault()?.Message?.Content ?? "No response from Groq AI";
    }
}

public class GroqResponse
{
    public List<Choice> Choices { get; set; } = new();
}

public class Choice
{
    public Message Message { get; set; } = new();
}

public class Message
{
    public string Content { get; set; } = string.Empty;
}