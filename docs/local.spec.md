# ТЗ: AI-Ассистент для работы с документами (автономная локальная версия)

## 1. Цель проекта

Создать десктопное веб-приложение для загрузки документов и получения ответов на вопросы по их содержанию (RAG). Приложение работает **полностью автономно** — не требует интернета, Docker, баз данных, внешних сервисов или предустановленного окружения. Устанавливается одним инсталлятором.

---

## 2. Ключевые принципы

- **Нет интернета** — ни при работе, ни после первого запуска
- **Нет Docker** — всё запускается как обычное десктопное приложение
- **Нет внешних сервисов** — LLM, эмбеддинги и БД работают внутри процесса
- **Один инсталлятор** — модели поставляются в комплекте, скачивать ничего не нужно
- **Кроссплатформенность** — Windows, macOS, Linux

---

## 3. Технологический стек

| Компонент | Технология | Назначение |
|---|---|---|
| **Backend + UI** | ASP.NET Core 9 + Blazor Server | Веб-API и интерфейс в одном процессе |
| **LLM (генерация)** | LLamaSharp + llama.cpp | Запуск GGUF-модели внутри процесса |
| **Эмбеддинги** | LLamaSharp (та же библиотека) | Генерация векторов внутри процесса |
| **Векторный поиск** | sqlite-vec | Расширение SQLite, нативные бинарники включены в NuGet |
| **БД** | SQLite (Microsoft.Data.Sqlite) | Файловая БД без сервера |
| **AI-оркестрация** | Microsoft Semantic Kernel + LLamaSharp.semantic-kernel | Управление промптами |
| **Упаковка** | Velopack | Кроссплатформенный инсталлятор |

---

## 4. Модели (поставляются в комплекте)

| Назначение | Модель | Формат | Размер |
|---|---|---|---|
| **Эмбеддинги** | `nomic-embed-text` | GGUF | ~274 MB |
| **Генерация ответов** | `llama3.2:3b` Q4_K_M | GGUF | ~2.0 GB |

Модели хранятся в папке `models/` внутри директории приложения. LLamaSharp загружает их напрямую по пути без каких-либо внешних сервисов.

**Размер итогового инсталлятора:** ~2.3 GB

### Минимальные системные требования

- **CPU:** 4+ ядра (x64 или ARM64)
- **RAM:** 8 GB
- **Диск:** 3 GB свободного места
- **ОС:** Windows 10+, macOS 12+, Ubuntu 20.04+

---

## 5. Функциональные требования

### 5.1. Управление документами

- Загрузка документов (PDF, DOCX, TXT, MD) через веб-интерфейс
- Просмотр списка загруженных документов со статусами
- Удаление документов (каскадное удаление чанков и эмбеддингов)
- Асинхронная фоновая обработка после загрузки

### 5.2. Pipeline обработки документов

1. **Извлечение текста** по формату файла
2. **Очистка текста** (нормализация пробелов и переносов строк)
3. **Чанкинг** — разбиение на фрагменты 500–1000 символов, перекрытие 100–200 символов
4. **Генерация эмбеддингов** через LLamaSharp (in-process)
5. **Сохранение** чанков и эмбеддингов в SQLite через sqlite-vec

### 5.3. Чат с RAG

- Диалоговый интерфейс (как в ChatGPT)
- При отправке вопроса:
  1. Генерация эмбеддинга вопроса (LLamaSharp, in-process)
  2. Векторный поиск через sqlite-vec — топ-5 релевантных чанков
  3. Формирование промпта с найденными фрагментами
  4. Генерация ответа через LLamaSharp (in-process)
  5. Отображение ответа и источников (документ, фрагмент текста)

---

## 6. Структура базы данных (SQLite + sqlite-vec)

```sql
-- Таблица документов
CREATE TABLE documents (
    id        TEXT PRIMARY KEY,  -- UUID
    filename  TEXT NOT NULL,
    file_path TEXT NOT NULL,
    content_type TEXT NOT NULL,
    status    TEXT NOT NULL DEFAULT 'pending',  -- pending, processing, completed, failed
    uploaded_at TEXT DEFAULT (datetime('now'))
);

-- Таблица чанков
CREATE TABLE document_chunks (
    id          TEXT PRIMARY KEY,  -- UUID
    document_id TEXT NOT NULL REFERENCES documents(id) ON DELETE CASCADE,
    content     TEXT NOT NULL,
    metadata    TEXT,              -- JSON: позиция в документе
    created_at  TEXT DEFAULT (datetime('now'))
);

-- Виртуальная таблица sqlite-vec для эмбеддингов
CREATE VIRTUAL TABLE chunk_embeddings USING vec0(
    chunk_id TEXT PRIMARY KEY,
    embedding float[768]           -- размерность nomic-embed-text
);
```

Поиск по косинусному сходству:
```sql
SELECT c.content, d.filename, e.distance
FROM chunk_embeddings e
JOIN document_chunks c ON c.id = e.chunk_id
JOIN documents d ON d.id = c.document_id
WHERE embedding MATCH ?
  AND k = 5
ORDER BY e.distance;
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
│   │   │   └── ITextExtractor.cs
│   │   ├── Services/
│   │   │   ├── DocumentProcessingService.cs
│   │   │   ├── ChunkingService.cs
│   │   │   └── RagService.cs
│   │   └── DTOs/
│   │       ├── UploadDocumentDto.cs
│   │       └── ChatRequestDto.cs
│   │
│   ├── Infrastructure/
│   │   ├── Data/
│   │   │   ├── SqliteDbContext.cs          -- инициализация SQLite + sqlite-vec
│   │   │   └── Repositories/
│   │   │       ├── DocumentRepository.cs
│   │   │       └── VectorRepository.cs     -- sqlite-vec поиск
│   │   ├── AI/
│   │   │   └── LlamaSharpSetup.cs          -- регистрация LLamaSharp в SK
│   │   ├── TextExtraction/
│   │   │   ├── PdfTextExtractor.cs         -- UglyToad.PdfPig
│   │   │   ├── DocxTextExtractor.cs        -- DocumentFormat.OpenXml
│   │   │   └── TextFileExtractor.cs
│   │   └── BackgroundJobs/
│   │       └── DocumentProcessingBackgroundService.cs
│   │
│   └── WebApp/                             -- ASP.NET Core + Blazor Server (один проект)
│       ├── Program.cs
│       ├── appsettings.json
│       ├── Endpoints/
│       │   ├── DocumentEndpoints.cs
│       │   └── ChatEndpoints.cs
│       └── Components/
│           ├── Pages/
│           │   ├── Home.razor              -- чат
│           │   └── Documents.razor         -- управление документами
│           ├── Shared/
│           │   └── MainLayout.razor
│           └── ChatMessage.razor
│
├── models/                                 -- GGUF-модели (поставляются в комплекте)
│   ├── nomic-embed-text.gguf
│   └── llama3.2-3b-q4_k_m.gguf
│
├── RagChat.sln
└── README.md
```

---

## 8. Ключевые моменты реализации

### 8.1. Инициализация LLamaSharp через Semantic Kernel

```csharp
var modelPath = Path.Combine(AppContext.BaseDirectory, "models");

var embedParams = new ModelParams(Path.Combine(modelPath, "nomic-embed-text.gguf"));
var chatParams  = new ModelParams(Path.Combine(modelPath, "llama3.2-3b-q4_k_m.gguf"));

builder.Services.AddSingleton(new LLamaEmbedder(new LLamaWeights.LoadFromFile(embedParams), embedParams));
builder.Services.AddSingleton(new LLamaContext(new LLamaWeights.LoadFromFile(chatParams), chatParams));

// Регистрация в Semantic Kernel
kernelBuilder.AddLLamaSharpChatCompletion(llamaContext);
kernelBuilder.AddLLamaSharpTextEmbeddingGeneration(llamaEmbedder);
```

### 8.2. Инициализация SQLite + sqlite-vec

```csharp
public class SqliteDbContext
{
    private readonly SqliteConnection _connection;

    public SqliteDbContext(string dbPath)
    {
        _connection = new SqliteConnection($"Data Source={dbPath}");
        _connection.Open();
        SqliteVec.Load(_connection);  // загружаем расширение sqlite-vec
        InitSchema();
    }
}
```

### 8.3. Фоновая обработка документов

```csharp
public class DocumentProcessingBackgroundService : BackgroundService
{
    private readonly Channel<Guid> _queue = Channel.CreateUnbounded<Guid>();

    public void QueueDocument(Guid documentId) => _queue.Writer.TryWrite(documentId);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var id in _queue.Reader.ReadAllAsync(stoppingToken))
            await ProcessDocumentAsync(id);
    }
}
```

---

## 9. Упаковка и дистрибуция (Velopack)

```bash
# Сборка self-contained приложения
dotnet publish src/WebApp -r win-x64 --self-contained -o publish/

# Создание инсталлятора
vpk pack --packId RagChat --packVersion 1.0.0 --packDir publish/ --mainExe RagChat.exe
```

Velopack создаёт:
- `RagChat-Setup.exe` (Windows)
- `RagChat.dmg` (macOS)
- `RagChat.AppImage` (Linux)

Все файлы из `models/` включаются в пакет автоматически.

---

## 10. NuGet-зависимости

```xml
<!-- LLM + эмбеддинги -->
<PackageReference Include="LLamaSharp" />
<PackageReference Include="LLamaSharp.Backend.Cpu" />        <!-- или Metal/Cuda -->
<PackageReference Include="LLamaSharp.semantic-kernel" />

<!-- БД + векторный поиск -->
<PackageReference Include="Microsoft.Data.Sqlite" />
<PackageReference Include="sqlite-vec" />

<!-- AI-оркестрация -->
<PackageReference Include="Microsoft.SemanticKernel" />

<!-- Извлечение текста -->
<PackageReference Include="UglyToad.PdfPig" />
<PackageReference Include="DocumentFormat.OpenXml" />

```

---

## 11. План разработки

| Этап | Описание |
|---|---|
| **0** | **MVP: UI-прототип со stub-данными** |
| **1** | Скаффолдинг solution, структура проектов, NuGet-зависимости |
| **2** | Domain-слой: сущности, перечисления |
| **3** | Infrastructure: SQLite + sqlite-vec, репозитории |
| **4** | Infrastructure: LLamaSharp + Semantic Kernel (эмбеддинги + генерация) |
| **5** | Infrastructure: извлечение текста (PDF, DOCX, TXT/MD) |
| **6** | Application: ChunkingService, DocumentProcessingService, RagService |
| **7** | Infrastructure: фоновая обработка (BackgroundService + Channel) |
| **8** | WebApp: Minimal API эндпоинты (документы + чат) |
| **9** | WebApp: Blazor UI (страница документов + чат-интерфейс) — замена stub на реальные сервисы |
| **10** | Сборка инсталлятора через Velopack, финальное тестирование |

---

## 12. Этап 0: MVP — UI-прототип

Цель: проверить и согласовать интерфейс до реализации бизнес-логики. Никаких внешних зависимостей — только Blazor Server с in-memory stub-сервисами.

### Stub-данные (фейковые документы)

```csharp
public class StubDocumentRepository : IDocumentRepository
{
    private readonly List<Document> _docs = new()
    {
        new Document { Id = Guid.NewGuid(), Filename = "Руководство пользователя.pdf",   ContentType = "application/pdf",  Status = DocumentStatus.Completed, UploadedAt = DateTime.Now.AddDays(-3) },
        new Document { Id = Guid.NewGuid(), Filename = "Техническое задание.docx",        ContentType = "application/docx", Status = DocumentStatus.Completed, UploadedAt = DateTime.Now.AddDays(-1) },
        new Document { Id = Guid.NewGuid(), Filename = "Заметки по проекту.md",           ContentType = "text/markdown",    Status = DocumentStatus.Processing, UploadedAt = DateTime.Now.AddMinutes(-5) },
    };

    public Task<IEnumerable<Document>> GetAllAsync() => Task.FromResult(_docs.AsEnumerable());
    public Task DeleteAsync(Guid id) { _docs.RemoveAll(d => d.Id == id); return Task.CompletedTask; }
    public Task AddAsync(Document doc) { _docs.Add(doc); return Task.CompletedTask; }
}
```

### Stub-чат

```csharp
public class StubRagService : IRagService
{
    public Task<ChatResponse> AskAsync(string question) =>
        Task.FromResult(new ChatResponse { Answer = $"Вы спросили: {question}" });
}
```

### Регистрация stub-сервисов

```csharp
// Program.cs — MVP-режим
builder.Services.AddScoped<IDocumentRepository, StubDocumentRepository>();
builder.Services.AddScoped<IRagService, StubRagService>();
```

При переходе к этапу 9 stub-реализации заменяются на реальные — остальной код (Blazor-компоненты, эндпоинты) не меняется.
