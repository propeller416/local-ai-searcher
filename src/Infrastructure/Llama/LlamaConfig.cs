namespace Infrastructure.Llama;

public sealed record LlamaConfig(
    string ChatModelPath,
    string EmbedModelPath,
    uint ChatContextSize,
    uint EmbedContextSize,
    int GpuLayerCount,
    float Temperature = 0.1f,
    int MaxTokens = 1024)
{

    public const string DefaultChatFileName = "llama-3.2-3b-instruct-uncensored-q3_k_m.gguf";
    public const string DefaultEmbedFileName = "nomic-embed-text.gguf";

    public static LlamaConfig FromSettings(Application.Models.AppSettings settings, string? baseDirectory = null, int gpuLayerCount = 0)
    {
        return new LlamaConfig(
            Application.Helpers.AppPaths.ResolveModelPath(settings.ChatModelFileName),
            Application.Helpers.AppPaths.ResolveModelPath(settings.EmbedModelFileName),
            ChatContextSize: 4096,
            EmbedContextSize: 2048,
            GpuLayerCount: gpuLayerCount,
            Temperature: settings.Temperature,
            MaxTokens: settings.MaxTokens);
    }
}
