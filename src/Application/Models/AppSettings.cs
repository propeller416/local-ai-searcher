namespace Application.Models;

public class AppSettings
{
    public string ChatModelFileName { get; set; } = "llama-3.2-3b-instruct-uncensored-q3_k_m.gguf";
    public string EmbedModelFileName { get; set; } = "nomic-embed-text.gguf";
    public float Temperature { get; set; } = 0.1f;
    public int MaxTokens { get; set; } = 1024;
    public string SystemPrompt { get; set; } = "Ты — ассистент для поиска по документации. Отвечай на вопросы пользователя на русском языке, используя только предоставленный контекст. Если в контексте нет ответа, скажи об этом.";
    public bool EnableLlm { get; set; } = true;
    public bool ShowSources { get; set; } = true;
}
