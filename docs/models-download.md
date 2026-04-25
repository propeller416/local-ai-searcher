# Загрузка моделей (GGUF)

Модели **не входят в git** (см. `.gitignore`). Для локальной разработки и сборки их нужно скачать вручную или через CI и положить в каталог **`models/` в корне репозитория** (рядом с `src/`). При сборке `DesktopApp` файлы копируются в **`{выход сборки}/models/`** — там же их ожидает приложение в рантайме.

## Ожидаемые имена файлов

Имена зашиты в коде (`LlamaConfig` в проекте Infrastructure):

| Файл | Назначение |
|------|------------|
| `llama3.2-3b-q4_k_m.gguf` | Чат / генерация (Llama 3.2 3B Instruct, квантизация Q4_K_M) |
| `nomic-embed-text.gguf` | Эмбеддинги для RAG |

Если у скачанного файла другое имя — **переименуйте** его под таблицу выше.

## Откуда скачать

### 1. Чат: Meta Llama 3.2 3B Instruct (Q4_K_M)

Репозиторий с готовыми GGUF (пример):

- [lmstudio-community/Meta-Llama-3.2-3B-Instruct-GGUF](https://huggingface.co/lmstudio-community/Meta-Llama-3.2-3B-Instruct-GGUF/tree/main)

### 2. Эмбеддинги: Nomic Embed Text

Официальные GGUF от Nomic (пример):

- [nomic-ai/nomic-embed-text-v1.5-GGUF](https://huggingface.co/nomic-ai/nomic-embed-text-v1.5-GGUF/tree/main)

