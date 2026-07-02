# SmartDoc QA 🧠📄

SmartDoc QA is a professional C# & .NET 8 Blazor Web Application providing advanced **AI Document Intelligence**. It allows users to upload documents, automatically extract text (using OCR for images), parse tabular data (Excel & CSV), query local SQLite datasets, and chat with files using state-of-the-art Large Language Models (LLMs) via an intuitive, premium glassmorphism interface.

---

## 🎨 Premium Glassmorphic UI

The interface has been meticulously designed using **glassmorphic design principles**:
- **Ambient Glow Backgrounds**: Dynamic, fixed color circles slowly pulse behind elements to give beautiful depth.
- **Frosted Glass Cards**: Cards feature `backdrop-filter` blur, semi-transparent frosted borders, and soft shadows.
- **Unified Dark Mode**: Automatically shifts layout into dark-frost tones.
- **Fluid Layout**: Clean typography (using Google Fonts' *Inter*) and custom interactive micro-animations.

---

## ✨ Features

- 📁 **Multi-Format Processing**: Supports PDFs, Word Documents (`.docx`), Plain Text (`.txt`), CSVs, and Excel Worksheets (`.xlsx`).
- 👁️ **Image OCR Integration**: Automatically runs Optical Character Recognition on image uploads to index contents.
- 📊 **Table Parser & SQLite Engine**: Loads tabular worksheets directly into an in-memory SQLite schema, enabling accurate structural data querying.
- ⚡ **RAG Architecture**: Efficiently chunks documents, generates vector embeddings, and stores them in a fast vector repository, with an automatic fallback to TF-IDF keyword indexing.
- 🤖 **Multi-AI Provider Selector**: Hot-swap providers on the fly in the sidebar menu or settings panel:
  - **Local Models**: Run Ollama models completely offline.
  - **OpenAI**: Harness GPT-4 and GPT-3.5 models.
  - **Google Gemini**: Access Gemini model families.
  - **Groq**: Utilize high-speed Llama 3.3 models.
- 🌡️ **Creativity Controls**: Fine-tune model temperature ranges directly from the slider controls.

---

## 🚀 Getting Started

### 1. Prerequisites
- **.NET 8.0 SDK** (or newer)
- Optionally **Ollama** running locally (if using local AI execution)

### 2. Configuration
Create a `.env` file in the root directory (or inside `SmartDocQA.Web/`) and populate your API credentials:

```ini
# AI Provider Keys
OPENAI_API_KEY=your-openai-key
GEMINI_API_KEY=your-gemini-key
GROQ_API_KEY=your-groq-key

# Embedding Model Config
GEMINI_EMBEDDING_API_KEY=your-gemini-key
```

### 3. Build & Run
Run the Blazor web application project using the dotnet command line:

```bash
dotnet run --project SmartDocQA.Web/SmartDocQA.Web.csproj
```

The application compiles and launches a local development server at:
👉 **[http://localhost:5231](http://localhost:5231)**

---

## 🏗️ Repository Architecture

- `SmartDocQA.Core`: Interface declarations, model schemas, and state objects.
- `SmartDocQA.Infrastructure`: Implementation layer containing:
  - PDF/Word/Excel extraction filters.
  - AI connector handlers (OpenAI, Gemini, Groq, Ollama).
  - SQLite query builder logic.
  - TF-IDF and embedding generators.
- `SmartDocQA.Web`: Front-end Blazor component pages (`Home`, `Upload`, `Chat`, `Settings`) and styled layout variables.
