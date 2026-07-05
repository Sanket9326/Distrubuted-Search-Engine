# 🚀 Document Search Platform

> A production-inspired, microservice-based distributed document search platform built with **.NET 10**, designed to evolve into a scalable **hybrid keyword + semantic search engine**.

The platform follows an event-driven architecture where documents are uploaded, stored in object storage, processed asynchronously, indexed, and made searchable through both traditional information retrieval techniques and AI-powered semantic search.

---

# ✨ Vision

The goal of this project is to build a search platform that demonstrates how modern search engines are designed and implemented.

The platform will progressively evolve from a simple document upload service into a distributed search system featuring:

* Keyword Search
* Full-text Search
* BM25 Ranking
* Inverted Indexes
* Distributed Indexing
* Semantic Search
* Vector Embeddings
* Hybrid Search
* Retrieval-Augmented Generation (RAG)

Rather than being a tutorial project, this repository is intended to resemble the architecture and engineering practices used in production systems.

---

# 🏗️ High-Level Architecture

```text
                   +----------------------+
                   |      Client/API      |
                   +----------+-----------+
                              |
                              v
                  +-------------------------+
                  |     Upload Service      |
                  +-----------+-------------+
                              |
          +-------------------+-------------------+
          |                                       |
          v                                       v
 +--------------------+                 +--------------------+
 |       MinIO        |                 |    PostgreSQL      |
 | Object Storage     |                 | Document Metadata  |
 +--------------------+                 +--------------------+
                              |
                              |
                              v
                      +---------------+
                      |     Kafka     |
                      +-------+-------+
                              |
                              v
             +----------------------------------+
             | Document Ingestion Service       |
             | Extract • Parse • Process        |
             +---------------+------------------+
                             |
                             v
                     Search Index (Future)
                             |
                             v
                     Search API (Future)
```

---

# 📦 Technology Stack

## Backend

* .NET 10
* ASP.NET Core Web API
* Worker Services
* C#

## Messaging

* Apache Kafka

## Storage

* PostgreSQL
* MinIO (S3-compatible Object Storage)

## Containerization

* Docker
* Docker Compose

## Future Technologies

* Redis
* Elasticsearch-like Inverted Index
* Vector Database
* OpenTelemetry
* Prometheus
* Grafana

---

# 📂 Repository Structure

```text
DocumentSearchPlatform
│
├── src
│   ├── BuildingBlocks
│   │   ├── SharedKernel
│   │   ├── Contracts
│   │   ├── Infrastructure
│   │   └── Common
│   │
│   ├── Services
│   │   ├── UploadService
│   │   └── DocumentIngestionService
│   │
│   └── Tests
│       ├── UploadService.Tests
│       └── DocumentIngestionService.Tests
│
├── infrastructure
│   ├── docker-compose.yml
│   └── .env.example
│
├── docs
│
└── README.md
```

---

# 📚 Building Blocks

## SharedKernel

Contains domain primitives shared across all services.

Examples:

* BaseEntity
* Result<T>
* Error
* Domain Exceptions

---

## Contracts

Contains communication contracts shared between services.

Examples:

* Kafka Events
* Integration Events
* Shared DTOs

---

## Infrastructure

Provides reusable infrastructure abstractions.

Examples:

* Kafka Producer
* Kafka Consumer
* MinIO Client
* File Storage
* Clock
* Configuration

---

## Common

Contains cross-cutting functionality used across services.

Examples:

* Middleware
* Logging
* Correlation IDs
* Extensions
* Health Checks

---

# 🚀 Services

## Upload Service

Responsible for receiving document uploads.

Responsibilities:

* Validate uploaded files
* Store files in MinIO
* Save document metadata
* Publish upload events to Kafka

---

## Document Ingestion Service

Consumes upload events and prepares documents for indexing.

Responsibilities:

* Download documents from MinIO
* Extract text
* Parse documents
* Build search indexes
* Generate embeddings (future)

---

# 🛣️ Development Roadmap

## ✅ Phase 1 — Document Upload

* Upload API
* Local File Storage
* MinIO Integration
* PostgreSQL Metadata
* Kafka Event Publishing

---

## 🚧 Phase 2 — Document Ingestion

* Kafka Consumer
* File Download
* Text Extraction
* Parsing Pipeline
* Metadata Enrichment

---

## ⏳ Phase 3 — Keyword Search Engine

* Tokenization
* Stop Word Removal
* Stemming
* Inverted Index
* Boolean Search
* Phrase Search
* Prefix Search

---

## ⏳ Phase 4 — Ranking

* TF
* IDF
* TF-IDF
* BM25
* Top-K Retrieval

---

## ⏳ Phase 5 — Distributed Search

* Sharding
* Replication
* Query Fan-out
* Fault Tolerance
* Distributed Index Updates

---

## ⏳ Phase 6 — Semantic Search

* Embedding Generation
* Vector Storage
* Hybrid Search
* Re-ranking
* Retrieval-Augmented Generation (RAG)

---

# 🐳 Local Development

## Prerequisites

* .NET 10 SDK
* Docker Desktop
* Git

---

## Clone Repository

```bash
git clone <repository-url>
cd DocumentSearchPlatform
```

---

## Configure Environment

Copy the example environment file.

```bash
cp .env.example .env
```

Update the values if required.

---

## Start Infrastructure

```bash
docker compose up -d
```

This starts:

* PostgreSQL
* MinIO
* Kafka

---

## Build

```bash
dotnet build
```

---

## Run Upload Service

```bash
cd src/Services/UploadService

dotnet run
```

---

## Run Document Ingestion Service

```bash
cd src/Services/DocumentIngestionService

dotnet run
```

---

## Run Tests

```bash
dotnet test
```

---

# 🧪 Engineering Practices

* Clean Architecture principles
* SOLID design
* Dependency Injection
* Event-Driven Architecture
* Asynchronous Processing
* Separation of Concerns
* Strongly Typed Contracts
* Interface-Based Infrastructure
* Nullable Reference Types Enabled
* Warnings Treated as Errors

---

# 📖 Documentation

Additional documentation will be added under the `docs/` directory, including:

* Architecture Diagrams
* Sequence Diagrams
* API Documentation
* Indexing Pipeline
* Search Pipeline
* Deployment Guides
* Design Decisions

---

# 📌 Current Status

The repository is under active development.

Current progress includes:

* Solution structure
* Project scaffolding
* Docker-based local infrastructure
* Initial Upload Service implementation

Upcoming work focuses on MinIO integration, metadata persistence, Kafka event publishing, and the document ingestion pipeline.

---

# 📄 License

This project is intended for learning, experimentation, and demonstrating distributed systems and search engine architecture. Choose and add an open-source license (such as MIT or Apache-2.0) before publishing if you plan to make the repository public.
