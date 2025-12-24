# Volcanion Tracking - Event Tracking System

A production-ready event tracking system built with .NET 10, following Clean Architecture, DDD, and CQRS patterns. Similar in concept to Google Analytics, Airbridge, or MoEngage, but focused on core tracking features.

## 🏗️ Architecture Overview

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                         API Layer                           │
│  Controllers │ Middleware │ OpenAPI/Scalar │ OpenTelemetry  │
└───────────────────────────┬─────────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────────┐
│                     Application Layer                       │
│    Commands │ Queries │ Handlers │ Validators │ DTOs        │
│                     (CQRS + MediatR)                        │
└───────────────────────────┬─────────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────────┐
│                   Infrastructure Layer                      │
│  EF Core │ PostgreSQL │ Redis │ Repositories │ Services     │
└───────────────────────────┬─────────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────────┐
│                       Domain Layer                          │
│     Entities │ Aggregates │ Value Objects │ Domain Events   │
│                        (Pure DDD)                           │
└─────────────────────────────────────────────────────────────┘
```

### Clean Architecture Layers

#### 1. **Domain Layer** (VolcanionTracking.Domain)
- Contains core business logic and rules
- No dependencies on external frameworks
- Aggregates: Partner, EventStructure, TrackingEvent
- Value Objects: ApiKey
- Domain Events for event-driven communication

#### 2. **Application Layer** (VolcanionTracking.Application)
- Contains use cases (Commands & Queries)
- CQRS implementation with MediatR
- Validation with FluentValidation
- Interfaces for infrastructure concerns
- No knowledge of database or external systems

#### 3. **Infrastructure Layer** (VolcanionTracking.Infrastructure)
- EF Core implementations
- PostgreSQL database contexts (Write & Read)
- Repository implementations
- Redis caching service
- Event validation service

#### 4. **API Layer** (VolcanionTracking.API)
- REST API endpoints
- OpenAPI/Scalar documentation
- Middleware (Exception handling, Correlation ID)
- OpenTelemetry & Prometheus metrics
- Serilog structured logging

---

## 🎯 Domain Model (DDD)

### Aggregates & Entities

```
Partner (Aggregate Root)
├── Id: Guid
├── Name: string
├── Email: string
├── IsActive: bool
└── Systems: List<PartnerSystem>
    └── PartnerSystem (Entity)
        ├── Id: Guid
        ├── PartnerId: Guid
        ├── Name: string
        ├── Type: SystemType (enum)
        ├── ApiKey: ApiKey (Value Object)
        └── IsActive: bool

EventStructure (Aggregate Root)
├── Id: Guid
├── EventName: string
├── Description: string
├── SchemaJson: string (JSON schema)
└── IsActive: bool

PartnerEventStructure (Entity)
├── Id: Guid
├── PartnerId: Guid
├── EventStructureId: Guid?
├── EventName: string
├── SchemaJson: string
└── IsActive: bool

TrackingEvent (Aggregate Root - Write Model)
├── Id: Guid
├── PartnerSystemId: Guid
├── EventName: string
├── EventTimestamp: DateTime
├── UserId: string?
├── AnonymousId: string
├── EventPropertiesJson: string (JSONB)
├── IsValid: bool
├── ValidationErrors: string?
└── CorrelationId: string

TrackingEventReadModel (Read Model)
├── Id: Guid
├── TrackingEventId: Guid
├── PartnerSystemId: Guid
├── PartnerId: Guid
├── PartnerName: string
├── SystemName: string
├── EventName: string
├── EventTimestamp: DateTime
├── UserId: string?
├── AnonymousId: string
├── EventPropertiesJson: string (JSONB)
├── IsValid: bool
├── ValidationErrors: string?
├── CorrelationId: string
└── ProcessedAt: DateTime
```

### Domain Rules

- **Partner** can have multiple **PartnerSystems**
- Each **PartnerSystem** gets a unique **ApiKey** for authentication
- **TrackingEvents** must belong to exactly one **PartnerSystem**
- Events are append-only (never deleted or modified)
- Schema validation is fail-safe (log errors, don't reject events)

---

## 🔄 CQRS Architecture

### Write Side (Commands)

**Optimized for high-throughput event ingestion**

```
HTTP Request → IngestEventCommand
    ↓
IngestEventCommandHandler
    ↓
Validate API Key (cached)
    ↓
Validate Event Schema (lightweight)
    ↓
Create TrackingEvent Aggregate
    ↓
Save to Write DB (PostgreSQL)
    ↓
Return 202 Accepted
```

**Write Database Schema:**
- `write.Partners`
- `write.PartnerSystems`
- `write.TrackingEvents`

### Read Side (Queries)

**Optimized for analytics and reporting**

```
HTTP Request → GetEventsByPartnerSystemQuery
    ↓
GetEventsByPartnerSystemQueryHandler
    ↓
Query Read DB (denormalized)
    ↓
Apply filters & pagination
    ↓
Return results (cached)
```

**Read Database Schema:**
- `read.TrackingEventsReadModel` (denormalized with Partner & System names)

### Synchronization (Write → Read)

**MVP Approach:**
```
1. Write DB saves TrackingEvent
2. Background service (or CDC) reads from Write DB
3. Enriches data with Partner/System info
4. Writes to TrackingEventReadModel in Read DB
```

**Production Options:**
- Change Data Capture (CDC) from PostgreSQL
- Background worker with polling
- Message queue (RabbitMQ/Azure Service Bus)
- PostgreSQL Logical Replication

**Eventual Consistency:**
- Read side may lag behind write side by seconds
- Acceptable for analytics use case
- Real-time queries can optionally check Write DB

---

## 💾 Database Design

### PostgreSQL Schema

#### Write Schema (`write`)

```sql
-- Partners table
CREATE TABLE write.Partners (
    Id UUID PRIMARY KEY,
    Name VARCHAR(200) NOT NULL,
    Email VARCHAR(255) NOT NULL UNIQUE,
    IsActive BOOLEAN NOT NULL,
    DeactivatedAt TIMESTAMP,
    CreatedAt TIMESTAMP NOT NULL,
    UpdatedAt TIMESTAMP
);

-- PartnerSystems table
CREATE TABLE write.PartnerSystems (
    Id UUID PRIMARY KEY,
    PartnerId UUID NOT NULL REFERENCES write.Partners(Id) ON DELETE CASCADE,
    Name VARCHAR(200) NOT NULL,
    Type INTEGER NOT NULL,
    Description VARCHAR(1000) NOT NULL,
    ApiKey VARCHAR(100) NOT NULL UNIQUE,
    IsActive BOOLEAN NOT NULL,
    DeactivatedAt TIMESTAMP,
    CreatedAt TIMESTAMP NOT NULL,
    UpdatedAt TIMESTAMP
);

CREATE INDEX IX_PartnerSystems_PartnerId ON write.PartnerSystems(PartnerId);
CREATE INDEX IX_PartnerSystems_ApiKey ON write.PartnerSystems(ApiKey);
CREATE INDEX IX_PartnerSystems_IsActive ON write.PartnerSystems(IsActive);

-- TrackingEvents table (Write model)
CREATE TABLE write.TrackingEvents (
    Id UUID PRIMARY KEY,
    PartnerSystemId UUID NOT NULL,
    EventName VARCHAR(200) NOT NULL,
    EventTimestamp TIMESTAMP NOT NULL,
    UserId VARCHAR(255),
    AnonymousId VARCHAR(255) NOT NULL,
    EventPropertiesJson JSONB NOT NULL,
    IsValid BOOLEAN NOT NULL,
    ValidationErrors VARCHAR(2000),
    CorrelationId VARCHAR(100) NOT NULL,
    CreatedAt TIMESTAMP NOT NULL
);

CREATE INDEX IX_TrackingEvents_PartnerSystemId ON write.TrackingEvents(PartnerSystemId);
CREATE INDEX IX_TrackingEvents_EventName ON write.TrackingEvents(EventName);
CREATE INDEX IX_TrackingEvents_EventTimestamp ON write.TrackingEvents(EventTimestamp);
CREATE INDEX IX_TrackingEvents_CorrelationId ON write.TrackingEvents(CorrelationId);
CREATE INDEX IX_TrackingEvents_PartnerSystemId_EventTimestamp 
    ON write.TrackingEvents(PartnerSystemId, EventTimestamp);
```

#### Read Schema (`read`)

```sql
-- TrackingEventsReadModel (Denormalized for analytics)
CREATE TABLE read.TrackingEventsReadModel (
    Id UUID PRIMARY KEY,
    TrackingEventId UUID NOT NULL UNIQUE,
    PartnerSystemId UUID NOT NULL,
    PartnerId UUID NOT NULL,
    PartnerName VARCHAR(200) NOT NULL,
    SystemName VARCHAR(200) NOT NULL,
    EventName VARCHAR(200) NOT NULL,
    EventTimestamp TIMESTAMP NOT NULL,
    UserId VARCHAR(255),
    AnonymousId VARCHAR(255) NOT NULL,
    EventPropertiesJson JSONB NOT NULL,
    IsValid BOOLEAN NOT NULL,
    ValidationErrors VARCHAR(2000),
    CorrelationId VARCHAR(100) NOT NULL,
    ProcessedAt TIMESTAMP NOT NULL,
    CreatedAt TIMESTAMP NOT NULL
);

-- Optimized indexes for analytics
CREATE INDEX IX_ReadModel_TrackingEventId ON read.TrackingEventsReadModel(TrackingEventId);
CREATE INDEX IX_ReadModel_PartnerSystemId ON read.TrackingEventsReadModel(PartnerSystemId);
CREATE INDEX IX_ReadModel_PartnerId ON read.TrackingEventsReadModel(PartnerId);
CREATE INDEX IX_ReadModel_EventName ON read.TrackingEventsReadModel(EventName);
CREATE INDEX IX_ReadModel_EventTimestamp ON read.TrackingEventsReadModel(EventTimestamp);
CREATE INDEX IX_ReadModel_UserId ON read.TrackingEventsReadModel(UserId);
CREATE INDEX IX_ReadModel_AnonymousId ON read.TrackingEventsReadModel(AnonymousId);
CREATE INDEX IX_ReadModel_PartnerId_EventTimestamp 
    ON read.TrackingEventsReadModel(PartnerId, EventTimestamp);
CREATE INDEX IX_ReadModel_PartnerSystemId_EventTimestamp 
    ON read.TrackingEventsReadModel(PartnerSystemId, EventTimestamp);
CREATE INDEX IX_ReadModel_EventName_EventTimestamp 
    ON read.TrackingEventsReadModel(EventName, EventTimestamp);
```

### Partitioning Strategy (Optional)

For high-volume scenarios (millions of events per day):

```sql
-- Partition TrackingEvents by month
CREATE TABLE write.TrackingEvents (
    -- columns...
) PARTITION BY RANGE (EventTimestamp);

CREATE TABLE write.TrackingEvents_2025_12 
    PARTITION OF write.TrackingEvents
    FOR VALUES FROM ('2025-12-01') TO ('2026-01-01');

-- Similar for ReadModel
```

---

## 🔌 API Design

### REST Endpoints

#### Partner Management

```http
POST /api/partners
Content-Type: application/json

{
  "name": "Acme Corp",
  "email": "admin@acme.com"
}

Response: 201 Created
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "name": "Acme Corp",
  "email": "admin@acme.com",
  "isActive": true
}
```

#### PartnerSystem Management

```http
POST /api/partnersystems
Content-Type: application/json

{
  "partnerId": "123e4567-e89b-12d3-a456-426614174000",
  "name": "Acme Website",
  "type": 1,
  "description": "Main corporate website"
}

Response: 201 Created
{
  "id": "223e4567-e89b-12d3-a456-426614174001",
  "partnerId": "123e4567-e89b-12d3-a456-426614174000",
  "name": "Acme Website",
  "type": 1,
  "description": "Main corporate website",
  "apiKey": "sk_JpR8xF2hNkT3vQwL9mYcD5sZ1aBeFg4H",
  "isActive": true
}
```

#### Event Ingestion (High-Throughput)

```http
POST /api/events/ingest
Content-Type: application/json

{
  "apiKey": "sk_JpR8xF2hNkT3vQwL9mYcD5sZ1aBeFg4H",
  "eventName": "page_view",
  "eventTimestamp": "2025-12-24T10:30:00Z",
  "userId": "user_12345",
  "anonymousId": "anon_67890",
  "eventProperties": "{\"page\": \"/home\", \"referrer\": \"google.com\"}"
}

Response: 202 Accepted
{
  "eventId": "323e4567-e89b-12d3-a456-426614174002",
  "isValid": true,
  "validationErrors": null,
  "receivedAt": "2025-12-24T10:30:00.123Z"
}
```

#### Analytics Queries

```http
GET /api/events/partner-system/{partnerSystemId}
    ?startDate=2025-12-01T00:00:00Z
    &endDate=2025-12-31T23:59:59Z
    &pageNumber=1
    &pageSize=100

Response: 200 OK
{
  "events": [...],
  "totalCount": 15420,
  "pageNumber": 1,
  "pageSize": 100
}
```

```http
GET /api/events/partner-system/{partnerSystemId}/statistics
    ?startDate=2025-12-01T00:00:00Z
    &endDate=2025-12-31T23:59:59Z

Response: 200 OK
{
  "partnerSystemId": "223e4567-e89b-12d3-a456-426614174001",
  "totalEvents": 15420,
  "validEvents": 15320,
  "invalidEvents": 100,
  "eventCounts": {
    "page_view": 10000,
    "button_click": 5000,
    "form_submit": 420
  },
  "startDate": "2025-12-01T00:00:00Z",
  "endDate": "2025-12-31T23:59:59Z"
}
```

---

## 🗄️ Redis Caching Strategy

### Cache Keys

```
# PartnerSystem lookup by API Key
partner_system:apikey:{apiKey} → Guid (PartnerSystemId)
TTL: 1 hour

# PartnerSystem details
partner_system:{systemId} → PartnerSystem object
TTL: 1 hour

# Event statistics
stats:{partnerSystemId}:{startDate}:{endDate} → EventStatisticsResult
TTL: 5 minutes

# EventStructure schemas
event_structure:{eventName} → EventStructure object
TTL: 1 hour

# PartnerEventStructure schemas
partner_event_structure:{partnerId}:{eventName} → PartnerEventStructure object
TTL: 1 hour
```

### Cache Invalidation

```csharp
// When PartnerSystem is updated/deactivated
await _cacheService.RemoveAsync($"partner_system:{systemId}");
await _cacheService.RemoveAsync($"partner_system:apikey:{apiKey}");

// When Partner is updated
await _cacheService.RemoveByPrefixAsync($"partner_systems:{partnerId}");

// When EventStructure is updated
await _cacheService.RemoveAsync($"event_structure:{eventName}");
```

---

## 📊 Observability

### OpenTelemetry Tracing

**Automatic instrumentation for:**
- HTTP requests (ASP.NET Core)
- Database queries (EF Core)
- External HTTP calls
- Custom spans

**Example trace:**
```
IngestEvent API Call [200ms]
  ├── Get API Key from Cache [5ms]
  ├── Validate Event Schema [10ms]
  ├── Create TrackingEvent Domain [1ms]
  └── Save to Database [184ms]
      └── INSERT INTO TrackingEvents [180ms]
```

### Prometheus Metrics

**Exposed at:** `/metrics`

**Key metrics:**
```
# Request metrics
http_server_request_duration_seconds
http_server_requests_total

# Runtime metrics
process_cpu_usage
process_memory_usage
dotnet_gc_collections_total

# Custom metrics (extensible)
volcanion_events_ingested_total{partner_id, system_id, event_name}
volcanion_events_validation_failures_total
volcanion_event_ingestion_duration_seconds
```

### Structured Logging (Serilog)

**Log format:**
```json
{
  "timestamp": "2025-12-24T10:30:00.123Z",
  "level": "Information",
  "message": "Ingesting event: page_view with correlation ID: abc123",
  "properties": {
    "Application": "VolcanionTracking",
    "Environment": "Production",
    "CorrelationId": "abc123",
    "EventName": "page_view",
    "PartnerSystemId": "223e4567-e89b-12d3-a456-426614174001"
  }
}
```

**Log sinks:**
- Console (development)
- File (production, rolling daily)
- Can add: Elasticsearch, Seq, Application Insights

### Correlation ID Strategy

- Every request gets a `X-Correlation-ID` header
- If not provided, automatically generated
- Propagated through all layers
- Included in logs and traces
- Returned in response headers

---

## 🚀 Getting Started

### Prerequisites

- .NET 10 SDK
- PostgreSQL 16+
- Redis 7+
- Docker (optional)

### Setup

1. **Clone the repository**
```bash
git clone https://github.com/yourorg/volcanion-tracking.git
cd volcanion-tracking
```

2. **Start dependencies (Docker)**
```bash
docker run -d --name postgres -p 5432:5432 \
  -e POSTGRES_PASSWORD=postgres \
  postgres:16

docker run -d --name redis -p 6379:6379 redis:7-alpine
```

3. **Create databases**
```bash
psql -U postgres -h localhost -c "CREATE DATABASE volcanion_tracking_write;"
psql -U postgres -h localhost -c "CREATE DATABASE volcanion_tracking_read;"
```

4. **Apply migrations**
```bash
cd src/VolcanionTracking.API
dotnet ef database update --context WriteDbContext
dotnet ef database update --context ReadDbContext
```

5. **Run the application**
```bash
dotnet run
```

6. **Access API documentation**
- Scalar UI: https://localhost:5001/scalar/v1
- OpenAPI spec: https://localhost:5001/openapi/v1.json
- Prometheus metrics: https://localhost:5001/metrics

### Example Usage

```bash
# Create a partner
curl -X POST https://localhost:5001/api/partners \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test Partner",
    "email": "test@example.com"
  }'

# Create a partner system
curl -X POST https://localhost:5001/api/partnersystems \
  -H "Content-Type: application/json" \
  -d '{
    "partnerId": "{partner-id-from-above}",
    "name": "Test Website",
    "type": 1,
    "description": "Test tracking"
  }'

# Ingest an event
curl -X POST https://localhost:5001/api/events/ingest \
  -H "Content-Type: application/json" \
  -d '{
    "apiKey": "{api-key-from-above}",
    "eventName": "page_view",
    "eventTimestamp": "2025-12-24T10:30:00Z",
    "userId": null,
    "anonymousId": "visitor_123",
    "eventProperties": "{\"page\": \"/home\"}"
  }'
```

---

## 📁 Project Structure

```
volcanion-tracking/
├── VolcanionTracking.slnx
├── src/
│   ├── VolcanionTracking.Domain/
│   │   ├── Aggregates/
│   │   │   ├── PartnerAggregate/
│   │   │   ├── EventStructureAggregate/
│   │   │   └── TrackingEventAggregate/
│   │   ├── Common/
│   │   └── ValueObjects/
│   ├── VolcanionTracking.Application/
│   │   ├── Common/Interfaces/
│   │   ├── Partners/Commands/
│   │   ├── PartnerSystems/Commands/
│   │   └── TrackingEvents/
│   │       ├── Commands/
│   │       └── Queries/
│   ├── VolcanionTracking.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── Configurations/
│   │   │   ├── Repositories/
│   │   │   ├── WriteDbContext.cs
│   │   │   └── ReadDbContext.cs
│   │   └── Services/
│   └── VolcanionTracking.API/
│       ├── Controllers/
│       ├── Middleware/
│       └── Program.cs
└── tests/
    ├── VolcanionTracking.Domain.Tests/
    ├── VolcanionTracking.Application.Tests/
    ├── VolcanionTracking.Infrastructure.Tests/
    └── VolcanionTracking.API.Tests/
```

---

## 🔧 Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "WriteDatabase": "Host=localhost;Port=5432;Database=volcanion_tracking_write;Username=postgres;Password=postgres",
    "ReadDatabase": "Host=localhost;Port=5432;Database=volcanion_tracking_read;Username=postgres;Password=postgres",
    "Redis": "localhost:6379"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

---

## 🎯 Key Design Decisions

### 1. Separate Write & Read Databases
- **Why:** CQRS pattern for optimal performance
- **Trade-off:** Eventual consistency
- **Benefit:** Independent scaling & optimization

### 2. Append-Only Event Storage
- **Why:** Events are immutable facts
- **Trade-off:** No updates/deletes
- **Benefit:** Audit trail, easier reprocessing

### 3. Fail-Safe Validation
- **Why:** Never lose tracking data
- **Trade-off:** Invalid events are stored
- **Benefit:** Data integrity, debugging capability

### 4. JSONB for Event Properties
- **Why:** Flexible schema per partner
- **Trade-off:** Less type safety
- **Benefit:** Easy evolution, no schema migrations

### 5. Redis for Hot Path Caching
- **Why:** Reduce database load on ingestion
- **Trade-off:** Cache invalidation complexity
- **Benefit:** Sub-millisecond lookups

---

## 📈 Performance Considerations

### Expected Performance (single instance)
- **Event ingestion:** ~5,000 events/sec
- **Query latency (read):** <100ms (cached), <500ms (uncached)
- **Database:** PostgreSQL can handle millions of events/day

### Scaling Strategies

**Horizontal Scaling:**
- Multiple API instances behind load balancer
- Shared Redis cache
- Read replicas for Read DB

**Vertical Scaling:**
- Increase PostgreSQL resources
- Add read replicas
- Scale Redis cluster

**Future Optimizations:**
- Batch ingestion endpoint
- Message queue for async processing
- Time-series database (TimescaleDB)
- Partitioning strategy

---

## 🔒 Security Considerations

- API Key authentication for event ingestion
- HTTPS enforced in production
- SQL injection prevention (EF Core parameterized queries)
- JSONB validation to prevent injection
- Rate limiting (to be implemented)
- CORS policy (to be configured)

---

## 🧪 Testing Strategy

- **Unit Tests:** Domain logic, command/query handlers
- **Integration Tests:** Repository implementations, database
- **API Tests:** Controller endpoints, middleware
- **Load Tests:** Event ingestion performance

---

## 📝 License

MIT License

---

## 👥 Contributing

Contributions welcome! Please follow Clean Architecture principles and maintain test coverage.

---

## 🎓 Learning Resources

- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Domain-Driven Design](https://martinfowler.com/bliki/DomainDrivenDesign.html)
- [CQRS Pattern](https://martinfowler.com/bliki/CQRS.html)
- [OpenTelemetry](https://opentelemetry.io/)
- [Prometheus](https://prometheus.io/)

---

**Built with ❤️ for production-ready event tracking**
