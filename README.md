<div align="center">

<img src="https://capsule-render.vercel.app/api?type=waving&height=230&color=0:0F2027,50:203A43,100:00C9A7&text=Distributed%20Search%20Engine&fontColor=ffffff&fontSize=48&fontAlignY=38&desc=Building%20a%20Production-Inspired%20Hybrid%20Search%20Platform&descAlignY=58&descSize=18&animation=fadeIn"/>

<p>
<a href="https://github.com/Sanket9326">
<img src="https://readme-typing-svg.demolab.com?font=Fira+Code&weight=600&size=20&pause=1200&color=00C9A7&center=true&vCenter=true&width=850&lines=Upload+%E2%86%92+Store+%E2%86%92+Publish+%E2%86%92+Ingest+%E2%86%92+Chunk+%E2%86%92+Embed+%E2%86%92+Search+%E2%86%92+Answer;Distributed+Microservices+Built+with+.NET+10;Apache+Kafka+%7C+PostgreSQL+%7C+MinIO+%7C+Qdrant+%7C+Ollama;Semantic+Search+%2B+RAG+%28Gemini%29+%2B+Angular+UI+%2B+Observability%3B+Next%3A+Hybrid+Retrieval" />
</a>
</p>

![.NET](https://img.shields.io/badge/.NET-10-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Kafka](https://img.shields.io/badge/Apache%20Kafka-Event%20Streaming-231F20?style=for-the-badge&logo=apachekafka&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?style=for-the-badge&logo=postgresql&logoColor=white)
![MinIO](https://img.shields.io/badge/MinIO-Object%20Storage-C72E49?style=for-the-badge&logo=minio&logoColor=white)
![Qdrant](https://img.shields.io/badge/Qdrant-Vector%20Store-DC244C?style=for-the-badge&logo=qdrant&logoColor=white)
![Ollama](https://img.shields.io/badge/Ollama-Embeddings-000000?style=for-the-badge&logo=ollama&logoColor=white)
![TEI](https://img.shields.io/badge/HF%20TEI-Cross--Encoder%20Reranker-FFD21E?style=for-the-badge&logo=huggingface&logoColor=black)
![Gemini](https://img.shields.io/badge/Google%20Gemini-RAG%20Answer%20Generation-4285F4?style=for-the-badge&logo=googlegemini&logoColor=white)
![Prometheus](https://img.shields.io/badge/Prometheus-Metrics%20%26%20Health-E6522C?style=for-the-badge&logo=prometheus&logoColor=white)
![Grafana](https://img.shields.io/badge/Grafana-Dashboards-F46800?style=for-the-badge&logo=grafana&logoColor=white)
![Angular](https://img.shields.io/badge/Angular-Web%20UI-DD0031?style=for-the-badge&logo=angular&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?style=for-the-badge&logo=docker&logoColor=white)

</div>

---

# 🚀 Overview

**Distributed Search Engine** is a production-inspired search platform built with **.NET 10** using an **event-driven microservice architecture**.

The platform starts with document uploads and progressively evolves into a complete distributed search engine featuring:

- 🔍 Keyword Search
- 📖 Full-text Search
- ⚡ BM25 Ranking
- 📂 Distributed Indexing
- 🧠 Semantic Search
- 🤖 Vector Embeddings
- 🔄 Hybrid Retrieval
- 💬 Retrieval-Augmented Generation (RAG)

The objective is to build every major search engine component from scratch instead of relying on existing search platforms.

**Where things stand today:** the full pipeline is end to end — a document can be uploaded, stored, chunked, embedded, landed as a filterable vector in Qdrant, and **queried back through a semantic Search API** with cross-encoder re-ranking. On top of that, a **RAG answer endpoint** now takes those re-ranked chunks, builds a token-budgeted prompt, and calls Google Gemini to return a grounded, cited natural-language answer. An **Angular Web UI** now sits in front of both (upload + a chat-style ask page + a live metrics dashboard), so the whole thing is usable from a browser, not just `curl`. Keyword/BM25 search and hybrid retrieval are the next phase.

---

# ✨ Current Features

| Feature | Status |
|---------|:------:|
| 📤 Upload API (multipart, file + department authorization) | ✅ |
| 🪣 Store raw file in MinIO | ✅ |
| 📣 Kafka event publishing (`DocumentIngestion`, `ChunksCreated`) | ✅ |
| ⚙️ Document Ingestion worker (Kafka consumer) | ✅ |
| 🗄 PostgreSQL metadata + chunk storage | ✅ |
| 📄 Text extraction (PDF, DOCX, TXT) | ✅ |
| ✂️ Paragraph-aware chunking (with overlap) | ✅ |
| 🧬 Embedding generation (Ollama, `nomic-embed-text`, 768-dim) | ✅ |
| 🗂 Vector storage (Qdrant, one point per chunk) | ✅ |
| 🔐 Department-based authorization tagging on vectors | ✅ |
| 🔍 Search API (`POST /api/search`) | ✅ |
| 🧠 Semantic Search (query → embed → Qdrant → results) | ✅ |
| 🎯 Cross-encoder re-ranking (TEI, `bge-reranker-v2-m3`) | ✅ |
| 🔐 Department-filtered retrieval on search | ✅ |
| 🧩 Token-budgeted prompt builder over re-ranked chunks | ✅ |
| 💬 RAG Answer API (`POST /api/search/answer`, Google Gemini) | ✅ |
| 📈 Prometheus metrics (`/metrics`) on every .NET service | ✅ |
| 🩺 Dependency-aware health checks (`/health`, `/health/live`) | ✅ |
| 🐳 Per-container CPU/memory/network metrics (cAdvisor) | ✅ |
| 📊 Grafana dashboards (auto-provisioned) | ✅ |
| 📝 Structured JSON logging (Serilog) | ✅ |
| 🖥 Angular Web UI (upload, RAG chat, live metrics) | ✅ |
| ⚡ BM25 / keyword search | ⏳ |
| 🔄 Hybrid retrieval (keyword + semantic) | ⏳ |

---

# 🏛 Architecture

```mermaid
flowchart LR

Client([Browser])

WebUI[Web UI - Angular]

API[Upload Service]

MinIO[(MinIO)]

Kafka1[/Kafka: DocumentIngestion/]

Worker[Document Ingestion Service]

Postgres[(PostgreSQL)]

Kafka2[/Kafka: ChunksCreated/]

Embed[Embedding Service]

Ollama[(Ollama)]

Qdrant[(Qdrant)]

Search[Search Service]

Reranker[(TEI Reranker)]

Gemini[(Google Gemini)]

Client --> WebUI

WebUI -->|Upload file + departments| API

API -->|Store Document| MinIO

API -->|Publish DocumentUploadedEvent| Kafka1

Kafka1 --> Worker

Worker -->|Extract + Chunk| Postgres

Worker -->|Publish ChunksCreatedEvent| Kafka2

Kafka2 --> Embed

Embed -->|Read chunks / write status| Postgres

Embed -->|Generate embeddings| Ollama

Embed -->|Upsert vectors + payload| Qdrant

WebUI -->|Query + departments| Search

WebUI -.->|PromQL, direct from browser| Prometheus[(Prometheus)]

Search -->|Embed query| Ollama

Search -->|Filtered vector search| Qdrant

Search -->|Cross-encoder rerank| Reranker

Search -->|Prompt-build + generate answer| Gemini

style API fill:#00c9a7,color:#000
style Search fill:#00c9a7,color:#000
style WebUI fill:#DD0031,color:#fff
style Worker fill:#203A43,color:#fff
style Embed fill:#203A43,color:#fff
style Kafka1 fill:#231F20,color:#fff
style Kafka2 fill:#231F20,color:#fff
style MinIO fill:#C72E49,color:#fff
style Postgres fill:#4169E1,color:#fff
style Qdrant fill:#DC244C,color:#fff
style Ollama fill:#000000,color:#fff
style Reranker fill:#FFD21E,color:#000
style Gemini fill:#4285F4,color:#fff
style Prometheus fill:#E6522C,color:#fff
```

---

# 🔄 Current Data Flow

```text
              Upload File + Departments
                        │
                        ▼
              ASP.NET Core API (Upload Service)
                        │
                        ▼
              Store File in MinIO
                        │
                        ▼
        Publish DocumentUploadedEvent → Kafka
                        │
                        ▼
              Document Ingestion Service
                        │
          ┌─────────────┼─────────────┐
          ▼             ▼             ▼
   Validate Signature  Extract Text   Chunk Text
   (PDF/DOCX/TXT)      (per type)     (paragraph-aware,
                                       1200 chars, 150 overlap)
          │
          ▼
   Persist chunks + metadata → PostgreSQL
                        │
                        ▼
        Publish ChunksCreatedEvent → Kafka
                        │
                        ▼
              Embedding Service
                        │
          ┌─────────────┼─────────────┐
          ▼             ▼             ▼
   Read chunks       Generate        Read authorized
   from Postgres     embeddings      departments
                      (Ollama)
          │             │             │
          └─────────────┴─────────────┘
                        ▼
        Upsert vectors + payload → Qdrant
                        │
                        ▼
          Status: Embedded (or EmbeddingFailed)


              Query + Departments
                        │
                        ▼
              Search Service
                        │
          ┌─────────────┼─────────────┐
          ▼             ▼             ▼
   Sanitize query   Parse/validate  Embed query
                     departments     (Ollama)
          │             │             │
          └─────────────┴─────────────┘
                        ▼
      Filtered vector search (Qdrant, by department)
                        ▼
      Cross-encoder rerank (TEI, bge-reranker-v2-m3)
                        ▼
                 Top-K Search Results
                        │
                        │   (POST /api/search/answer only)
                        ▼
         Prompt Builder (token-budgeted context packing)
                        ▼
         Generate answer (Google Gemini)
                        ▼
         Answer + cited Sources
```

---

# 🧩 Services at a Glance

| Service | Type | Port | Responsibility |
|---|---|---|---|
| **Upload Service** | ASP.NET Core Web API | `8080` | Validates + accepts uploads, stores the file in MinIO, publishes `DocumentUploadedEvent` |
| **Document Ingestion Service** | Background worker + minimal HTTP (`/health`, `/metrics`) | `8083` | Downloads the file, extracts text, chunks it, persists chunks/metadata to Postgres, publishes `ChunksCreatedEvent` |
| **Embedding Service** | Background worker + minimal HTTP (`/health`, `/metrics`) | `8084` | Reads chunks for a document, generates embeddings via Ollama, upserts vectors + payload into Qdrant, tracks status |
| **Search Service** | ASP.NET Core Web API | `8081` | Embeds the query (Ollama), runs a department-filtered vector search against Qdrant, re-ranks candidates via a TEI cross-encoder, returns top-K results (`POST /api/search`); optionally builds a token-budgeted prompt from those chunks and generates a grounded, cited answer via Google Gemini (`POST /api/search/answer`) |
| **Web UI** | Angular SPA (nginx-served) | `4200` | Browser client: upload page, RAG chat ("Ask") page, live metrics dashboard |

### External inference dependencies

| Component | Role | Port |
|---|---|---|
| **Ollama** (`nomic-embed-text`) | Embeds document chunks (Embedding Service) and queries (Search Service) into 768-dim vectors | `11434` |
| **TEI Reranker** (`BAAI/bge-reranker-v2-m3`) | Cross-encoder re-scores Qdrant's top candidates against the raw query for true relevance | `8082` |
| **Google Gemini** (`gemini-flash-lite-latest`) | Generates a cited natural-language answer from the re-ranked chunks (`POST /api/search/answer` only) | — (hosted API) |

> ⚠️ First boot note: the TEI reranker container downloads the model on first start. `bge-reranker-v2-m3` has no published ONNX weights, so TEI falls back to safetensors + CPU (Candle backend) — first-time download + warmup can take **10–15 minutes**. `search-service` calls will `500` with `Connection refused (reranker:80)` until `GET http://localhost:8082/health` returns `200`.

### Kafka topics

| Topic | Producer | Consumer | Payload |
|---|---|---|---|
| `DocumentIngestion` | Upload Service | Document Ingestion Service | `DocumentUploadedEvent` — `DocumentId`, `FileName`, `ContentType`, `AuthorizedDepartments`, `UploadedAtUtc` |
| `ChunksCreated` | Document Ingestion Service | Embedding Service | `ChunksCreatedEvent` — `DocumentId`, `ChunkCount`, `CreatedAtUtc` |

---

# 📊 Observability

Every .NET service exposes the same three endpoints (via a shared `Common.Extensions` wiring, `src/BuildingBlocks/Common/Extensions/`):

| Endpoint | Purpose |
|---|---|
| `GET /metrics` | Prometheus exposition format — generic HTTP request rate/latency (`prometheus-net`) plus one domain-specific counter per service (`documents_uploaded_total`, `documents_ingested_total`, `chunks_embedded_total`, `rag_answers_generated_total`) |
| `GET /health` | Real dependency checks (Postgres, Kafka, Qdrant, MinIO, Ollama, TEI reranker; Gemini is a config-presence check only — see note below) as readable JSON, e.g. `{ "status": "Healthy", "checks": [...] }` |
| `GET /health/live` | Liveness only — always `200` if the process is up, no dependency calls |

Health results are also republished as a `health_check_status` Prometheus gauge (1 = healthy, 0.5 = degraded, 0 = unhealthy) every 15s, so health shows up in Grafana from the same datasource as everything else — no separate JSON-API datasource needed.

> ⚠️ Gemini's health check never makes a live API call — it only checks that `GEMINI_API_KEY` is configured. Free-tier Gemini quotas are tight enough that a live call on every 15s health poll would compete with actual answer generation for the same budget.

### Stack

| Component | Role | Port |
|---|---|---|
| **Prometheus** | Scrapes `/metrics` from all 4 .NET services + cAdvisor every 15s | `9090` |
| **Grafana** | Auto-provisioned Prometheus datasource + a "System Overview" dashboard (per-service health, request rate/latency, domain counters, per-container CPU/memory/network) | `3000` (default login `admin` / `GRAFANA_ADMIN_PASSWORD`) |
| **cAdvisor** | Reports CPU/memory/network for every container in the stack (not just the .NET services) | `8085` |

Config lives in `observability/prometheus/prometheus.yml` and `observability/grafana/provisioning/`. Structured logging (Serilog, JSON to console) is wired into every service but isn't shipped anywhere yet — check logs via `docker compose logs <service>`; log aggregation (e.g. Loki) is a deliberately deferred follow-up.

> ⚠️ **cAdvisor on Docker Desktop (Windows/Mac)**: cAdvisor's per-container CPU/memory/network breakdown relies on inspecting each container's overlay filesystem layer directly, which only works reliably on a native Linux Docker host. On Docker Desktop for Windows/Mac (containers running inside an internal VM), cAdvisor can't resolve individual container layers and only reports Docker Desktop's own internal cgroup slices (`/docker`, `/kubepods`, ...) instead of per-service names — the "Container Resources" dashboard row will be empty/unhelpful there. Everything else (health, metrics, dashboards, per-service panels) is unaffected and works identically on any platform.

---

# 🖥 Web UI

An Angular SPA (`src/Services/WebUI`) at `http://localhost:4200`, containerized and served via nginx — three pages:

| Page | Route | Calls | Description |
|---|---|---|---|
| **Upload** | `/upload` | `POST http://localhost:8080/api/FileHandler/upload` | Drag-and-drop file zone + multi-select department picker |
| **Ask** | `/ask` | `POST http://localhost:8081/api/search/answer` | Chat-style RAG Q&A; pick "your department" (single-select stand-in until real auth exists), see the cited answer + sources |
| **Metrics** | `/metrics` | Prometheus HTTP API directly (`http://localhost:9090`) | Fully custom dashboard (not a Grafana embed) — service health tiles, domain counters, request rate/latency, per-container resources — polling every 15s |

**Stack:** Angular 19 (standalone components, signals), Angular Material + Tailwind CSS for styling, `ngx-echarts`/Apache ECharts for the metrics charts.

The browser always talks to the host-mapped ports (`localhost:8080`/`:8081`/`:9090`), never the internal docker-network hostnames — configured once in `src/environments/environment.ts`. This is also the first browser client in the repo, so CORS is enabled specifically for the UI's origin on `UploadService`, `SearchService` (`Cors:WebUiOrigin` config, defaults to `http://localhost:4200`), and Prometheus (`--web.cors.origin` flag in `docker-compose.yml`).

> Note: there's no authentication anywhere in this repo yet. The Ask page's department picker is a manual stand-in for "the logged-in user's department" — a placeholder until real auth is added, not a security boundary.

---

# 🗄 Data Model

### PostgreSQL — `document_metadata`

One row per uploaded document. Owned by Document Ingestion Service; Embedding Service reads/updates it too (shared database, no shared code between the two services).

| Column | Type | Notes |
|---|---|---|
| `document_id` | varchar(600) | **PK**, = MinIO object name |
| `file_name` | varchar(512) | |
| `content_type` | varchar(256) | |
| `authorized_departments` | int | `Department` flags enum |
| `uploaded_at_utc` | timestamp | |
| `ingested_at_utc` | timestamp | |
| `status` | int | `DocumentProcessingStatus` (see below) |
| `error_message` | varchar(2048) | nullable |

### PostgreSQL — `document_chunks`

One row per chunk. Unique on `(document_id, chunk_index)`, cascades on document delete.

| Column | Type | Notes |
|---|---|---|
| `id` | uuid | **PK**, also used as the Qdrant point id |
| `document_id` | varchar(600) | **FK** → `document_metadata` |
| `chunk_index` | int | 0-based order within the document |
| `content` | text | the chunk's text |
| `char_count` | int | |
| `created_at_utc` | timestamp | |

### Qdrant — collection `document_chunks`

768 dimensions, Cosine distance, one point per chunk (`id` = the chunk's Postgres `id`).

| Payload key | Type | Notes |
|---|---|---|
| `documentId` | string | |
| `chunkIndex` | int | |
| `content` | string | same text that was embedded |
| `createdAtUtc` | string | ISO-8601 |
| `authorizedDepartments` | string[] | flag names (e.g. `["Finance", "Engineering"]`), not the raw bitmask — enables a `MatchAny` filter against a caller's department once search exists |

### Document status lifecycle (`DocumentProcessingStatus`)

```text
0 Pending → 1 Processing → 2 Chunked → 3 Failed
                          → 4 Unsupported

2 Chunked → 5 Embedding → 6 Embedded
                         → 7 EmbeddingFailed
```

---

# 🏗 Repository Structure

```text
src
│
├── BuildingBlocks
│   ├── SharedKernel        # Kafka topic constants, cross-cutting constants
│   ├── Contracts           # Shared events (DocumentUploadedEvent, ChunksCreatedEvent)
│   │                       # and enums (DocumentProcessingStatus, Department)
│   ├── Infrastructure      # IKafkaProducer, IFileStorage, IMinioStorage, IEmbeddingGenerator
│   └── Common              # File validation, GUID generation, department parsing, text sanitization,
│                           # shared observability wiring (Extensions/) — Prometheus health-check
│                           # publisher, /health JSON writer, used identically by all 4 services
│
├── observability
│   ├── prometheus          # prometheus.yml scrape config
│   └── grafana/provisioning  # datasource + auto-provisioned dashboard
│
├── Services
│   ├── UploadService              # Web API — upload endpoint
│   ├── DocumentIngestionService    # Worker — extract, chunk, persist
│   ├── EmbeddingService            # Worker — embed, upsert to Qdrant
│   ├── SearchService                # Web API — embed query, vector search, rerank,
│   │                                # prompt build + Gemini answer generation
│   └── WebUI                        # Angular SPA — upload, RAG chat, live metrics dashboard
│
├── Tests
│   ├── UploadService.Tests
│   ├── DocumentIngestionService.Tests
│   ├── EmbeddingService.Tests
│   └── SearchService.Tests
│
└── docker-compose.yml
```

---

# 🛠 Tech Stack

| Layer | Technology |
|--------|------------|
| Language | C# |
| Framework | .NET 10 |
| Messaging | Apache Kafka |
| Database | PostgreSQL |
| Object Storage | MinIO |
| Embedding Model Runtime | Ollama (`nomic-embed-text`, 768-dim) |
| Vector Store | Qdrant (Cosine similarity) |
| Re-ranking | Hugging Face Text Embeddings Inference (`BAAI/bge-reranker-v2-m3`) |
| RAG Answer Generation | Google Gemini (`gemini-flash-lite-latest`, free tier) |
| Metrics | Prometheus + `prometheus-net.AspNetCore` |
| Dashboards | Grafana (auto-provisioned) |
| Container Metrics | cAdvisor |
| Structured Logging | Serilog (JSON to console) |
| Web UI | Angular 19 (standalone, signals) |
| UI Styling | Angular Material + Tailwind CSS |
| UI Charts | ngx-echarts (Apache ECharts) |
| Containerization | Docker / Docker Compose |
| Architecture | Microservices, event-driven |
| Future Search | BM25, hybrid retrieval |

---

# 🎯 Design Principles

- Clean Architecture
- Event-Driven Design
- SOLID Principles
- Dependency Injection
- Interface-Based Infrastructure
- Asynchronous Processing
- Loose Coupling
- High Cohesion
- Production-Inspired Engineering

---

# 🗺 Roadmap

## Phase 1 — Upload Platform

- [x] Upload API
- [x] MinIO Storage
- [x] Kafka Producer
- [x] Docker Infrastructure

## Phase 2 — Document Processing

- [x] Kafka Consumer
- [x] Metadata Storage
- [x] Text Extraction
- [x] Parsing Pipeline (chunking)

## Phase 3 — Search Engine

- [ ] Tokenization
- [ ] Stop-word Removal
- [ ] Inverted Index
- [ ] Boolean Search
- [ ] Phrase Search
- [ ] Prefix Search

> Note: keyword/BM25 search (this phase) is still pending — what's live today is the **semantic** path, tracked under Phase 6 below.

## Phase 4 — Ranking

- [ ] TF
- [ ] IDF
- [ ] TF-IDF
- [ ] BM25
- [ ] Top-K Retrieval

## Phase 5 — Distributed Search

- [ ] Sharding
- [ ] Replication
- [ ] Query Fan-out
- [ ] Distributed Index Updates

## Phase 6 — AI Search

- [x] Embedding Generation
- [x] Vector Store
- [x] Semantic Search (query API)
- [x] Re-ranking (cross-encoder via TEI)
- [x] RAG (prompt builder + Google Gemini answer generation)
- [ ] Hybrid Retrieval (blend with keyword search)

---

# 🚀 Getting Started

### Clone

```bash
git clone https://github.com/Sanket9326/Distributed-Search-Engine.git
```

### Configure

```bash
cp .env.example .env
```

Set `GEMINI_API_KEY` in `.env` to a free key from [Google AI Studio](https://aistudio.google.com/apikey) if you want the RAG answer endpoint (`POST /api/search/answer`) to work — plain semantic search (`POST /api/search`) doesn't need it.

### Start Infrastructure + Services

```bash
docker compose up -d --build
```

This brings up Postgres, pgAdmin, MinIO, Kafka, Qdrant, Ollama, the TEI reranker, all four .NET services (Upload, Document Ingestion, Embedding, Search), the Angular Web UI, and the observability stack (Prometheus, Grafana, cAdvisor).

On first run:
- **Ollama** needs the `nomic-embed-text` model pulled — `docker exec -it document-search-ollama ollama pull nomic-embed-text` if it isn't already cached.
- **TEI reranker** downloads `BAAI/bge-reranker-v2-m3` on first start; this model has no ONNX weights published, so TEI falls back to safetensors on CPU — expect **10–15 minutes** before it's ready. Poll `http://localhost:8082/health` (expect `200`) before calling the Search Service, or its requests will fail with `500 Connection refused (reranker:80)`.

### Build & Test locally

```bash
dotnet build
dotnet test
```

### Try it

The fastest way is the Web UI at `http://localhost:4200` — an **Upload** page, a chat-style **Ask** page, and a live **Metrics** dashboard, covering everything below without needing `curl`/Postman. The steps below show the same flow via raw HTTP, useful for scripting or understanding the exact contracts the UI itself calls.

**1. Upload a document**

```
POST http://localhost:8080/api/FileHandler/upload
Content-Type: multipart/form-data

file: <your .pdf / .docx / .txt>
departments: Finance,Engineering   # optional, comma-separated
```

Valid department values (case-insensitive): `HumanResources`, `Finance`, `Engineering`, `Legal`, `Sales`, `Marketing`, `Operations`, `ExecutiveManagement`. This must go in the multipart **form body**, not the query string — `[FromForm]` binding ignores query params, and an omitted/mismatched value silently resolves to `Department.None` (no authorized departments).

Check progress:
- **Postgres** (`document_metadata.status`) — via pgAdmin at `http://localhost:5050`
- **Qdrant** — built-in dashboard at `http://localhost:6333/dashboard`

**2. Search it** (once status reaches `Embedded` — ingestion + embedding are async over Kafka)

```
POST http://localhost:8081/api/search
Content-Type: application/json

{
  "query": "your question about the document",
  "departments": ["Finance"],
  "topK": 5
}
```

`departments` here must overlap what the document was uploaded with, or the result set is empty by design (department is an authorization filter, not a ranking signal).

**3. Get a generated answer instead of raw chunks** (requires `GEMINI_API_KEY` in `.env`)

```
POST http://localhost:8081/api/search/answer
Content-Type: application/json

{
  "query": "your question about the document",
  "departments": ["Finance"],
  "topK": 5
}
```

Response is `{ "answer": "...", "sources": [ { "chunkId", "documentId", "fileName", "chunkIndex", "score" } ] }` — `sources` only lists the chunks that actually made it into the prompt (some low-ranked chunks may be dropped if they don't fit the token budget). If no authorized chunks are found, `answer` is a fixed "no relevant information" message and Gemini is never called.

**4. Watch it all live**

Open `http://localhost:4200/metrics` for the built-in Web UI dashboard, or `http://localhost:3000` for Grafana (login `admin` / whatever you set `GRAFANA_ADMIN_PASSWORD` to, dashboard auto-provisioned). Both show live health status per service, request rate/latency, the domain counters above (documents uploaded/ingested, chunks embedded, RAG answers generated), and per-container CPU/memory/network from cAdvisor. Prometheus itself is at `http://localhost:9090` if you want to run raw PromQL queries or check `/targets` for scrape health.

---

# 📈 Future Architecture

```text
                   Search API
                        │
                        ▼
              Hybrid Query Engine
              ┌─────────┴─────────┐
              ▼                   ▼
       Keyword Search      Semantic Search  ✅ live today
              │                   │
      Inverted Index        Qdrant Vector Store
        (⏳ pending)               │
              └─────────┬─────────┘
                        ▼
                   Re-ranking          ✅ live today (TEI cross-encoder)
                        │
                        ▼
                 Final Search Results
                        │
                        ▼
                RAG Answer Generation  ✅ live today (prompt builder + Google Gemini)
```

---

# 📖 Why this project?

Modern search systems are much more than simple databases.

This repository is an educational journey into how production-grade search engines are designed using distributed systems, asynchronous messaging, information retrieval algorithms, and AI-powered semantic search.

Rather than depending on existing search engines, the goal is to implement many core components from first principles to understand how modern search platforms work internally.

---

<div align="center">

### ⭐ If you like this project, consider giving it a Star!

<img src="https://capsule-render.vercel.app/api?type=waving&height=110&section=footer&color=0:00C9A7,50:203A43,100:0F2027"/>

</div>
