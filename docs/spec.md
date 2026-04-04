dotnet-local-rag

# Техническое задание: AI-Ассистент для работы с документами (локальный RAG)

## 1. Цель проекта
Создать локальное веб-приложение для загрузки документов и получения ответов на вопросы по их содержанию с использованием RAG-подхода. Приложение работает полностью локально, без внешних API, использует открытые LLM и векторный поиск.

## 2. Функциональные требования

### 2.1. Управление документами
- Загрузка документов (PDF, DOCX, TXT, MD) через веб-интерфейс
- Просмотр списка загруженных документов
- Удаление документов (с каскадным удалением всех связанных чанков и эмбеддингов)
- **Асинхронная фоновая обработка** после загрузки (пользователь не ждет окончания обработки)

### 2.2. Pipeline обработки документов
1. **Извлечение текста** из загруженного файла в зависимости от формата
2. **Очистка текста** (удаление лишних пробелов, нормализация переносов строк)
3. **Chunking** — разбиение текста на смысловые фрагменты:
   - Размер чанка: 500-1000 символов
   - Перекрытие между чанками: 100-200 символов
4. **Генерация эмбеддингов** для каждого чанка через локальную модель (Ollama)
5. **Сохранение** чанков и их эмбеддингов в PostgreSQL с pgvector

### 2.3. Чат с RAG
- Интерфейс в виде диалогового окна (как в ChatGPT)
- При отправке вопроса:
  1. Генерация эмбеддинга вопроса (та же модель, что для документов)
  2. Векторный поиск в PostgreSQL (поиск косинусного сходства) — возвращаем топ-5 наиболее релевантных чанков
  3. Формирование промпта:
     ```
     Используй следующие фрагменты документов, чтобы ответить на вопрос.
     Если ответа нет в фрагментах, скажи, что информации недостаточно.
     
     Фрагменты:
     ---
     {chunk1}
     ---
     {chunk2}
     ---
     
     Вопрос: {question}
     Ответ:
     ```
  4. Отправка промпта в локальную LLM (Ollama)
  5. Отображение ответа + отображение источников (название документа, фрагмент текста, на основе которого дан ответ)

## 3. Нефункциональные требования
- **Полная локальность** — не используются внешние API
- **Контейнеризация** — Docker Compose для всех компонентов
- **Структурированное логирование** (Serilog) для отладки и мониторинга работы пайплайна
- **Health Checks** для мониторинга состояния сервисов

---

## 4. Технологический стек (финальный)

| Компонент | Технология | Назначение |
|-----------|------------|------------|
| **Backend** | ASP.NET Core 9 | Веб-API и фоновая обработка |
| **AI-оркестрация** | Microsoft Semantic Kernel | Управление промптами, коннекторы к Ollama |
| **Векторная БД** | PostgreSQL 17 + pgvector | Хранение чанков и эмбеддингов |
| **Локальная LLM** | Ollama | Запуск моделей (эмбеддинги + генерация) |
| **Фронтенд** | Blazor Server | Веб-интерфейс на C# |
| **Логирование** | Serilog | Структурированное логирование |
| **Оркестрация** | Docker Compose | Запуск всех сервисов одной командой |

---

## 5. Выбор LLM-моделей (Ollama)

Для работы с локальными моделями через Ollama потребуются **две модели**:

### 5.1. Модель для эмбеддингов
**Рекомендация:** `nomic-embed-text` (или `all-minilm`)

| Модель | Размер | Качество | Требования |
|--------|--------|----------|------------|
| `nomic-embed-text` | ~274 MB | Отличное | 2-4 GB RAM |
| `all-minilm` | ~120 MB | Хорошее | 2 GB RAM |

*Эту модель нужно будет скачать через `ollama pull nomic-embed-text`*

### 5.2. Модель для генерации ответов
**Рекомендация:** `llama3.2:3b` или `mistral`

| Модель | Размер | Качество | Требования |
|--------|--------|----------|------------|
| `llama3.2:3b` | ~2 GB | Хорошее | 8 GB RAM |
| `mistral:7b` | ~4.1 GB | Отличное | 16 GB RAM |
| `phi4:14b` | ~9 GB | Очень хорошее | 32 GB RAM |

*Для старта рекомендую `llama3.2:3b` — баланс качества и требований к ресурсам.*

---

## 6. Структура базы данных (PostgreSQL + pgvector)

```sql
-- Таблица документов
CREATE TABLE documents (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    filename TEXT NOT NULL,
    file_path TEXT NOT NULL,
    content_type TEXT NOT NULL,
    status TEXT NOT NULL DEFAULT 'pending', -- pending, processing, completed, failed
    uploaded_at TIMESTAMP DEFAULT NOW()
);

-- Включить расширение pgvector
CREATE EXTENSION vector;

-- Таблица чанков (с векторным полем)
CREATE TABLE document_chunks (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    document_id UUID NOT NULL REFERENCES documents(id) ON DELETE CASCADE,
    content TEXT NOT NULL,
    embedding vector(768), -- размерность зависит от модели (nomic-embed-text = 768)
    metadata JSONB,        -- для хранения позиции в документе и др.
    created_at TIMESTAMP DEFAULT NOW()
);

-- Индекс для быстрого поиска по векторам (IVFFlat или HNSW)
CREATE INDEX ON document_chunks USING ivfflat (embedding vector_cosine_ops);
```

---

## 7. Структура проекта

```
RagChat/
├── src/
│   ├── Domain/
│   │   ├── Entities/
│   │   │   ├── Document.cs
│   │   │   └── DocumentChunk.cs
│   │   └── Enums/
│   │       └── DocumentStatus.cs
│   │
│   ├── Application/
│   │   ├── Interfaces/
│   │   │   ├── IDocumentRepository.cs
│   │   │   ├── IVectorRepository.cs
│   │   │   └── ITextExtractor.cs (стратегия для разных форматов)
│   │   ├── Services/
│   │   │   ├── DocumentProcessingService.cs (фоновый обработчик)
│   │   │   ├── ChunkingService.cs (разбиение текста)
│   │   │   └── RagService.cs (вопрос -> поиск -> генерация)
│   │   └── DTOs/
│   │       ├── UploadDocumentDto.cs
│   │       └── ChatRequestDto.cs
│   │
│   ├── Infrastructure/
│   │   ├── Data/
│   │   │   ├── AppDbContext.cs
│   │   │   ├── Configurations/
│   │   │   └── Repositories/
│   │   ├── AI/
│   │   │   ├── SemanticKernelSetup.cs (настройка Kernel)
│   │   │   └── OllamaConnector.cs
│   │   ├── TextExtraction/
│   │   │   ├── PdfTextExtractor.cs
│   │   │   ├── DocxTextExtractor.cs
│   │   │   └── TextFileExtractor.cs
│   │   └── BackgroundJobs/
│   │       └── DocumentProcessingBackgroundService.cs (BackgroundService)
│   │
│   ├── WebAPI/
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   └── Endpoints/
│   │       ├── DocumentEndpoints.cs
│   │       └── ChatEndpoints.cs
│   │
│   └── BlazorUI/
│       ├── Components/
│       │   ├── Pages/
│       │   │   ├── Home.razor (чат)
│       │   │   └── Documents.razor (управление документами)
│       │   ├── Shared/
│       │   │   └── MainLayout.razor
│       │   └── ChatMessage.razor (компонент сообщения)
│       ├── Services/
│       │   └── ApiClient.cs
│       └── Program.cs
│
├── docker-compose.yml
├── Dockerfile.backend
├── Dockerfile.frontend
└── README.md
```

---

## 8. Docker Compose (предварительная структура)

```yaml
services:
  postgres:
    image: pgvector/pgvector:pg17
    environment:
      POSTGRES_DB: ragchat
      POSTGRES_USER: user
      POSTGRES_PASSWORD: password
    volumes:
      - postgres_data:/var/lib/postgresql/data
    ports:
      - "5432:5432"

  ollama:
    image: ollama/ollama:latest
    volumes:
      - ollama_data:/root/.ollama
    ports:
      - "11434:11434"
    command: serve
    # после запуска нужно будет выполнить:
    # docker exec -it ollama ollama pull nomic-embed-text
    # docker exec -it ollama ollama pull llama3.2:3b

  backend:
    build:
      context: .
      dockerfile: Dockerfile.backend
    environment:
      - ConnectionStrings__Default=Host=postgres;Database=ragchat;Username=user;Password=password
      - Ollama__Endpoint=http://ollama:11434
      - Ollama__EmbeddingModel=nomic-embed-text
      - Ollama__ChatModel=llama3.2:3b
    ports:
      - "5000:8080"
    depends_on:
      - postgres
      - ollama
    volumes:
      - ./uploads:/app/uploads

  frontend:
    build:
      context: .
      dockerfile: Dockerfile.frontend
    ports:
      - "5252:8080"
    depends_on:
      - backend
```

---

## 9. Ключевые моменты реализации

### 9.1. Semantic Kernel настройка для Ollama

```csharp
// В Program.cs
using Microsoft.SemanticKernel;

var builder = Kernel.CreateBuilder();

// Добавляем Ollama для генерации текста
builder.AddOpenAITextGeneration(
    modelId: configuration["Ollama:ChatModel"],
    endpoint: new Uri(configuration["Ollama:Endpoint"]),
    apiKey: null  // локальные модели не требуют ключа
);

// Добавляем Ollama для эмбеддингов
builder.AddOpenAITextEmbeddingGeneration(
    modelId: configuration["Ollama:EmbeddingModel"],
    endpoint: new Uri(configuration["Ollama:Endpoint"]),
    apiKey: null
);
```

### 9.2. Фоновая обработка документов

Используем `BackgroundService` с `Channel<T>` для очереди заданий:

```csharp
public class DocumentProcessingBackgroundService : BackgroundService
{
    private readonly Channel<Guid> _queue = Channel.CreateUnbounded<Guid>();
    
    public void QueueDocument(Guid documentId) => _queue.Writer.TryWrite(documentId);
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var documentId in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            await ProcessDocument(documentId);
        }
    }
}
```

### 9.3. Chunking с перекрытием

```csharp
public List<string> ChunkText(string text, int chunkSize = 500, int overlap = 100)
{
    var chunks = new List<string>();
    var start = 0;
    
    while (start < text.Length)
    {
        var end = Math.Min(start + chunkSize, text.Length);
        var chunk = text.Substring(start, end - start);
        chunks.Add(chunk);
        start += chunkSize - overlap;
    }
    
    return chunks;
}
```

### 9.4. Поиск с pgvector

```csharp
// EF Core запрос для поиска по косинусному сходству
var results = await context.DocumentChunks
    .Where(c => c.DocumentId == documentId) // опциональная фильтрация
    .OrderBy(c => c.Embedding.CosineDistance(questionEmbedding))
    .Take(5)
    .Select(c => new { c.Content, c.Document.Filename })
    .ToListAsync();
```

---

## 10. План разработки (по шагам)

| Этап | Описание |
|------|----------|
| **1** | Настройка Docker Compose (PostgreSQL + pgvector, Ollama) |
| **2** | Создание проекта ASP.NET Core 9, подключение EF Core, миграции |
| **3** | Реализация загрузки файлов и сохранения метаданных в БД |
| **4** | Настройка Semantic Kernel для Ollama (эмбеддинги + генерация) |
| **5** | Реализация фоновой обработки: извлечение текста → чанкинг → эмбеддинги → сохранение |
| **6** | Создание API эндпоинта для чата (вопрос → поиск → генерация) |
| **7** | Создание Blazor UI (страница загрузки документов, страница чата) |
| **8** | Логирование через Serilog (консоль + файл) |
| **9** | Финальное тестирование, доработка README |

---

## 11. Минимальные системные требования для локальной работы

- **Процессор:** 4+ ядер
- **Оперативная память:** 16 GB (рекомендуется для комфортной работы с моделью 3b)
- **Диск:** 10+ GB свободного места (для моделей Ollama)
- **Docker Desktop** с поддержкой Linux-контейнеров
