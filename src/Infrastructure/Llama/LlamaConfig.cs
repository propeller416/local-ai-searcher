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
    //llama3.2-3b-q4_k_m.gguf
    //gemma-4-E4B-it-UD-Q4_K_XL.gguf
    //gemma-4-E4B-it-Q4_K_M.gguf
    //Qwen3.5-4B-Q4_K_M.gguf
    //T-lite-it-2.1-Q8_0.gguf

    public const string DefaultChatFileName = "T-lite-it-2.1-Q8_0.gguf";
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
            GpuLayerCount: gpuLayerCount,
            Temperature: 0.1f,
            MaxTokens: 1024);
    }
}
