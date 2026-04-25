using LLama;
using LLama.Common;
using LLamaSharp.SemanticKernel.ChatCompletion;
using LLamaSharp.SemanticKernel.TextEmbedding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;

namespace Infrastructure.Llama;

/// <summary>Ленивая загрузка весов/контекста и сборка <see cref="Kernel"/>; освобождает нативные ресурсы при <see cref="Dispose"/>.</summary>
public sealed class LlamaKernelHost : IDisposable
{
    private readonly LlamaConfig _config;
    private readonly object _gate = new();
    private Kernel? _kernel;
    private InteractiveExecutor? _chatExecutor;
    private LLamaWeights? _chatWeights;
    private LLamaContext? _chatContext;
    private LLamaWeights? _embedWeights;
    private LLamaEmbedder? _embedder;

    public LlamaKernelHost(LlamaConfig config) => _config = config;

    public Kernel Kernel
    {
        get
        {
            lock (_gate)
            {
                if (_kernel is not null)
                    return _kernel;

                var embedParams = new ModelParams(_config.EmbedModelPath)
                {
                    ContextSize = _config.EmbedContextSize,
                    GpuLayerCount = _config.GpuLayerCount,
                };
                _embedWeights = LLamaWeights.LoadFromFile(embedParams);
                _embedder = new LLamaEmbedder(_embedWeights, embedParams);

                var chatParams = new ModelParams(_config.ChatModelPath)
                {
                    ContextSize = _config.ChatContextSize,
                    GpuLayerCount = _config.GpuLayerCount,
                };
                _chatWeights = LLamaWeights.LoadFromFile(chatParams);
                _chatContext = _chatWeights.CreateContext(chatParams);
                _chatExecutor = new InteractiveExecutor(_chatContext);

                var builder = Kernel.CreateBuilder();
                builder.Services.AddSingleton<IChatCompletionService>(_ => new LLamaSharpChatCompletion(_chatExecutor));
#pragma warning disable CS0618 // ITextEmbeddingGenerationService — контракт SK для шага 3/4 плана
                builder.Services.AddSingleton<ITextEmbeddingGenerationService>(_ => new LLamaSharpEmbeddingGeneration(_embedder));
#pragma warning restore CS0618

                _kernel = builder.Build();
                return _kernel;
            }
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _kernel = null;
            _chatExecutor = null;
            _chatContext?.Dispose();
            _chatContext = null;
            _chatWeights?.Dispose();
            _chatWeights = null;
            _embedder?.Dispose();
            _embedder = null;
            _embedWeights?.Dispose();
            _embedWeights = null;
        }
    }
}
