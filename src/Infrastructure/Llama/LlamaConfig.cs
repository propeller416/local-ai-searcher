namespace Infrastructure.Llama;

/// <summary>Пути к GGUF и параметры контекста. В рантайме: <c>{BaseDirectory}/models/*.gguf</c> (в дистрибутив копируются из <c>models/</c> в корне репозитория).</summary>
public sealed record LlamaConfig(
    string ChatModelPath,
    string EmbedModelPath,
    uint ChatContextSize,
    uint EmbedContextSize,
    int GpuLayerCount)
{
    public const string DefaultChatFileName = "llama3.2-3b-q4_k_m.gguf";
    public const string DefaultEmbedFileName = "nomic-embed-text.gguf";

    public static LlamaConfig FromBaseDirectory(string? baseDirectory = null, int gpuLayerCount = 0)
    {
        var root = baseDirectory ?? AppContext.BaseDirectory;
        var modelsDir = Path.Combine(root, "models");
        return new LlamaConfig(
            Path.Combine(modelsDir, DefaultChatFileName),
            Path.Combine(modelsDir, DefaultEmbedFileName),
            ChatContextSize: 4096,
            EmbedContextSize: 2048,
            GpuLayerCount: gpuLayerCount);
    }
}
