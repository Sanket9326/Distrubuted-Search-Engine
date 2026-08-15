# Distributed Search Engine

[![CI](https://github.com/Sanket9326/Distrubuted-Search-Engine/actions/workflows/ci.yaml/badge.svg)](https://github.com/Sanket9326/Distrubuted-Search-Engine/actions/workflows/ci.yaml)
[![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Kubernetes](https://img.shields.io/badge/Kubernetes-Helm%20%2B%20Argo%20CD-326CE5?logo=kubernetes&logoColor=white)](https://kubernetes.io/)

A production-inspired, event-driven search platform built with .NET. It accepts documents, processes them asynchronously, and provides department-aware hybrid search and grounded RAG answers.

## Highlights

- Upload and extract PDF, DOCX, and TXT documents
- Kafka-based ingestion pipeline with Redis retries and dead-letter topics
- Hybrid retrieval: BM25 keyword index in PostgreSQL plus vector search in Qdrant
- Ollama embeddings, TEI cross-encoder reranking, and optional Google Gemini answers
- Angular UI, Prometheus metrics, Grafana dashboards, and health checks
- Docker Compose for development; Helm, Argo CD, and kind for GitOps deployment

## Architecture

```mermaid
flowchart LR
    Client[Browser] --> UI[Angular Web UI]
    UI --> Upload[Upload Service]
    Upload --> MinIO[(MinIO)]
    Upload --> Kafka{{Kafka}}
    Kafka --> Ingest[Document Ingestion]
    Ingest --> Postgres[(PostgreSQL)]
    Ingest --> Embed[Embedding Service]
    Ingest --> Index[Keyword Index Service]
    Embed --> Ollama[Ollama]
    Embed --> Qdrant[(Qdrant)]
    Index --> Postgres

    UI --> Search[Search Service]
    Search --> Ollama
    Search --> Qdrant
    Search --> Postgres
    Search --> Reranker[TEI Reranker]
    Reranker --> Gemini[Gemini]

    Ingest -. retries .-> Redis[(Redis)]
    Embed -. retries .-> Redis
    Index -. retries .-> Redis
    Redis --> Reliability[Reliability Service]
    Reliability --> Kafka
```

Failures in ingestion, embedding, and indexing are scheduled in Redis and retried by the Reliability Service. Messages that exceed the retry limit are sent to Kafka dead-letter topics.

## Tech stack

| Area | Technology |
| --- | --- |
| Services | .NET 10, ASP.NET Core, Kafka |
| Storage | PostgreSQL, MinIO, Qdrant, Redis |
| Search and AI | BM25, Ollama (`nomic-embed-text`), TEI reranker, Google Gemini |
| UI and observability | Angular, Prometheus, Grafana |
| Delivery | Docker Compose, Kubernetes, Helm, Argo CD, GitHub Actions |

## Quick start: Docker Compose

### Prerequisites

- Docker Desktop with Docker Compose
- .NET 10 SDK (for local builds/tests)
- A Gemini API key only if you want generated RAG answers

```bash
git clone https://github.com/Sanket9326/Distributed-Search-Engine.git
cd Distributed-Search-Engine
cp .env.example .env
docker compose up -d --build
```

Set `GEMINI_API_KEY` in `.env` to enable `POST /api/search/answer`.

Open the Web UI at [http://localhost:4200](http://localhost:4200). On the first run, Ollama and the reranker download their models, so search may not be ready immediately.

Useful local endpoints:

| Service | URL |
| --- | --- |
| Web UI | http://localhost:4200 |
| Upload API | http://localhost:8080 |
| Search API | http://localhost:8081 |
| Qdrant dashboard | http://localhost:6333/dashboard |
| Prometheus | http://localhost:9090 |
| Grafana | http://localhost:3000 |

Run the test suite with:

```bash
dotnet test
```

## Use the API

Upload a document and assign the departments allowed to search it:

```bash
curl -X POST http://localhost:8080/api/FileHandler/upload \
  -F "file=@document.pdf" \
  -F "departments=Engineering,Finance"
```

Search indexed documents:

```bash
curl -X POST http://localhost:8081/api/search \
  -H "Content-Type: application/json" \
  -d '{"query":"What does the document say?","departments":["Engineering"],"topK":5}'
```

For a cited generated answer, send the same body to `POST /api/search/answer`. A document appears in results only when the requested departments overlap its upload departments.

## Kubernetes and GitOps

The `deploy/` directory contains Helm charts, Argo CD Applications, and a kind bootstrap script. The standard local GitOps flow uses images already published by GitHub Actions:

```powershell
.\deploy\kind\bootstrap.ps1 -UseRegistryImages
```

For a clean local cluster:

```powershell
.\deploy\kind\bootstrap.ps1 -UseRegistryImages -Recreate
```

The bootstrap installs ingress-nginx, Metrics Server, and Argo CD, then deploys the application charts. First cluster creation can take several minutes because the Ollama image and embedding model are large.

Verify the deployment:

```powershell
kubectl get applications -n argocd
kubectl get pods -n search-engine
```

All Argo CD applications should report `Synced` and `Healthy`.

To access the UI through ingress, add `127.0.0.1 search.local` to the hosts file and open `http://search.local:8080`. Alternatively:

```powershell
kubectl port-forward -n search-engine svc/web-ui-web-ui 8081:80
```

## CI/CD

GitHub Actions provides the delivery pipeline:

1. **CI** runs .NET tests, builds the Angular UI, and validates container builds for pull requests and `master` pushes.
2. **CD** publishes application images to GHCR using the commit SHA, updates Helm `image.tag` values, and commits the GitOps change to `master`.
3. **Argo CD** watches `master` and automatically synchronizes the new Helm values to the cluster.

For registry-based deployments, make the GHCR packages public or configure an image-pull secret in Kubernetes.

## Repository layout

```text
src/
  Services/          Application services and Angular Web UI
  BuildingBlocks/    Shared contracts, infrastructure, and cross-cutting code
  Tests/             Service and search tests
  observability/     Prometheus and Grafana configuration
deploy/
  helm/              Helm charts for services and infrastructure
  argocd/            Argo CD project and applications
  kind/              Local kind cluster configuration and bootstrap script
```

## Roadmap

- Phrase and proximity search (term positions are already indexed)
- Distributed index sharding and replication
- Query fan-out across index shards

## License

This project is intended for learning and portfolio use. Add a license before using it in production.
