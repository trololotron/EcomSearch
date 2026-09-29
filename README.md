# EcomSearch

EcomSearch is a study and portfolio backend project that demonstrates a search-oriented e-commerce architecture built with .NET.
The project focuses on distributed-system concerns rather than CRUD alone: full-text search, caching, event-driven indexing, transactional outbox, 
eventual consistency, retries, idempotent processing, and integration testing.

## Architecture

The system uses MongoDB as the source of truth for product data, Elasticsearch as a search projection, Redis as a search cache, 
and Kafka for asynchronous propagation of product updates.

```text
                         ┌─────────────────────┐
                         │     Search.Api      │
                         │   ASP.NET Core API  │
                         └─────────┬───────────┘
                                   │
                   ┌───────────────┴───────────────┐
                   │                               │
                 READ                            WRITE
                   │                               │
                   ▼                               ▼
             SearchProducts                   UpdateProduct
                   │                               │
                   ▼                               ▼
          Redis cache version                MongoDB transaction
                   │                          ├── Product
                   ▼                          └── OutboxMessage
             Redis cache                            │
              │       │                             ▼
             HIT     MISS                     OutboxPublisher
              │       │                             │
              │       ▼                             ▼
              │  Elasticsearch                    Kafka
              │       │                             │
              │       ▼                             ▼
              │   Redis SET            KafkaProductUpdatedConsumer
              │                                     │
              └──────────────┐                      ▼
                             │            ProductUpdatedHandler
                             │                │           │
                             │                ▼           ▼
                             │         Elasticsearch   Redis version++
                             │
                             ▼
                       HTTP response
```

## Main technologies

- .NET 10
- ASP.NET Core
- Elasticsearch
- Redis
- Apache Kafka
- MongoDB
- Docker Compose
- xUnit

Infrastructure libraries currently include:

- `Elastic.Clients.Elasticsearch`
- `StackExchange.Redis`
- `Confluent.Kafka`
- `MongoDB.Driver`

## Project structure

```text
EcomSearch
├── Search.Api
├── Search.Application
├── Search.Domain
├── Search.Infrastructure
└── Search.Application.Tests
```

### Search.Domain

Contains the core domain model.
The domain layer does not depend on MongoDB, Kafka, Redis, Elasticsearch, or ASP.NET Core.

### Search.Application

Contains application use cases and abstractions such as:

- product search
- product updates
- product update event handling
- search repository abstraction
- indexing abstraction
- cache abstraction
- event publisher abstraction
- transactional product update storage abstraction

### Search.Infrastructure

Contains implementations for external systems:

- Elasticsearch search and indexing
- Redis cache and cache-version storage
- Kafka producer and consumer
- MongoDB persistence
- transactional outbox
- background workers

### Search.Api

Acts as the HTTP entry point and composition root.
Configures dependency injection and exposes product search/update endpoints.

## Product update flow

A product update does not write directly to MongoDB and Kafka independently.
Doing so would create a dual-write problem:

```text
MongoDB write succeeds
↓
process crashes
↓
Kafka publish never happens
```
MongoDB would contain the new product state while Elasticsearch could remain stale.
EcomSearch uses the transactional outbox pattern instead.

### 1. Store product and event atomically

`MongoProductUpdateStore` starts a MongoDB transaction and writes:

```text
Product
+
OutboxMessage(ProductUpdated)
```

Both writes commit together or both are rolled back.

### 2. Publish the outbox event

`OutboxPublisher` runs as a background service.
`OutboxProcessor` retrieves an unprocessed outbox message and publishes the contained `ProductUpdated` event to Kafka.
After a successful Kafka publish:

```text
ProcessedAt = current time
```

If Kafka publishing fails, `ProcessedAt` remains `null`, so the event can be retried.

## Delivery semantics

The outbox provides at-least-once delivery, not exactly-once delivery.
For example:

```text
Kafka publish succeeds
↓
process crashes
↓
ProcessedAt was not saved
↓
message is published again
```

Consumers therefore need to tolerate duplicate events.
The Elasticsearch update uses the product ID as the document identity and performs an upsert, making repeated processing of the same product 
update safe for the current use case.

## Kafka

Product update events are published to:

```text
product-updated
```

The product ID used as the Kafka message key.
This helps keep events for the same product in the same Kafka partition.
The search index consumer uses a consumer group and manual offset commits.
An offset is committed only after the application handler successfully processes the message.

```text
Kafka message
↓
ProductUpdatedHandler
↓
Elasticsearch update
↓
Redis cache invalidation
↓
Kafka offset commit
```

If required processing fails before the commit, the message can be delivered again.

## Search and caching

Search requests use Elasticsearch with Redis in front of it.

```text
Search request
↓
read cache version
↓
build versioned cache key
↓
Redis
├── HIT  → return cached result
└── MISS → Elasticsearch → cache result → return
```

Cache keys contain a global search-cache version:

```text
search:products:v32:<request-hash>
```

When a product update is successfully written to Elasticsearch, the version is incremented.

```text
32 → 33
```

Old cache entries do not need to be deleted immediately. They become unreachable because new searches use `v33` keys and eventually expire 
through TTL.

### Redis failure strategy

Redis is treated as an optimization rather than a source of truth.
If normal cache access fails, search falls back to Elasticsearch.
If the cache version cannot be read, EcomSearch bypasses Redis completely instead of falling back to version `0`, because `v0` is a 
legitimate historical version and could contain stale data.

## Data ownership

The current data responsibilities are:

```text
MongoDB       → source of truth
Elasticsearch → search projection
Redis         → disposable cache
Kafka         → event transport
Outbox        → reliable event delivery
```

Elasticsearch and Redis can be rebuilt from authoritative data.
The complete automatic MongoDB → Elasticsearch rebuild workflow is not implemented yet.

## MongoDB transactions

The outbox transaction requires MongoDB transaction support.
The local Docker setup runs MongoDB as a single-node replica set:

```text
rs0
└── PRIMARY
```

This is intended for local development and allows multi-document transactions.

## Running locally

### Requirements

- .NET 10 SDK
- Docker Desktop
- Git

### Start infrastructure

```bash
docker compose up -d
```

Check containers:

```bash
docker compose ps
```

The project currently runs:

- Elasticsearch
- Redis
- Kafka
- MongoDB

### Initialize MongoDB replica set

On the first run, initialize the local replica set if it has not already been initialized:

```bash
docker exec ecomsearch-mongo mongosh --eval "rs.initiate({_id:'rs0',members:[{_id:0,host:'localhost:27017'}]})"
```

Verify:

```bash
docker exec ecomsearch-mongo mongosh --eval "rs.status()"
```

The node should report itself as `PRIMARY`.

### Run the API

```bash
dotnet run --project Search.Api
```

### Build

```bash
dotnet build
```

### Run tests

```bash
dotnet test
```

Current test suite:

```text
36 tests
36 passed
0 failed
```

The tests include unit, API, MongoDB integration, Kafka integration, and transactional-outbox scenarios.

## Tested failure scenarios

The project includes tests for cases such as:

- Redis cache read failure
- Redis cache write failure
- Redis cache-version failure
- Elasticsearch indexing failure
- Kafka handler failure
- Kafka redelivery after missing commit
- MongoDB transaction rollback
- outbox publication failure
- successful outbox processing
- cache invalidation after product updates

## Transactional outbox guarantees

The current implementation guarantees that a product update and its corresponding outbox event are stored atomically in MongoDB.
It does not guarantee exactly-once processing.
The design intentionally combines:

```text
at-least-once delivery
+
idempotent processing
```

## Current limitations and planned improvements

The current implementation is intentionally still evolving.
Important improvements include:

- atomic outbox message claiming for multiple application instances
- claim/lease expiration for crashed workers
- retry counters and exponential backoff
- poison-message handling
- dead-letter queue support
- outbox retention and cleanup
- protection against out-of-order product updates
- product/event versioning
- automatic Elasticsearch rebuild from MongoDB
- better test isolation for shared Redis/Kafka infrastructure
- OpenTelemetry tracing and metrics
- load and high-concurrency testing

### Multiple outbox workers

The current outbox processor does not yet implement an atomic claim/lease mechanism.
With multiple `Search.Api` instances, two workers could read the same unprocessed outbox message and both publish it.
A future implementation can use an atomic MongoDB state transition such as:

```text
Pending
↓
Processing
WorkerId = ...
LockedUntil = ...
```

This allows one worker to temporarily claim a message while still allowing another worker to recover it if the first worker crashes.

## Design topics demonstrated

EcomSearch is intended to demonstrate and explore:

- Clean Architecture-style dependency direction
- Dependency Injection and service lifetimes
- async I/O and cancellation
- Elasticsearch indexing and search
- Redis caching
- cache invalidation
- Kafka topics, partitions, offsets, and consumer groups
- manual Kafka commits
- eventual consistency
- dual-write problem
- transactional outbox
- at-least-once delivery
- idempotency
- background services
- MongoDB transactions
- integration testing
- failure handling in distributed systems

## Status

Current end-to-end flow has been verified manually:

```text
HTTP POST
↓
MongoDB Product + Outbox
↓
OutboxPublisher
↓
Kafka
↓
Kafka consumer
↓
Elasticsearch
↓
Redis cache version increment
```

The project is actively being extended as a practical exercise in production-oriented .NET backend architecture.
