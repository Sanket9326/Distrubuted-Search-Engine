<div align="center">

<img src="https://capsule-render.vercel.app/api?type=waving&color=0:0f2027,50:2c5364,100:00c9a7&height=220&section=header&text=Distributed%20Search%20Engine&fontSize=46&fontColor=ffffff&fontAlignY=38&desc=Hybrid%20Keyword%20%2B%20Semantic%20Document%20Search%20Platform&descAlignY=58&descSize=18&animation=fadeIn" width="100%" alt="header"/>

<a href="https://github.com/Sanket9326/Distrubuted-Search-Engine">
  <img src="https://readme-typing-svg.demolab.com?font=Fira+Code&size=20&pause=1000&color=00C9A7&center=true&vCenter=true&width=700&lines=Upload+%E2%86%92+Store+%E2%86%92+Publish+%E2%86%92+Ingest+%E2%86%92+Search;Event-driven+microservices+on+.NET+10;MinIO+object+storage+%7C+Kafka+event+bus+%7C+PostgreSQL" alt="Typing SVG" />
</a>

<br/>

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Kafka](https://img.shields.io/badge/Apache%20Kafka-Event%20Bus-231F20?style=for-the-badge&logo=apachekafka&logoColor=white)
![MinIO](https://img.shields.io/badge/MinIO-Object%20Storage-C72E49?style=for-the-badge&logo=minio&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?style=for-the-badge&logo=postgresql&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?style=for-the-badge&logo=docker&logoColor=white)

</div>

---

## Overview

**Distributed Search Engine** is a microservice-based system that will evolve into a hybrid **keyword + semantic** document search platform. Documents are uploaded through an HTTP API, streamed into object storage, announced on an event bus, and picked up asynchronously downstream for ingestion and (eventually) indexing.

<table align="center">
  <tr>
    <td align="center">📤<br/><b>Upload</b><br/><sub>ASP.NET Core API</sub></td>
    <td align="center">➡️</td>
    <td align="center">🪣<br/><b>Store</b><br/><sub>MinIO (S3-compatible)</sub></td>
    <td align="center">➡️</td>
    <td align="center">📣<br/><b>Publish</b><br/><sub>Apache Kafka</sub></td>
    <td align="center">➡️</td>
    <td align="center">⚙️<br/><b>Ingest</b><br/><sub>Worker Service</sub></td>
    <td align="center">➡️</td>
    <td align="center">🔎<br/><b>Search</b><br/><sub>Keyword + Semantic</sub></td>
  </tr>
</table>

## Table of Contents

- [Architecture](#architecture)
- [Solution Layout](#solution-layout)
- [Status](#status)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [Verifying the Flow](#verifying-the-flow)
- [Coding Standards](#coding-standards)

## Architecture

```mermaid
flowchart LR
    Client([Client]) -->|multipart/form-data| Upload[UploadService]
    Upload -->|PutObject| Minio[(MinIO)]
    Upload -->|DocumentUploadedEvent| Kafka[/Apache Kafka/]
    Kafka --> Ingestion[DocumentIngestionService]
    Ingestion --> Postgres[(PostgreSQL)]
    Ingestion -.future.-> Index[(Search Index)]

    style Upload fill:#00c9a7,stroke:#0f2027,color:#0f2027
    style Ingestion fill:#2c5364,stroke:#0f2027,color:#fff
    style Minio fill:#c72e49,stroke:#0f2027,color:#fff
    style Kafka fill:#231f20,stroke:#0f2027,color:#fff
    style Postgres fill:#4169e1,stroke:#0f2027,color:#fff
    style Index fill:#555,stroke:#0f2027,color:#fff,stroke-dasharray: 5 5
```

## Solution Layout

```
src/
  BuildingBlocks/
    SharedKernel/     Base types shared across all services (BaseEntity, Result<T>, Error, exceptions)
    Contracts/        Cross-service DTOs and Kafka event contracts
    Infrastructure/   Shared infrastructure abstractions (Kafka, MinIO, file storage, clock)
    Common/           Cross-cutting extensions, middleware, logging, correlation ID support
  Services/
    UploadService/               ASP.NET Core Web API — upload → MinIO → Kafka
    DocumentIngestionService/    Worker service that consumes upload events and ingests documents
  Tests/
    UploadService.Tests/
    DocumentIngestionService.Tests/

docker-compose.yml   PostgreSQL, MinIO, Kafka, and service containers
```

## Status

| Component | State |
|---|---|
| ✅ Upload API (`UploadService`) | Implemented |
| ✅ MinIO object storage integration | Implemented |
| ✅ Kafka event publishing (`DocumentUploadedEvent`) | Implemented |
| 🚧 Document ingestion worker | Scaffolded, no logic yet |
| ⏳ PostgreSQL persistence layer | Not started |
| ⏳ Keyword search indexing | Not started |
| ⏳ Semantic search | Not started |

## Getting Started

**1. Configure environment**

```bash
cp .env.example .env
```

**2. Start local infrastructure** (PostgreSQL, MinIO, Kafka)

```bash
docker compose up -d postgres minio kafka
```

**3. Build & test**

```bash
dotnet build
dotnet test
```

**4. Run a service**

```bash
cd src/Services/UploadService
dotnet run
```

> Or bring the whole stack up in containers with `docker compose up -d`.

## Configuration

`UploadService` reads MinIO and Kafka settings from configuration (`appsettings.Development.json` locally, environment variables in Docker):

| Key | Purpose |
|---|---|
| `Kafka__BootstrapServers` | Kafka broker address |
| `Minio__Endpoint` | MinIO API endpoint |
| `Minio__AccessKey` / `Minio__SecretKey` | MinIO credentials |
| `Minio__BucketName` | Target bucket (auto-created if missing) |

## Verifying the Flow

```bash
# Upload a file
curl -F "file=@sample.pdf" http://localhost:5230/api/FileHandler/upload

# Watch the event land on the DocumentIngestion topic
docker exec -it document-search-kafka \
  /opt/kafka/bin/kafka-console-consumer.sh \
  --bootstrap-server localhost:9092 \
  --topic DocumentIngestion --from-beginning
```

## Coding Standards

- Target framework: **.NET 10**
- Nullable reference types and implicit usings enabled solution-wide
- Warnings are treated as errors (`Directory.Build.props`)
- File-scoped namespaces enforced via `.editorconfig`

---

<div align="center">
<img src="https://capsule-render.vercel.app/api?type=waving&color=0:00c9a7,50:2c5364,100:0f2027&height=100&section=footer" width="100%" alt="footer"/>
</div>
