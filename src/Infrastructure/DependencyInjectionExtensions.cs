using Infrastructure.Llama;
using Infrastructure.Services;
using Application.Interfaces;
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
        services.AddSingleton<ISettingsService, SettingsService>();
        
        services.AddSingleton(sp => 
        {
            var settingsService = sp.GetRequiredService<ISettingsService>();
            var settings = settingsService.LoadSettings();
            return LlamaConfig.FromSettings(settings);
        });
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
