# ТЗ: AI-Ассистент для работы с документами (автономная локальная версия)

## 1. Цель проекта

Создать десктопное приложение для загрузки документов и получения ответов на вопросы по их содержанию (RAG). Приложение работает **полностью автономно** — не требует интернета, Docker, баз данных, внешних сервисов или предустановленного окружения. Устанавливается одним инсталлятором.

---

## 2. Ключевые принципы

- **Нет интернета** — ни при работе, ни после первого запуска
- **Нет Docker** — всё запускается как обычное десктопное приложение
- **Нет внешних сервисов** — LLM, эмбеддинги и БД работают внутри процесса
- **Один инсталлятор** — модели поставляются в комплекте, скачивать ничего не нужно
- **Кроссплатформенность** — Windows, macOS, Linux

---

## 3. Технологический стек


| Компонент           | Технология                                             | Назначение                                                             |
| ------------------- | ------------------------------------------------------ | ---------------------------------------------------------------------- |
| **Платформа + UI**  | .NET 10 (ASP.NET Core Host) + Avalonia UI              | Нативный десктопный интерфейс (XAML/C#) с использованием DI-контейнера |
| **LLM (генерация)** | LLamaSharp + llama.cpp                                 | Запуск GGUF-модели внутри процесса                                     |
| **Эмбеддинги**      | LLamaSharp (та же библиотека)                          | Генерация векторов внутри процесса                                     |
| **Векторный поиск** | sqlite-vec                                             | Расширение SQLite, нативные бинарники включены в NuGet                 |
| **БД**              | SQLite (Microsoft.Data.Sqlite)                         | Файловая БД без сервера                                                |
| **AI-оркестрация**  | Microsoft Semantic Kernel + LLamaSharp.semantic-kernel | Управление промптами                                                   |
| **Упаковка**        | Velopack                                               | Кроссплатформенный инсталлятор                                         |


---

## 4. Модели (поставляются в комплекте)


| Назначение            | Модель               | Формат | Размер  |
| --------------------- | -------------------- | ------ | ------- |
| **Эмбеддинги**        | `nomic-embed-text`   | GGUF   | ~274 MB |
| **Генерация ответов** | `llama3.2:3b` Q4_K_M | GGUF   | ~2.0 GB |


Модели хранятся в папке `models/` внутри директории приложения. LLamaSharp загружает их напрямую по пути без каких-либо внешних сервисов.

**Размер итогового инсталлятора:** ~2.3 GB

---

## 5. Функциональные требования

### 5.1. Управление документами

- Загрузка документов (PDF, DOCX, TXT, MD) через десктопный интерфейс
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
LocalAiSearcher/
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
│   └── DesktopApp/                         -- Avalonia UI
│       ├── Program.cs
│       ├── App.axaml
│       ├── ViewModels/
│       │   ├── MainWindowViewModel.cs
│       │   ├── ChatViewModel.cs
│       │   └── DocumentsViewModel.cs
│       └── Views/
│           ├── MainWindow.axaml
│           ├── ChatView.axaml              -- чат
│           └── DocumentsView.axaml         -- управление документами
│
├── models/                                 -- GGUF-модели (поставляются в комплекте)
│   ├── nomic-embed-text.gguf
│   └── llama3.2-3b-q4_k_m.gguf
│
├── LocalAiSearcher.sln
└── README.md
```

---

---

## 9. Упаковка и дистрибуция (Velopack)

```bash
# Сборка self-contained приложения
dotnet publish src/DesktopApp -r win-x64 --self-contained -o publish/

# Создание инсталлятора
vpk pack --packId LocalAiSearcher --packVersion 1.0.0 --packDir publish/ --mainExe LocalAiSearcher.exe
```

Velopack создаёт:

- `LocalAiSearcher-Setup.exe` (Windows)
- `LocalAiSearcher.dmg` (macOS)
- `LocalAiSearcher.AppImage` (Linux)

Все файлы из `models/` включаются в пакет автоматически.



# Процесс установки продукта пользователем

Благодаря автономной архитектуре и упаковке через Velopack, установка максимально проста и не требует технических навыков.

1. **Скачивание:** Пользователь скачивает один файл-инсталлятор для своей ОС (`.exe` для Windows, `.dmg` для macOS, `.AppImage` для Linux). Размер файла ~2.3 GB, так как он уже содержит внутри себя LLM-модели.
2. **Запуск инсталлятора:** Пользователь запускает скачанный файл.
  - *Никаких дополнительных загрузок* из интернета не происходит.
3. **Распаковка:** Инсталлятор автоматически распаковывает приложение и папку `models/` с нейросетями в директорию программы.
4. **Запуск:** После установки на рабочем столе появляется ярлык приложения. Пользователь открывает его, и система сразу готова к работе (загрузке документов и чату) в полностью офлайн-режиме.

---

