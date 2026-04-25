using Infrastructure.Llama;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;

namespace Infrastructure;

public static class DependencyInjectionExtensions
{
    /// <summary>LLamaSharp + Semantic Kernel: модели подгружаются при первом обращении к <see cref="Kernel"/> или AI-сервисам.</summary>
    public static IServiceCollection AddLlamaAndSemanticKernelServices(this IServiceCollection services)
    {
        services.AddSingleton(LlamaConfig.FromBaseDirectory());
        services.AddSingleton<LlamaKernelHost>();

        services.AddSingleton(sp => new Lazy<Kernel>(() => sp.GetRequiredService<LlamaKernelHost>().Kernel));

        services.AddSingleton(sp => sp.GetRequiredService<Lazy<Kernel>>().Value);

        services.AddSingleton(sp =>
            sp.GetRequiredService<Kernel>().GetRequiredService<IChatCompletionService>());
            
        services.AddSingleton(sp => new Lazy<IChatCompletionService>(() => 
            sp.GetRequiredService<Kernel>().GetRequiredService<IChatCompletionService>()));

#pragma warning disable CS0618
        services.AddSingleton(sp =>
            sp.GetRequiredService<Kernel>().GetRequiredService<ITextEmbeddingGenerationService>());
            
        services.AddSingleton(sp => new Lazy<ITextEmbeddingGenerationService>(() => 
            sp.GetRequiredService<Kernel>().GetRequiredService<ITextEmbeddingGenerationService>()));
#pragma warning restore CS0618

        return services;
    }
}
