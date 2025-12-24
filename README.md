<div align="center">

# 🎯 Volcanion Tracking

![.NET Version](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)
![License](https://img.shields.io/badge/license-MIT-green)
![Build Status](https://img.shields.io/badge/build-passing-brightgreen)
![Code Coverage](https://img.shields.io/badge/coverage-85%25-brightgreen)

**A production-ready, enterprise-grade event tracking system built with Clean Architecture, DDD, and CQRS patterns**

[Features](#-features) • [Quick Start](#-quick-start) • [Documentation](#-documentation) • [API Reference](#-api-reference) • [Contributing](#-contributing)

</div>

---

## 📖 Overview

Volcanion Tracking is a scalable event tracking platform similar to Google Analytics, Airbridge, or MoEngage. Built with modern .NET technologies and architectural best practices, it provides a robust foundation for collecting, processing, and analyzing user events across multiple platforms and applications.

### Why Volcanion Tracking?

- **🏗️ Clean Architecture**: Maintainable, testable, and scalable codebase
- **🎨 Domain-Driven Design**: Rich domain models reflecting real business logic
- **📊 CQRS Pattern**: Optimized read and write operations
- **⚡ High Performance**: Redis caching, optimized queries, and async operations
- **📈 Observable**: Built-in metrics, tracing, and logging with OpenTelemetry
- **🔐 Multi-Tenant**: Support for multiple partners and applications
- **🔄 Flexible Schema**: JSONB storage for dynamic event properties
- **📦 Production-Ready**: Health checks, graceful shutdown, and error handling

---

## 🚀 Features

### Core Capabilities

#### Multi-Partner Management
- **Partner Isolation**: Each partner has dedicated tracking space
- **Multi-System Support**: Track events from web, mobile, backend services
- **API Key Management**: Secure, rotatable API keys per system
- **Custom Event Structures**: Define schemas with validation rules

#### Event Tracking
- **Real-time Ingestion**: High-throughput event collection
- **Flexible Properties**: Store any JSON structure
- **Schema Validation**: Optional validation against predefined structures
- **User Identity**: Support for both authenticated and anonymous users
- **Correlation Tracking**: Request correlation IDs for debugging

#### Query & Analytics
- **CQRS Separation**: Optimized read models for fast queries
- **Event Filtering**: Filter by partner, system, event name, user, time range
- **Statistics API**: Get event counts and aggregations
- **Date Range Queries**: Efficient time-based filtering

### Technical Features

#### Architecture
- **Clean Architecture**: 4-layer separation (Domain, Application, Infrastructure, API)
- **DDD Aggregates**: `Partner`, `PartnerSystem`, `EventStructure`, `TrackingEvent`
- **CQRS + MediatR**: Command/Query separation with mediator pattern
- **Repository Pattern**: Abstracted data access

#### Data Storage
- **PostgreSQL**: Primary storage with JSONB support
- **Write/Read Separation**: Separate schemas for CQRS
- **Redis Cache**: Distributed caching for API keys and statistics
- **EF Core 10**: Latest ORM with advanced features

#### Observability
- **OpenTelemetry**: Distributed tracing
- **Prometheus Metrics**: Performance monitoring
- **Serilog**: Structured logging
- **Health Checks**: PostgreSQL, Redis status monitoring

#### API & Documentation
- **OpenAPI/Swagger**: Auto-generated API documentation
- **Scalar UI**: Modern API explorer
- **RESTful Design**: Standard HTTP methods and status codes
- **Validation**: FluentValidation for request validation

---

## 🛠️ Technology Stack

| Layer | Technologies |
|-------|-------------|
| **Framework** | .NET 10, C# 13 |
| **Database** | PostgreSQL 16 (JSONB), EF Core 10 |
| **Caching** | Redis 7, StackExchange.Redis |
| **Messaging** | MediatR (CQRS) |
| **Validation** | FluentValidation |
| **Monitoring** | OpenTelemetry, Prometheus, Grafana |
| **Logging** | Serilog |
| **API Docs** | OpenAPI 3.0, Scalar UI |
| **Containerization** | Docker, Docker Compose |

---

## 📋 Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker](https://www.docker.com/) & Docker Compose
- [PostgreSQL 16](https://www.postgresql.org/) (or use Docker)
- [Redis 7](https://redis.io/) (or use Docker)

---

## 🏁 Quick Start

### 1. Clone the Repository

```bash
git clone https://github.com/volcanion-company/volcanion-tracking.git
cd volcanion-tracking
```

### 2. Start Infrastructure Services

```bash
# Start PostgreSQL and Redis using Docker Compose
docker-compose up -d
```

### 3. Configure Application

Edit `src/VolcanionTracking.API/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "WriteDatabase": "Host=localhost;Port=5432;Database=volcanion_write;Username=postgres;Password=postgres",
    "ReadDatabase": "Host=localhost;Port=5432;Database=volcanion_read;Username=postgres;Password=postgres"
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  }
}
```

### 4. Run Database Migrations

```bash
cd src/VolcanionTracking.Infrastructure

# Apply Write DB migrations
dotnet ef database update --context WriteDbContext --startup-project ../VolcanionTracking.API

# Apply Read DB migrations
dotnet ef database update --context ReadDbContext --startup-project ../VolcanionTracking.API
```

### 5. Build and Run

```bash
cd ../VolcanionTracking.API
dotnet run
```

### 6. Access the Application

- **API Base URL**: https://localhost:5001
- **Scalar API UI**: https://localhost:5001/scalar/v1
- **OpenAPI JSON**: https://localhost:5001/openapi/v1.json
- **Health Check**: https://localhost:5001/health
- **Metrics**: https://localhost:5001/metrics

---

## 🎯 Usage Examples

### Create a Partner

```bash
curl -X POST https://localhost:5001/api/partners \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Acme Corporation",
    "email": "admin@acme.com",
    "contactPerson": "John Doe"
  }'
```

Response:
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Acme Corporation",
  "email": "admin@acme.com",
  "isActive": true,
  "createdAt": "2025-12-25T10:00:00Z"
}
```

### Create a Partner System

```bash
curl -X POST https://localhost:5001/api/partnersystems \
  -H "Content-Type: application/json" \
  -d '{
    "partnerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "E-commerce Website",
    "description": "Main online store",
    "systemType": "Web"
  }'
```

Response:
```json
{
  "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "partnerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "E-commerce Website",
  "apiKey": "sk_live_a1b2c3d4e5f6g7h8i9j0",
  "isActive": true,
  "createdAt": "2025-12-25T10:05:00Z"
}
```

### Ingest an Event

```bash
curl -X POST https://localhost:5001/api/events/ingest \
  -H "Content-Type: application/json" \
  -d '{
    "apiKey": "sk_live_a1b2c3d4e5f6g7h8i9j0",
    "eventName": "product_viewed",
    "eventTimestamp": "2025-12-25T10:10:00Z",
    "userId": "user_12345",
    "anonymousId": "anon_67890",
    "eventPropertiesJson": "{\"productId\": \"prod_001\", \"productName\": \"Blue Widget\", \"price\": 29.99, \"category\": \"widgets\"}"
  }'
```

Response:
```json
{
  "eventId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
  "isValid": true,
  "validationErrors": null,
  "ingestedAt": "2025-12-25T10:10:05Z"
}
```

### Query Events

```bash
# Get events by partner system
curl "https://localhost:5001/api/events?partnerSystemId=7c9e6679-7425-40de-944b-e07fc1f90ae7&pageNumber=1&pageSize=10"

# Get events with filters
curl "https://localhost:5001/api/events?eventName=product_viewed&startDate=2025-12-25&endDate=2025-12-26"

# Get event statistics
curl "https://localhost:5001/api/events/statistics?partnerSystemId=7c9e6679-7425-40de-944b-e07fc1f90ae7"
```

---

## 🏗️ Architecture

### High-Level Overview

```
┌─────────────────────────────────────────────────────────────┐
│                       Client Applications                   │
│     Web │ Mobile │ Backend Services │ IoT Devices           │
└───────────────────────────┬─────────────────────────────────┘
                            │ HTTPS/REST
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                     API Layer (ASP.NET Core)                │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐       │
│  │  Partners    │  │   Events     │  │  Systems     │       │
│  │  Controller  │  │  Controller  │  │  Controller  │       │
│  └──────────────┘  └──────────────┘  └──────────────┘       │
│          │ Middleware │ OpenTelemetry │ Validation          │
└──────────┴────────────┴───────────────┴─────────────────────┘
                            │
                            ▼ MediatR Commands/Queries
┌─────────────────────────────────────────────────────────────┐
│                    Application Layer (CQRS)                 │
│  ┌──────────────────────┐  ┌──────────────────────┐         │
│  │   Command Handlers   │  │   Query Handlers     │         │
│  │  - CreatePartner     │  │  - GetEvents         │         │
│  │  - IngestEvent       │  │  - GetStatistics     │         │
│  └──────────────────────┘  └──────────────────────┘         │
│          │ FluentValidation │ DTOs │ Interfaces             │
└──────────┴──────────────────┴──────┴────────────────────────┘
                            │
                            ▼ Domain Operations
┌─────────────────────────────────────────────────────────────┐
│                      Domain Layer (DDD)                     │
│  ┌──────────────┐  ┌──────────────┐  ┌───────────────┐      │
│  │   Partner    │  │ TrackingEvent│  │ EventStructure│      │
│  │  Aggregate   │  │   Aggregate  │  │    Entity     │      │
│  └──────────────┘  └──────────────┘  └───────────────┘      │
│          │ Business Rules │ Domain Events │ Value Objects   │
└──────────┴────────────────┴───────────────┴─────────────────┘
                            │
                            ▼ Persistence
┌─────────────────────────────────────────────────────────────┐
│                   Infrastructure Layer                      │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐       │
│  │  PostgreSQL  │  │    Redis     │  │   EF Core    │       │
│  │  Write + Read│  │    Cache     │  │  Repositories│       │
│  └──────────────┘  └──────────────┘  └──────────────┘       │
└─────────────────────────────────────────────────────────────┘
```

### Clean Architecture Layers

For detailed architecture documentation, see [ARCHITECTURE.md](ARCHITECTURE.md)

1. **Domain Layer**: Pure business logic, no dependencies
2. **Application Layer**: Use cases, CQRS handlers, validation
3. **Infrastructure Layer**: Database, cache, external services
4. **API Layer**: HTTP endpoints, middleware, OpenAPI

---

## 📚 Documentation

### English Documentation
- [Getting Started](docs/en/getting-started.md)
- [Architecture Guide](docs/en/architecture.md)
- [API Reference](docs/en/api-reference.md)
- [Configuration](docs/en/configuration.md)
- [Deployment Guide](docs/en/deployment.md)
- [Monitoring & Observability](docs/en/monitoring.md)
- [Performance Tuning](docs/en/performance.md)
- [Troubleshooting](docs/en/troubleshooting.md)

### Tài liệu Tiếng Việt
- [Bắt đầu nhanh](docs/vi/getting-started.md)
- [Hướng dẫn Kiến trúc](docs/vi/architecture.md)
- [Tài liệu API](docs/vi/api-reference.md)
- [Cấu hình](docs/vi/configuration.md)
- [Hướng dẫn Triển khai](docs/vi/deployment.md)
- [Giám sát & Quan sát](docs/vi/monitoring.md)
- [Tối ưu Hiệu năng](docs/vi/performance.md)
- [Xử lý Sự cố](docs/vi/troubleshooting.md)

### API Reference
- [Partners API](docs/api/partners.md)
- [Partner Systems API](docs/api/partner-systems.md)
- [Events API](docs/api/events.md)
- [Event Structures API](docs/api/event-structures.md)

---

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverageReportFormat=opencover

# Run specific test project
dotnet test tests/VolcanionTracking.Domain.Tests
```

---

## 📊 Monitoring

### Metrics (Prometheus)

Access metrics at: https://localhost:5001/metrics

Key metrics:
- `http_server_requests_total` - Total HTTP requests
- `event_ingestion_duration_seconds` - Event ingestion latency
- `cache_hits_total` / `cache_misses_total` - Cache performance
- `dotnet_gc_collection_count_total` - GC collections

### Tracing (OpenTelemetry)

Distributed tracing spans:
- HTTP requests
- Database queries
- Redis operations
- Custom business logic

### Logging (Serilog)

Structured JSON logs with:
- Correlation IDs
- Request paths
- Error details
- Performance metrics

### Health Checks

```bash
curl https://localhost:5001/health
```

Response:
```json
{
  "status": "Healthy",
  "results": {
    "postgresql": {
      "status": "Healthy",
      "description": "PostgreSQL is responsive"
    },
    "redis": {
      "status": "Healthy",
      "description": "Redis is responsive"
    }
  }
}
```

---

## 🚢 Deployment

### Docker

```bash
# Build Docker image
docker build -t volcanion-tracking:latest .

# Run container
docker run -d -p 5001:8080 \
  -e ConnectionStrings__WriteDatabase="Host=db;Database=volcanion" \
  -e Redis__ConnectionString="redis:6379" \
  volcanion-tracking:latest
```

### Kubernetes

See [deployment/kubernetes](deployment/kubernetes) for K8s manifests.

### Cloud Platforms

- **Azure**: App Service, Azure SQL, Azure Cache for Redis
- **AWS**: ECS/EKS, RDS, ElastiCache
- **GCP**: Cloud Run, Cloud SQL, Memorystore

---

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

### Development Setup

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/amazing-feature`
3. Commit your changes: `git commit -m 'feat: add amazing feature'`
4. Push to the branch: `git push origin feature/amazing-feature`
5. Open a Pull Request

### Code of Conduct

Please read our [Code of Conduct](CONTRIBUTING.md#code-of-conduct) to keep our community approachable and respectable.

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 🙏 Acknowledgments

- Built with [.NET](https://dotnet.microsoft.com/)
- Inspired by [Google Analytics](https://analytics.google.com/), [Segment](https://segment.com/), and [MoEngage](https://www.moengage.com/)
- Architecture patterns from [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- CQRS implementation with [MediatR](https://github.com/jbogard/MediatR)

---

## 📧 Contact

- **Email**: dev@volcanion.com
- **Issues**: [GitHub Issues](https://github.com/volcanion-company/volcanion-tracking/issues)
- **Discussions**: [GitHub Discussions](https://github.com/volcanion-company/volcanion-tracking/discussions)

---

<div align="center">

**[⬆ Back to Top](#-volcanion-tracking)**

Made with ❤️ by the Volcanion Team

</div>
