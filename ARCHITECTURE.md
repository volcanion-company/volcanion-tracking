# Architecture Diagrams

## 1. System Context Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                     External Systems                            │
│                                                                 │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐         │
│  │ Website  │  │ iOS App  │  │ Android  │  │ Backend  │         │
│  │          │  │          │  │   App    │  │ Service  │         │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘  └────┬─────┘         │
│       │             │              │             │              │
│       └─────────────┴──────────────┴─────────────┘              │
│                           │                                     │
│                     [HTTP API]                                  │
│                           │                                     │
└───────────────────────────┼─────────────────────────────────────┘
                            │
                            ▼
           ┌────────────────────────────────┐
           │  Volcanion Tracking System     │
           │                                │
           │  ┌──────────────────────────┐ │
           │  │  Event Ingestion API     │ │
           │  │  Analytics Query API     │ │
           │  │  Management API          │ │
           │  └──────────────────────────┘ │
           │                                │
           │  ┌──────────┐  ┌───────────┐  │
           │  │PostgreSQL│  │   Redis   │  │
           │  │  Write   │  │  Cache    │  │
           │  │  + Read  │  │           │  │
           │  └──────────┘  └───────────┘  │
           └────────────────────────────────┘
                            │
                            ▼
           ┌────────────────────────────────┐
           │  Observability Platform        │
           │  - Prometheus (Metrics)        │
           │  - Grafana (Dashboards)        │
           │  - Logs (Serilog)              │
           └────────────────────────────────┘
```

## 2. Clean Architecture Layers

```
┌──────────────────────────────────────────────────────────────┐
│                    Presentation Layer                        │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ VolcanionTracking.API                                  │  │
│  │ - Controllers (PartnersController, EventsController)   │  │
│  │ - Middleware (Exception, CorrelationId)                │  │
│  │ - Program.cs (Startup, DI, OpenTelemetry)              │  │
│  └────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────┘
                            │
                            ▼ (depends on)
┌──────────────────────────────────────────────────────────────┐
│                    Application Layer                         │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ VolcanionTracking.Application                          │  │
│  │ - Commands (CreatePartner, IngestEvent)                │  │
│  │ - Queries (GetEvents, GetStatistics)                   │  │
│  │ - Handlers (CQRS with MediatR)                         │  │
│  │ - Interfaces (Repositories, Services)                  │  │
│  │ - Validators (FluentValidation)                        │  │
│  └────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────┘
                            │
                            ▼ (depends on)
┌──────────────────────────────────────────────────────────────┐
│                    Domain Layer (Core)                       │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ VolcanionTracking.Domain                               │  │
│  │ - Aggregates (Partner, TrackingEvent)                  │  │
│  │ - Entities (PartnerSystem, PartnerEventStructure)      │  │
│  │ - Value Objects (ApiKey)                               │  │
│  │ - Domain Events                                        │  │
│  │ - Business Rules & Logic                               │  │
│  └────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────┘
                            ▲
                            │ (implements)
┌──────────────────────────────────────────────────────────────┐
│                    Infrastructure Layer                      │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ VolcanionTracking.Infrastructure                       │  │
│  │ - DbContexts (WriteDbContext, ReadDbContext)           │  │
│  │ - Repositories (Partner, TrackingEvent)                │  │
│  │ - EF Core Configurations                               │  │
│  │ - Services (CacheService, ValidationService)           │  │
│  │ - External Dependencies (PostgreSQL, Redis)            │  │
│  └────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────┘
```

## 3. CQRS Flow Diagram

### Write Side (Command Flow)

```
┌─────────────┐
│   Client    │
└──────┬──────┘
       │ HTTP POST /api/events/ingest
       ▼
┌─────────────────────────┐
│  EventsController       │
│  (API Layer)            │
└──────┬──────────────────┘
       │ IngestEventCommand
       ▼
┌─────────────────────────────────┐
│  IngestEventCommandHandler      │
│  (Application Layer)            │
│                                 │
│  1. Validate API Key (Redis)    │
│  2. Validate Event Schema       │
│  3. Create TrackingEvent        │
│     Aggregate (Domain)          │
│  4. Persist to Write DB         │
└──────┬──────────────────────────┘
       │
       ▼
┌────────────────────────────┐
│  TrackingEventRepository   │
│  (Infrastructure)          │
└──────┬─────────────────────┘
       │
       ▼
┌────────────────────────────┐
│  Write DB (PostgreSQL)     │
│  Schema: write             │
│  Table: TrackingEvents     │
└────────────────────────────┘
       │
       │ (Background Sync)
       ▼
┌────────────────────────────┐
│  Read DB (PostgreSQL)      │
│  Schema: read              │
│  Table: ReadModel          │
└────────────────────────────┘
```

### Read Side (Query Flow)

```
┌─────────────┐
│   Client    │
└──────┬──────┘
       │ HTTP GET /api/events/partner-system/{id}
       ▼
┌─────────────────────────┐
│  EventsController       │
│  (API Layer)            │
└──────┬──────────────────┘
       │ GetEventsByPartnerSystemQuery
       ▼
┌──────────────────────────────────────┐
│  GetEventsByPartnerSystemQueryHandler│
│  (Application Layer)                 │
│                                      │
│  1. Check Redis Cache                │
│  2. Query Read DB (if cache miss)    │
│  3. Apply filters & pagination       │
│  4. Cache results                    │
└──────┬───────────────────────────────┘
       │
       ▼
┌────────────────────────────┐
│  TrackingEventReadRepo     │
│  (Infrastructure)          │
└──────┬─────────────────────┘
       │
       ▼
┌────────────────────────────┐
│  Read DB (PostgreSQL)      │
│  Denormalized ReadModel    │
│  - With Partner names      │
│  - Optimized indexes       │
└────────────────────────────┘
```

## 4. Data Synchronization (Write → Read)

```
┌─────────────────────────────────────────────────────────────┐
│              Write DB → Read DB Sync Flow                   │
└─────────────────────────────────────────────────────────────┘

Option 1: Background Worker (MVP)
────────────────────────────────
┌──────────────┐
│  Write DB    │
│  (new event) │
└──────┬───────┘
       │
       ▼
┌───────────────────────────┐
│  Background Service       │
│  (Polling every 1-5 sec)  │
│                           │
│  1. Read new events       │
│  2. Join with Partner     │
│  3. Join with System      │
│  4. Transform to ReadModel│
└──────┬────────────────────┘
       │
       ▼
┌──────────────┐
│  Read DB     │
│  (ReadModel) │
└──────────────┘

Option 2: Change Data Capture (Production)
───────────────────────────────────────────
┌──────────────┐
│  Write DB    │
│  (WAL/CDC)   │
└──────┬───────┘
       │
       ▼
┌────────────────────────┐
│  CDC Processor         │
│  (Debezium/pg_logical) │
└──────┬─────────────────┘
       │
       ▼
┌──────────────┐
│  Read DB     │
└──────────────┘

Option 3: Event Sourcing (Future)
──────────────────────────────────
┌──────────────┐
│  Write DB    │
│  (events)    │
└──────┬───────┘
       │ Domain Events
       ▼
┌────────────────┐
│  Event Bus     │
│  (RabbitMQ)    │
└──────┬─────────┘
       │
       ▼
┌──────────────────┐
│  Read Projection │
│  Builder         │
└──────┬───────────┘
       │
       ▼
┌──────────────┐
│  Read DB     │
└──────────────┘
```

## 5. Redis Caching Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Cache Strategy                           │
└─────────────────────────────────────────────────────────────┘

Request Flow with Caching:
──────────────────────────

1. Event Ingestion (Cache-Aside Pattern)
   ┌────────┐    1. Check Cache
   │ Client │────────────────────────────┐
   └────────┘                            │
                                         ▼
                            ┌────────────────────────┐
                            │  Redis Cache           │
                            │  Key: apikey:{key}     │
                            │  Value: SystemId       │
                            │  TTL: 1 hour           │
                            └────────┬───────────────┘
                                     │
                         ┌───────────┴────────────┐
                         │                        │
                    2a. Cache HIT           2b. Cache MISS
                         │                        │
                         │                        ▼
                         │              ┌──────────────────┐
                         │              │  Query Database  │
                         │              │  Cache Result    │
                         │              └──────────────────┘
                         │                        │
                         └────────────────────────┘
                                     │
                                     ▼
                         ┌────────────────────┐
                         │  Continue Request  │
                         └────────────────────┘

2. Statistics Query (Write-Through Pattern)
   ┌────────┐
   │ Client │
   └────┬───┘
        │ Query Stats
        ▼
   ┌────────────────┐    Check     ┌────────────┐
   │  Query Handler │──────────────│   Redis    │
   └────────┬───────┘              └────────────┘
            │                            │
            │                      Cache MISS
            │                            │
            ▼                            │
   ┌────────────────┐                    │
   │  Read DB       │◄───────────────────┘
   │  Calculate     │
   └────────┬───────┘
            │
            │ Store in Cache (5 min TTL)
            ▼
   ┌────────────────┐
   │  Redis Cache   │
   └────────────────┘

Cache Invalidation Triggers:
─────────────────────────────
- Partner Updated → Invalidate partner:* keys
- System Updated → Invalidate system:* keys
- Event Structure Updated → Invalidate schema:* keys
```

## 6. Observability Stack

```
┌──────────────────────────────────────────────────────────────┐
│                  Observability Architecture                  │
└──────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────┐
│  Application Layer                                           │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  VolcanionTracking.API                                 │  │
│  │  - OpenTelemetry SDK                                   │  │
│  │  - Activity Source: "VolcanionTracking.*"              │  │
│  │  - Serilog Enrichers                                   │  │
│  └──────────┬─────────────────────────┬───────────────────┘  │
└─────────────┼─────────────────────────┼──────────────────────┘
              │                         │
    ┌─────────▼───────┐       ┌─────────▼─────────┐
    │  Traces         │       │   Metrics         │
    │  (OpenTelemetry)│       │   (Prometheus)    │
    └─────────┬───────┘       └─────────┬─────────┘
              │                         │
              ▼                         ▼
    ┌──────────────────┐    ┌──────────────────────┐
    │  Jaeger/Zipkin   │    │  Prometheus Server   │
    │  (Trace Backend) │    │  (Metrics Storage)   │
    └──────────────────┘    └──────────┬───────────┘
                                       │
                                       ▼
                            ┌──────────────────────┐
                            │  Grafana             │
                            │  (Visualization)     │
                            └──────────────────────┘

    ┌──────────────────┐
    │  Logs            │
    │  (Serilog)       │
    └─────────┬────────┘
              │
              ▼
    ┌──────────────────┐
    │  Log Aggregator  │
    │  (Seq/ELK/Files) │
    └──────────────────┘

Correlation Flow:
─────────────────
HTTP Request → CorrelationId Middleware
              ↓
         Activity.Current.Id
              ↓
    ┌─────────┴─────────┐
    │                   │
    ▼                   ▼
Logs (enriched)    Traces (tagged)
with correlation   with correlation
        ↓               ↓
    All observability signals linked by CorrelationId
```

## 7. Database Schema Relationships

```
┌─────────────────────────────────────────────────────────────┐
│                  Write Schema (write.*)                     │
└─────────────────────────────────────────────────────────────┘

┌──────────────────┐
│   Partners       │
├──────────────────┤
│ Id (PK)          │
│ Name             │
│ Email (Unique)   │
│ IsActive         │
│ CreatedAt        │
└────────┬─────────┘
         │ 1
         │
         │ N
         ▼
┌──────────────────────────┐
│   PartnerSystems         │
├──────────────────────────┤
│ Id (PK)                  │
│ PartnerId (FK)           │
│ Name                     │
│ Type                     │
│ ApiKey (Unique)          │◄────┐
│ IsActive                 │     │
│ CreatedAt                │     │
└────────┬─────────────────┘     │
         │                       │
         │ Referenced by         │
         ▼                       │
┌──────────────────────────┐     │
│   TrackingEvents         │     │
├──────────────────────────┤     │
│ Id (PK)                  │     │
│ PartnerSystemId (FK)     │─────┘
│ EventName                │
│ EventTimestamp           │
│ UserId                   │
│ AnonymousId              │
│ EventPropertiesJson      │ (JSONB)
│ IsValid                  │
│ ValidationErrors         │
│ CorrelationId            │
│ CreatedAt                │
└──────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                  Read Schema (read.*)                       │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│   TrackingEventsReadModel               │
│   (Denormalized for Analytics)          │
├─────────────────────────────────────────┤
│ Id (PK)                                 │
│ TrackingEventId (Unique)                │
│ PartnerSystemId                         │
│ PartnerId                               │
│ PartnerName        ◄───┐ Denormalized   │
│ SystemName         ◄───┘ (no joins!)    │
│ EventName                               │
│ EventTimestamp                          │
│ UserId                                  │
│ AnonymousId                             │
│ EventPropertiesJson (JSONB)             │
│ IsValid                                 │
│ ValidationErrors                        │
│ CorrelationId                           │
│ ProcessedAt                             │
│ CreatedAt                               │
└─────────────────────────────────────────┘

Indexes for Analytics:
- PartnerId + EventTimestamp (time-series queries)
- PartnerSystemId + EventTimestamp
- EventName + EventTimestamp
- UserId (funnel analysis)
- AnonymousId (session tracking)
```

## 8. Deployment Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                   Production Deployment                     │
└─────────────────────────────────────────────────────────────┘

                    ┌──────────────┐
                    │   CDN/WAF    │
                    │  (Cloudflare)│
                    └──────┬───────┘
                           │
                           ▼
                 ┌──────────────────┐
                 │  Load Balancer   │
                 │  (nginx/HAProxy) │
                 └──────┬───────────┘
                        │
         ┌──────────────┼──────────────┐
         │              │              │
         ▼              ▼              ▼
┌────────────┐  ┌────────────┐  ┌────────────┐
│ API Node 1 │  │ API Node 2 │  │ API Node 3 │
│  (Docker)  │  │  (Docker)  │  │  (Docker)  │
└─────┬──────┘  └─────┬──────┘  └─────┬──────┘
      │               │               │
      └───────────────┼───────────────┘
                      │
         ┌────────────┼────────────┐
         │            │            │
         ▼            ▼            ▼
┌──────────────┐  ┌──────────┐  ┌───────────┐
│ PostgreSQL   │  │  Redis   │  │ Metrics   │
│ Write+Read   │  │ Cluster  │  │  Stack    │
│ (Primary)    │  │ (3 nodes)│  │ Prometheus│
└──────┬───────┘  └──────────┘  │ Grafana   │
       │                        └───────────┘
       │ Replication
       ▼
┌──────────────┐
│ PostgreSQL   │
│ Read Replica │
└──────────────┘
```

---

## Key Architectural Principles

1. **Separation of Concerns**: Each layer has distinct responsibilities
2. **Dependency Inversion**: Core domain has no external dependencies
3. **CQRS**: Separate models for reads and writes
4. **Event Sourcing Ready**: Domain events can evolve to full event sourcing
5. **Fail-Safe Design**: Never lose tracking data due to validation
6. **Observable by Default**: Built-in tracing, metrics, and logging
7. **Cache-First**: Redis reduces database load on hot paths
8. **Scalable Design**: Horizontal scaling supported out of the box
