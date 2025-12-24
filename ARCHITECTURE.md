# 🏗️ Architecture Documentation

## Table of Contents

- [Overview](#overview)
- [Architectural Principles](#architectural-principles)
- [System Context](#system-context)
- [Clean Architecture Layers](#clean-architecture-layers)
- [CQRS Pattern](#cqrs-pattern)
- [Domain-Driven Design](#domain-driven-design)
- [Data Flow](#data-flow)
- [Technology Decisions](#technology-decisions)
- [Quality Attributes](#quality-attributes)

---

## Overview

Volcanion Tracking follows **Clean Architecture** principles combined with **Domain-Driven Design (DDD)** and **CQRS** patterns. This architecture ensures:

- **Separation of Concerns**: Each layer has distinct responsibilities
- **Testability**: Business logic can be tested independently
- **Maintainability**: Changes are isolated and predictable
- **Scalability**: Read and write operations can scale independently
- **Flexibility**: Easy to swap implementations (database, cache, etc.)

### Architectural Style

- **Layered Architecture**: 4 distinct layers with dependency inversion
- **Hexagonal Architecture**: Core domain isolated from external concerns
- **Event-Driven**: Domain events for decoupling
- **CQRS**: Separate models for reads and writes

---

## Architectural Principles

### 1. Dependency Rule

```
API Layer → Application Layer → Domain Layer ← Infrastructure Layer
```

**Key Rule**: Dependencies point inward. Inner layers know nothing about outer layers.

- **Domain** has NO dependencies
- **Application** depends only on Domain
- **Infrastructure** implements Application interfaces
- **API** orchestrates and depends on Application

### 2. Separation of Concerns

Each layer has a single, well-defined responsibility:

| Layer | Responsibility | Example |
|-------|---------------|---------|
| Domain | Business logic, rules, entities | `Partner`, `TrackingEvent` |
| Application | Use cases, workflows | `CreatePartnerCommand`, `GetEventsQuery` |
| Infrastructure | External concerns | Database, cache, external APIs |
| API | HTTP presentation | Controllers, middleware |

### 3. Testability

```csharp
// Domain: Pure functions, easily testable
public class Partner
{
    public Result AddSystem(string name) 
    {
        if (string.IsNullOrEmpty(name))
            return Result.Failure("Name required");
        
        _systems.Add(new PartnerSystem(name));
        return Result.Success();
    }
}

// Test: No mocking needed
[Fact]
public void AddSystem_EmptyName_ReturnsFailure()
{
    var partner = new Partner("Test");
    var result = partner.AddSystem("");
    
    result.IsFailure.Should().BeTrue();
}
```

---

## System Context

### High-Level System Diagram

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
│                     [HTTPS REST API]                            │
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
           │  - OpenTelemetry (Traces)      │
           │  - Serilog (Logs)              │
           └────────────────────────────────┘
```

### External Dependencies

| System | Purpose | Protocol |
|--------|---------|----------|
| PostgreSQL | Primary data store | TCP/5432 |
| Redis | Caching layer | TCP/6379 |
| Prometheus | Metrics collection | HTTP/9090 |
| Grafana | Metrics visualization | HTTP/3000 |

---

## Clean Architecture Layers

### Layer Diagram

```
┌──────────────────────────────────────────────────────────────┐
│                    Presentation Layer                        │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ VolcanionTracking.API                                  │  │
│  │ - Controllers (REST endpoints)                         │  │
│  │ - Middleware (Exception, CorrelationId)                │  │
│  │ - Program.cs (Startup, DI, Configuration)              │  │
│  │ - OpenAPI/Scalar documentation                         │  │
│  └────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────┘
                            │ depends on
                            ▼
┌──────────────────────────────────────────────────────────────┐
│                    Application Layer                         │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ VolcanionTracking.Application                          │  │
│  │ ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │  │
│  │ │  Commands    │  │   Queries    │  │  Validators  │  │  │
│  │ │  - Create    │  │  - GetEvents │  │  - Fluent    │  │  │
│  │ │  - Ingest    │  │  - GetStats  │  │  Validation  │  │  │
│  │ └──────────────┘  └──────────────┘  └──────────────┘  │  │
│  │ ┌──────────────┐  ┌──────────────┐                    │  │
│  │ │  Handlers    │  │  Interfaces  │                    │  │
│  │ │  - MediatR   │  │  - Repos     │                    │  │
│  │ └──────────────┘  └──────────────┘                    │  │
│  └────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────┘
                            │ depends on
                            ▼
┌──────────────────────────────────────────────────────────────┐
│                    Domain Layer (Core)                       │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ VolcanionTracking.Domain                               │  │
│  │ ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │  │
│  │ │  Aggregates  │  │   Entities   │  │Value Objects │  │  │
│  │ │  - Partner   │  │  - System    │  │  - ApiKey    │  │  │
│  │ │  - Event     │  │  - Structure │  │  - Email     │  │  │
│  │ └──────────────┘  └──────────────┘  └──────────────┘  │  │
│  │ ┌──────────────┐  ┌──────────────┐                    │  │
│  │ │Domain Events │  │Business Rules│                    │  │
│  │ └──────────────┘  └──────────────┘                    │  │
│  └────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────┘
                            ▲ implements interfaces
┌──────────────────────────────────────────────────────────────┐
│                    Infrastructure Layer                      │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ VolcanionTracking.Infrastructure                       │  │
│  │ ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │  │
│  │ │  DbContexts  │  │ Repositories │  │   Services   │  │  │
│  │ │  - Write     │  │  - Partner   │  │  - Cache     │  │  │
│  │ │  - Read      │  │  - Event     │  │  - Validate  │  │  │
│  │ └──────────────┘  └──────────────┘  └──────────────┘  │  │
│  │ ┌──────────────┐  ┌──────────────┐                    │  │
│  │ │ EF Configs   │  │  Background  │                    │  │
│  │ │              │  │  Services    │                    │  │
│  │ └──────────────┘  └──────────────┘                    │  │
│  └────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────┘
```

### 1. Domain Layer

**Purpose**: Contains business logic and rules. No external dependencies.

**Components**:

```csharp
// Aggregate Root
public class Partner : AggregateRoot
{
    private readonly List<PartnerSystem> _systems = new();
    public IReadOnlyCollection<PartnerSystem> Systems => _systems.AsReadOnly();
    
    public Result<PartnerSystem> AddSystem(string name, string description)
    {
        // Business validation
        if (_systems.Count >= MaxSystemsPerPartner)
            return Result.Failure("Maximum systems limit reached");
        
        // Create entity
        var system = PartnerSystem.Create(Id, name, description);
        _systems.Add(system);
        
        // Raise domain event
        AddDomainEvent(new PartnerSystemAddedEvent(Id, system.Id));
        
        return Result.Success(system);
    }
}

// Entity
public class PartnerSystem : Entity
{
    public Guid PartnerId { get; private set; }
    public string Name { get; private set; }
    public ApiKey ApiKey { get; private set; }
    public bool IsActive { get; private set; }
    
    // Factory method
    public static PartnerSystem Create(Guid partnerId, string name, string description)
    {
        return new PartnerSystem
        {
            Id = Guid.NewGuid(),
            PartnerId = partnerId,
            Name = name,
            ApiKey = ApiKey.Generate(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }
}

// Value Object
public class ApiKey : ValueObject
{
    public string Value { get; private set; }
    
    public static ApiKey Generate()
    {
        var key = $"sk_live_{Guid.NewGuid():N}";
        return new ApiKey { Value = key };
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}

// Domain Event
public record PartnerSystemAddedEvent(Guid PartnerId, Guid SystemId) : IDomainEvent;
```

**Key Patterns**:
- **Aggregate Roots**: Enforce consistency boundaries
- **Entities**: Identity-based objects
- **Value Objects**: Immutable, equality by value
- **Domain Events**: Communicate changes
- **Factory Methods**: Ensure valid object creation

### 2. Application Layer

**Purpose**: Use cases and business workflows. Orchestrates domain objects.

**Components**:

```csharp
// Command
public record CreatePartnerCommand(
    string Name,
    string Email,
    string? ContactPerson
) : IRequest<Result<Guid>>;

// Command Handler
public class CreatePartnerCommandHandler : IRequestHandler<CreatePartnerCommand, Result<Guid>>
{
    private readonly IPartnerRepository _repository;
    private readonly ILogger<CreatePartnerCommandHandler> _logger;
    
    public async Task<Result<Guid>> Handle(
        CreatePartnerCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating partner: {Name}", request.Name);
        
        // Use domain factory
        var partner = Partner.Create(request.Name, request.Email, request.ContactPerson);
        
        // Persist
        await _repository.AddAsync(partner, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Partner created: {PartnerId}", partner.Id);
        
        return Result.Success(partner.Id);
    }
}

// Validator
public class CreatePartnerCommandValidator : AbstractValidator<CreatePartnerCommand>
{
    public CreatePartnerCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name too long");
        
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");
    }
}

// Query
public record GetEventsQuery(
    Guid PartnerSystemId,
    string? EventName,
    DateTime? StartDate,
    DateTime? EndDate,
    int PageNumber,
    int PageSize
) : IRequest<Result<PagedResult<EventDto>>>;

// Query Handler
public class GetEventsQueryHandler : IRequestHandler<GetEventsQuery, Result<PagedResult<EventDto>>>
{
    private readonly ITrackingEventReadRepository _repository;
    private readonly ICacheService _cache;
    
    public async Task<Result<PagedResult<EventDto>>> Handle(
        GetEventsQuery request,
        CancellationToken cancellationToken)
    {
        // Query read model
        var events = await _repository.GetByPartnerSystemAsync(
            request.PartnerSystemId,
            request.EventName,
            request.StartDate,
            request.EndDate,
            request.PageNumber,
            request.PageSize,
            cancellationToken);
        
        // Map to DTOs
        var dtos = events.Select(e => new EventDto
        {
            Id = e.Id,
            EventName = e.EventName,
            Timestamp = e.EventTimestamp,
            Properties = e.EventPropertiesJson
        });
        
        return Result.Success(new PagedResult<EventDto>(dtos, events.TotalCount));
    }
}
```

**Key Patterns**:
- **CQRS**: Separate commands and queries
- **MediatR**: Decoupled request handling
- **FluentValidation**: Input validation
- **DTOs**: Data transfer objects for API
- **Result Pattern**: Explicit success/failure

### 3. Infrastructure Layer

**Purpose**: Implements interfaces defined in Application. Handles external concerns.

**Components**:

```csharp
// DbContext (Write)
public class WriteDbContext : DbContext
{
    public DbSet<Partner> Partners => Set<Partner>();
    public DbSet<TrackingEvent> TrackingEvents => Set<TrackingEvent>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply all configurations
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}

// DbContext (Read)
public class ReadDbContext : DbContext
{
    public DbSet<TrackingEventReadModel> Events => Set<TrackingEventReadModel>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("read");
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}

// EF Configuration
public class PartnerConfiguration : IEntityTypeConfiguration<Partner>
{
    public void Configure(EntityTypeBuilder<Partner> builder)
    {
        builder.ToTable("Partners", "write");
        
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.OwnsMany(p => p.Systems, s =>
        {
            s.WithOwner().HasForeignKey("PartnerId");
            s.Property<string>("ApiKey_Value").HasColumnName("ApiKey");
        });
        
        // Ignore domain events (not persisted)
        builder.Ignore(p => p.DomainEvents);
    }
}

// Repository Implementation
public class PartnerRepository : IPartnerRepository
{
    private readonly WriteDbContext _context;
    
    public async Task<Partner?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _context.Partners
            .Include(p => p.Systems)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }
    
    public async Task AddAsync(Partner partner, CancellationToken ct)
    {
        await _context.Partners.AddAsync(partner, ct);
    }
    
    public async Task SaveChangesAsync(CancellationToken ct)
    {
        await _context.SaveChangesAsync(ct);
    }
}

// Cache Service
public class CacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    
    public async Task<T?> GetAsync<T>(string key, CancellationToken ct) where T : class
    {
        var db = _redis.GetDatabase();
        var value = await db.StringGetAsync(key);
        
        return value.HasValue
            ? JsonSerializer.Deserialize<T>(value!)
            : null;
    }
    
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry, CancellationToken ct) where T : class
    {
        var db = _redis.GetDatabase();
        var json = JsonSerializer.Serialize(value);
        await db.StringSetAsync(key, json, expiry);
    }
}
```

**Key Patterns**:
- **Repository Pattern**: Abstracted data access
- **Unit of Work**: DbContext as transaction boundary
- **EF Configurations**: Fluent API for mapping
- **Caching**: Redis for performance
- **Background Services**: Async data synchronization

### 4. API Layer

**Purpose**: HTTP presentation and orchestration.

**Components**:

```csharp
// Controller
[ApiController]
[Route("api/[controller]")]
public class PartnersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PartnersController> _logger;
    
    [HttpPost]
    [ProducesResponseType(typeof(PartnerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePartner(
        [FromBody] CreatePartnerRequest request,
        CancellationToken ct)
    {
        var command = new CreatePartnerCommand(
            request.Name,
            request.Email,
            request.ContactPerson);
        
        var result = await _mediator.Send(command, ct);
        
        if (result.IsFailure)
            return BadRequest(new ErrorResponse(result.Error));
        
        return CreatedAtAction(
            nameof(GetPartner),
            new { id = result.Value },
            new PartnerResponse { Id = result.Value });
    }
}

// Middleware
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error");
            await WriteErrorResponse(context, StatusCodes.Status400BadRequest, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteErrorResponse(context, StatusCodes.Status500InternalServerError, "Internal server error");
        }
    }
}

// Startup
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Configure observability
builder.Services.AddOpenTelemetry()
    .WithTracing(b => b.AddAspNetCoreInstrumentation())
    .WithMetrics(b => b.AddPrometheusExporter());

var app = builder.Build();

// Configure middleware pipeline
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseRouting();
app.MapControllers();
app.MapPrometheusScrapingEndpoint();

app.Run();
```

---

## CQRS Pattern

### Command Flow

```
┌──────────┐       ┌───────────────┐       ┌─────────────┐
│          │       │               │       │             │
│  Client  ├──────►│   Command     ├──────►│   Write     │
│          │       │   Handler     │       │   Database  │
└──────────┘       └───────────────┘       └─────────────┘
     POST               Validate                PostgreSQL
   /partners            Transform              (Write Schema)
                        Persist
                        ↓
                    Domain Events
                        ↓
                ┌───────────────┐
                │  Background   │
                │   Service     │
                └───────┬───────┘
                        ↓
                ┌─────────────┐
                │    Read     │
                │  Database   │
                └─────────────┘
                  PostgreSQL
                 (Read Schema)
```

### Query Flow

```
┌──────────┐       ┌───────────────┐       ┌─────────────┐
│          │       │               │       │             │
│  Client  ├──────►│   Query       ├──────►│    Read     │
│          │       │   Handler     │       │   Database  │
└──────────┘       └───────────────┘       └─────────────┘
     GET                Filter                PostgreSQL
   /events              Map DTOs             (Read Schema)
                        ↑
                   ┌────┴─────┐
                   │  Redis   │
                   │  Cache   │
                   └──────────┘
```

### Benefits

- **Performance**: Optimized read models
- **Scalability**: Scale reads and writes independently
- **Simplicity**: Separate concerns
- **Flexibility**: Different storage strategies

---

## Domain-Driven Design

### Aggregates

**Partner Aggregate**:
```
Partner (Root)
├── Name
├── Email
├── IsActive
└── Systems[]
    ├── PartnerSystem
    │   ├── Name
    │   ├── ApiKey
    │   └── IsActive
    └── EventStructures[]
        └── EventStructure
            ├── EventName
            ├── Schema
            └── IsRequired
```

**TrackingEvent Aggregate**:
```
TrackingEvent (Root)
├── PartnerSystemId
├── EventName
├── EventTimestamp
├── UserId
├── AnonymousId
├── EventPropertiesJson
├── IsValid
└── ValidationErrors
```

### Bounded Contexts

1. **Partner Management Context**
   - Partners
   - Partner Systems
   - API Keys

2. **Event Tracking Context**
   - Event Ingestion
   - Event Structures
   - Validation Rules

3. **Analytics Context**
   - Event Queries
   - Statistics
   - Aggregations

---

## Data Flow

### Event Ingestion Flow

```
1. Client sends event → API Controller
2. Controller creates IngestEventCommand
3. MediatR dispatches to Handler
4. Handler validates API key (Redis cache)
5. Handler validates event structure
6. Handler creates TrackingEvent aggregate
7. Repository persists to Write DB
8. Background service syncs to Read DB
9. Response returned to client
```

### Query Flow

```
1. Client requests events → API Controller
2. Controller creates GetEventsQuery
3. MediatR dispatches to Handler
4. Handler checks cache (Redis)
5. Handler queries Read DB (if not cached)
6. Handler maps to DTOs
7. Response returned with pagination
```

---

## Technology Decisions

### Why PostgreSQL?

- **JSONB Support**: Store flexible event properties
- **ACID Compliance**: Strong consistency guarantees
- **Performance**: Excellent for analytical queries
- **Maturity**: Battle-tested, reliable

### Why Redis?

- **Speed**: Sub-millisecond response times
- **Caching**: Reduce database load
- **Simple**: Easy to set up and maintain
- **Popular**: Well-supported libraries

### Why MediatR?

- **Decoupling**: Handlers don't know about each other
- **Testability**: Easy to test in isolation
- **Pipeline**: Built-in behaviors (validation, logging)
- **CQRS**: Natural fit for command/query separation

### Why EF Core?

- **Productivity**: Write less boilerplate code
- **Type Safety**: Compile-time checking
- **Migrations**: Database version control
- **Performance**: Optimized queries, change tracking

---

## Quality Attributes

### Performance

- **Caching**: Redis for API keys, statistics
- **Async**: All I/O operations are async
- **CQRS**: Optimized read models
- **Indexing**: Strategic database indexes

### Scalability

- **Horizontal**: Stateless API, can add instances
- **Vertical**: Async operations, efficient resource use
- **Database**: Read replicas, connection pooling
- **Cache**: Distributed Redis cluster

### Reliability

- **Health Checks**: Monitor dependencies
- **Graceful Shutdown**: Finish in-flight requests
- **Error Handling**: Proper exception handling
- **Logging**: Comprehensive error logging

### Maintainability

- **Clean Code**: SOLID principles
- **Clear Structure**: Obvious layer separation
- **Documentation**: Inline comments, XML docs
- **Patterns**: Consistent use of patterns

### Observability

- **Metrics**: Prometheus metrics
- **Tracing**: OpenTelemetry distributed tracing
- **Logging**: Structured logs with Serilog
- **Health**: Real-time health endpoints

---

## Conclusion

This architecture provides a solid foundation for a scalable, maintainable event tracking system. The combination of Clean Architecture, DDD, and CQRS ensures the system can grow and evolve while maintaining code quality and testability.

For more details, see the [documentation](docs/) folder.
