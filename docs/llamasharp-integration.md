# Интеграция LLamaSharp (v0.25) в LocalAiSearcher

Данный документ описывает архитектуру и технические детали использования библиотеки **LLamaSharp** для локального RAG-пайплайна. Интеграция обеспечивает полностью автономную работу приложения без обращения к внешним API, Docker или сторонним сервисам.

Источники:
https://github.com/SciSharp/LLamaSharp
https://scisharp.github.io/LLamaSharp/0.25.0/

## 1. Зависимости (NuGet)

Для работы с LLamaSharp версии 0.25 в рамках проекта на .NET 10 требуются следующие пакеты. Они обеспечивают инференс моделей и бесшовную интеграцию с оркестратором ИИ:

```xml
<ItemGroup>
    <!-- Основной движок и интеграция с Microsoft Semantic Kernel -->
    <PackageReference Include="LLamaSharp" Version="0.25.0" />
    <PackageReference Include="LLamaSharp.semantic-kernel" Version="0.25.0" />
    
    <!-- Нативные бэкенды для кроссплатформенного выполнения -->
    <!-- Для macOS ускорение Metal поставляется в составе Backend.Cpu -->
    <PackageReference Include="LLamaSharp.Backend.Cpu" Version="0.25.0" />
    
    <!-- Опционально: драйверы для GPU ускорения (NVIDIA) на Windows/Linux -->
    <!-- <PackageReference Include="LLamaSharp.Backend.Cuda12" Version="0.25.0" /> -->
</ItemGroup>
```

## 2. Управление жизненным циклом и DI

Поскольку загрузка GGUF-моделей (LLaMA 3.2 3B и Nomic Embed Text) является ресурсоемкой операцией, объекты весов и контекстов создаются единожды при старте приложения (Singleton). Они разделяются между всеми вызовами в Avalonia UI.

### 2.1. Инициализация в `Program.cs`

```csharp
using LLama.Common;
using LLama;
using Microsoft.SemanticKernel;

public static class LlamaSharpSetup
{
    public static void AddLocalAiServices(this IHostApplicationBuilder builder)
    {
        var modelPath = Path.Combine(AppContext.BaseDirectory, "models");

        // 1. Инициализация LLM для генерации ответов
        var chatParams = new ModelParams(Path.Combine(modelPath, "llama3.2-3b-q4_k_m.gguf"))
        {
            ContextSize = 4096, // Размер контекстного окна RAG
            GpuLayerCount = 20  // Максимальное кол-во слоев для выгрузки на GPU
        };

        var chatWeights = LLamaWeights.LoadFromFile(chatParams);
        var chatContext = chatWeights.CreateContext(chatParams);

        builder.Services.AddSingleton(chatWeights);
        builder.Services.AddSingleton(chatContext);

        // 2. Инициализация модели для эмбеддингов
        var embedParams = new ModelParams(Path.Combine(modelPath, "nomic-embed-text.gguf"))
        {
            ContextSize = 2048,
            GpuLayerCount = 20
        };

        var embedWeights = LLamaWeights.LoadFromFile(embedParams);
        var llamaEmbedder = new LLamaEmbedder(embedWeights, embedParams);

        builder.Services.AddSingleton(embedWeights);
        builder.Services.AddSingleton(llamaEmbedder);

        // 3. Регистрация провайдеров в Semantic Kernel
        var kernelBuilder = builder.Services.AddKernel();
        
        kernelBuilder.AddLLamaSharpChatCompletion(chatContext);
        kernelBuilder.AddLLamaSharpTextEmbeddingGeneration(llamaEmbedder);
    }
}
```

## 3. Архитектура RAG: Векторизация документов

Процесс разбиения загруженных файлов на чанки сопровождается вычислением векторных представлений in-process. Вся работа с LLamaSharp абстрагирована через `ITextEmbeddingGenerationService`.

```csharp
public class ChunkingService
{
    private readonly ITextEmbeddingGenerationService _embeddingService;

    public ChunkingService(ITextEmbeddingGenerationService embeddingService)
    {
        _embeddingService = embeddingService;
    }

    public async Task ProcessAndSaveChunksAsync(string documentText)
    {
        // 1. Разбиение текста на фрагменты (настраиваемая логика)
        var chunks = SplitIntoChunks(documentText);

        // 2. Локальная генерация эмбеддингов (Nomic Embed Text)
        var embeddings = await _embeddingService.GenerateEmbeddingsAsync(chunks);

        // 3. Сохранение чанков и векторов в SQLite (sqlite-vec)
        SaveToVectorDatabase(chunks, embeddings);
    }
}
```

## 4. Архитектура RAG: Чат и стриминг генерации

Во время чата система сначала векторизует вопрос пользователя, ищет релевантный контекст в БД, а затем передает собранный промпт в LLamaSharp для потоковой генерации ответа.

```csharp
public class RagService
{
    private readonly Kernel _kernel;
    private readonly IVectorRepository _vectorRepo;

    public RagService(Kernel kernel, IVectorRepository vectorRepo)
    {
        _kernel = kernel;
        _vectorRepo = vectorRepo;
    }

    public async IAsyncEnumerable<string> AskStreamAsync(string question)
    {
        // 1. Векторизация вопроса (in-process)
        var embeddingService = _kernel.GetRequiredService<ITextEmbeddingGenerationService>();
        var questionEmbedding = await embeddingService.GenerateEmbeddingAsync(question);

        // 2. Векторный поиск по SQLite (sqlite-vec)
        var contextChunks = await _vectorRepo.SearchSimilarAsync(questionEmbedding, k: 5);
        var contextText = string.Join("\n\n", contextChunks.Select(c => c.Content));

        // 3. Формирование системного промпта
        var prompt = $@"Используй следующий контекст для ответа на вопрос.
Контекст:
{contextText}

Вопрос: {question}";

        var history = new ChatHistory("Ты — полезный ИИ-ассистент.");
        history.AddUserMessage(prompt);

        // Настройки генерации
        var executionSettings = new PromptExecutionSettings
        {
            ExtensionData = new Dictionary<string, object>
            {
                { "MaxTokens", 512 },
                { "Temperature", 0.1 } // Низкая температура для фактологической точности
            }
        };

        // 4. Потоковая генерация ответа через Semantic Kernel -> LLamaSharp
        var chatService = _kernel.GetRequiredService<IChatCompletionService>();
        
        await foreach (var chunk in chatService.GetStreamingChatMessageContentsAsync(history, executionSettings, _kernel))
        {
            if (!string.IsNullOrEmpty(chunk.Content))
            {
                yield return chunk.Content;
            }
        }
    }
}
```

## 5. Особенности и лучшие практики версии 0.25

1. **Динамический `GpuLayerCount`:** Параметр выгрузки слоев на GPU критически влияет на скорость инференса. Рекомендуется выносить его в `appsettings.json`. Если на целевом устройстве нет дискретного GPU или возникают ошибки CUDA/Metal, значение должно динамически откатываться до `0`.
2. **Управление памятью:** `LLamaWeights` и `LLamaContext` — это unmanaged-обертки поверх `llama.cpp`. Они реализуют `IDisposable`. При регистрации их как Singleton в Microsoft.Extensions.Hosting IHost корректно вызовет `Dispose()` во время `ApplicationStopping`, очистив оперативную и видеопамять.
3. **Sampling Pipeline:** Начиная с версии 0.25 параметры сэмплинга вынесены в отдельную абстракцию (`DefaultSamplingPipeline`). При использовании прямого API (без Semantic Kernel) их необходимо передавать через инстанс пайплайна в `InferenceParams`. При использовании `LLamaSharp.semantic-kernel` данный маппинг происходит под капотом автоматически.