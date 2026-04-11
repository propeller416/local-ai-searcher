# Memory Bank

## Project: LocalAiSearcher

**Core Goal:** Autonomous local RAG desktop app (Avalonia UI) requiring no internet, Docker, or external databases.

**Tech Stack:**
- **UI Framework:** Avalonia UI
- **AI/LLM:** LLamaSharp v0.25, Semantic Kernel (In-process execution)
- **DB/Vector Search:** SQLite + sqlite-vec
- **Packaging:** Velopack

**Key Models (Bundled in `models/`):**
- Embedding: `nomic-embed-text.gguf`
- Chat: `llama3.2:3b` (Q4_K_M)

**Current Status:**
- Planning phase. 
- Implementation plan created (`docs/plan.md`). 
- Next step: MVP (UI Prototypes with stubbed data) to validate UX before backend integration.