using SmartDocQA.Core.Models;
using System.Threading.Tasks;

namespace SmartDocQA.Core.Interfaces;

public interface ILLMService
{
    Task<string> GetAnswerAsync(string question, string context, AIProvider provider, string model);
    Task<string> GetAnswerWithSystemPromptAsync(string systemPrompt, string userMessage, AIProvider provider, string model);
}

public interface IGroqService
{
    Task<string> GetAnswerAsync(string question, string context, string model, double temperature = 0.0);
}

public interface IOpenAIService
{
    Task<string> GetAnswerAsync(string question, string context, string model, double temperature = 0.0);
    Task<float[]> GetEmbeddingAsync(string text, string model);
}

public interface IGoogleAIService
{
    Task<string> GetAnswerAsync(string question, string context, string model, double temperature = 0.0);
    Task<float[]> GetEmbeddingAsync(string text, string model);
}

public interface IOllamaService
{
    Task<string> GetAnswerAsync(string question, string context, string model, double temperature = 0.0);
    Task<float[]> GetEmbeddingAsync(string text, string model);
}
