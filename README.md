# DocumentSearchPlatform

A microservice-based distributed system that will evolve into a hybrid keyword + semantic document search engine. Documents are uploaded via an API, published as events, ingested asynchronously, and (eventually) indexed for keyword and semantic search.

This repository currently contains solution scaffolding only — no business logic has been implemented yet.

## Solution Layout

```
src/
  BuildingBlocks/
    SharedKernel/     Base types shared across all services (BaseEntity, Result<T>, Error, exceptions)
    Contracts/        Cross-service DTOs and Kafka event contracts
    Infrastructure/   Shared infrastructure abstractions (Kafka, MinIO, file storage, clock)
    Common/           Cross-cutting extensions, middleware, logging, correlation ID support
  Services/
    UploadService/               ASP.NET Core Web API for document upload
    DocumentIngestionService/    Worker service that consumes upload events and ingests documents
  Tests/
    UploadService.Tests/
    DocumentIngestionService.Tests/

infrastructure/     Docker/Kafka/Postgres/MinIO configuration
docs/               Architecture notes, API docs, diagrams
```

## Getting Started

1. Copy `.env.example` to `.env` and adjust credentials as needed.
2. Start local infrastructure: `docker compose up -d`
3. Build the solution: `dotnet build`
4. Run tests: `dotnet test`

## Coding Standards

- Target framework: .NET 10
- Nullable reference types and implicit usings enabled solution-wide
- Warnings are treated as errors (`Directory.Build.props`)
- File-scoped namespaces enforced via `.editorconfig`
