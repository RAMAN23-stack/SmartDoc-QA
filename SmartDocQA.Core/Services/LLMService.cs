using SmartDocQA.Core.Interfaces;
using SmartDocQA.Core.Models;

namespace SmartDocQA.Core.Services;

public class LLMService : ILLMService
{
    private readonly IGroqService _groqService;
    private readonly IOpenAIService _openAIService;
    private readonly IGoogleAIService _googleAIService;
    private readonly IOllamaService _ollamaService;
    private readonly DocumentSessionState _sessionState;

    public LLMService(
        IGroqService groqService,
        IOpenAIService openAIService,
        IGoogleAIService googleAIService,
        IOllamaService ollamaService,
        DocumentSessionState sessionState)
    {
        _groqService = groqService;
        _openAIService = openAIService;
        _googleAIService = googleAIService;
        _ollamaService = ollamaService;
        _sessionState = sessionState;
    }

    public async Task<string> GetAnswerAsync(string question, string context, AIProvider provider, string model)
    {
        var temperature = _sessionState.Temperature;
        return provider switch
        {
            AIProvider.Groq   => await _groqService.GetAnswerAsync(question, context, model, temperature),
            AIProvider.OpenAI => await _openAIService.GetAnswerAsync(question, context, model, temperature),
            AIProvider.Google => await _googleAIService.GetAnswerAsync(question, context, model, temperature),
            AIProvider.Ollama => await _ollamaService.GetAnswerAsync(question, context, model, temperature),
            _                 => await _groqService.GetAnswerAsync(question, context, model, temperature)
        };
    }

    public async Task<string> GetAnswerWithSystemPromptAsync(string systemPrompt, string userMessage, AIProvider provider, string model)
    {
        // We encode the system prompt as a special context prefix understood by the provider
        // Each provider's GetAnswerAsync wraps context in user turn; here we bypass that by
        // sending systemPrompt as context and a custom user message.
        // All current providers use:  system = "You are a helpful assistant…" / user = "Context:\n{context}\n\nQuestion: {question}"
        // We override by passing the system prompt directly as context and leaving question empty,
        // so the full user message is sent without "Context:" wrapping.
        var temperature = _sessionState.Temperature;

        // Build a combined prompt: system instructions + the actual user message
        // We send systemPrompt as the context (which becomes the system message body in each provider)
        // and userMessage as the question
        return provider switch
        {
            AIProvider.Groq   => await _groqService.GetAnswerAsync(userMessage, systemPrompt, model, temperature),
            AIProvider.OpenAI => await _openAIService.GetAnswerAsync(userMessage, systemPrompt, model, temperature),
            AIProvider.Google => await _googleAIService.GetAnswerAsync(userMessage, systemPrompt, model, temperature),
            AIProvider.Ollama => await _ollamaService.GetAnswerAsync(userMessage, systemPrompt, model, temperature),
            _                 => await _groqService.GetAnswerAsync(userMessage, systemPrompt, model, temperature)
        };
    }
}