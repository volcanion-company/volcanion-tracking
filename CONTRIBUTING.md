# Contributing to Volcanion Tracking

First off, thank you for considering contributing to Volcanion Tracking! It's people like you that make this event tracking system better for everyone.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Workflow](#development-workflow)
- [Coding Standards](#coding-standards)
- [Commit Guidelines](#commit-guidelines)
- [Pull Request Process](#pull-request-process)
- [Testing Guidelines](#testing-guidelines)
- [Documentation](#documentation)

## Code of Conduct

### Our Pledge

We are committed to providing a welcoming and inspiring community for all. Please be respectful and constructive in all interactions.

### Our Standards

**Positive behavior includes:**
- Using welcoming and inclusive language
- Being respectful of differing viewpoints
- Gracefully accepting constructive criticism
- Focusing on what is best for the community
- Showing empathy towards other community members

**Unacceptable behavior includes:**
- Harassment, trolling, or discriminatory comments
- Publishing others' private information without permission
- Any conduct that could reasonably be considered inappropriate

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker](https://www.docker.com/) (for PostgreSQL and Redis)
- [Git](https://git-scm.com/)
- IDE: [Visual Studio 2022](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/)

### Fork and Clone

1. Fork the repository on GitHub
2. Clone your fork locally:
   ```bash
   git clone https://github.com/YOUR-USERNAME/volcanion-tracking.git
   cd volcanion-tracking
   ```

3. Add the upstream repository:
   ```bash
   git remote add upstream https://github.com/volcanion-company/volcanion-tracking.git
   ```

### Local Setup

1. **Start Infrastructure Services:**
   ```bash
   docker-compose up -d
   ```

2. **Build the Solution:**
   ```bash
   dotnet restore
   dotnet build
   ```

3. **Run Database Migrations:**
   ```bash
   cd src/VolcanionTracking.Infrastructure
   dotnet ef database update --context WriteDbContext --startup-project ../VolcanionTracking.API
   dotnet ef database update --context ReadDbContext --startup-project ../VolcanionTracking.API
   ```

4. **Run the API:**
   ```bash
   cd src/VolcanionTracking.API
   dotnet run
   ```

5. **Access the API:**
   - API: https://localhost:5001
   - Scalar UI: https://localhost:5001/scalar/v1
   - Health Check: https://localhost:5001/health
   - Metrics: https://localhost:5001/metrics

## Development Workflow

### Branch Strategy

We follow a simplified Git Flow:

- `master` - Production-ready code
- `develop` - Integration branch for features
- `feature/*` - Feature branches (e.g., `feature/add-event-filtering`)
- `bugfix/*` - Bug fix branches (e.g., `bugfix/fix-cache-invalidation`)
- `hotfix/*` - Urgent production fixes

### Creating a Feature Branch

```bash
git checkout develop
git pull upstream develop
git checkout -b feature/your-feature-name
```

### Keeping Your Branch Updated

```bash
git checkout develop
git pull upstream develop
git checkout feature/your-feature-name
git rebase develop
```

## Coding Standards

### C# Style Guide

We follow the official [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions) with these additions:

#### Naming Conventions

```csharp
// Classes, methods, properties: PascalCase
public class EventProcessor { }
public void ProcessEvent() { }
public string EventName { get; set; }

// Private fields: _camelCase with underscore prefix
private readonly IEventRepository _eventRepository;

// Method parameters, local variables: camelCase
public void Method(int eventCount)
{
    var totalCount = eventCount + 1;
}

// Constants: PascalCase
public const int MaxRetryCount = 3;

// Interfaces: I prefix + PascalCase
public interface IEventService { }
```

#### Code Organization

```csharp
// 1. Using statements (organized alphabetically)
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

// 2. Namespace
namespace VolcanionTracking.Application.Events;

// 3. Class documentation
/// <summary>
/// Handles event ingestion operations
/// </summary>
public class IngestEventCommandHandler
{
    // 4. Private readonly fields
    private readonly IEventRepository _repository;
    private readonly ILogger<IngestEventCommandHandler> _logger;

    // 5. Constructor
    public IngestEventCommandHandler(
        IEventRepository repository,
        ILogger<IngestEventCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    // 6. Public methods
    public async Task<Result> Handle(...)
    {
        // Implementation
    }

    // 7. Private methods
    private void ValidateEvent(Event @event)
    {
        // Implementation
    }
}
```

### Clean Architecture Principles

#### Domain Layer
- **No dependencies** on external frameworks or libraries
- Pure business logic and domain rules
- Rich domain models (not anemic)
- Domain events for cross-aggregate communication

```csharp
// Good: Rich domain model
public class Partner : AggregateRoot
{
    private readonly List<PartnerSystem> _systems = new();

    public Result<PartnerSystem> AddSystem(string name, string description)
    {
        // Business validation
        if (_systems.Count >= MaxSystemsPerPartner)
            return Result.Failure("Maximum systems limit reached");

        var system = PartnerSystem.Create(Id, name, description);
        _systems.Add(system);
        
        AddDomainEvent(new PartnerSystemAddedEvent(Id, system.Id));
        return Result.Success(system);
    }
}

// Bad: Anemic model
public class Partner
{
    public Guid Id { get; set; }
    public List<PartnerSystem> Systems { get; set; }
}
```

#### Application Layer
- Use cases as Commands and Queries (CQRS)
- MediatR for decoupling
- FluentValidation for input validation
- No knowledge of infrastructure details

```csharp
// Command
public record CreatePartnerCommand(string Name, string Email) : IRequest<Result<Guid>>;

// Handler
public class CreatePartnerCommandHandler : IRequestHandler<CreatePartnerCommand, Result<Guid>>
{
    private readonly IPartnerRepository _repository;

    public async Task<Result<Guid>> Handle(CreatePartnerCommand request, CancellationToken ct)
    {
        var partner = Partner.Create(request.Name, request.Email);
        await _repository.AddAsync(partner, ct);
        return Result.Success(partner.Id);
    }
}

// Validator
public class CreatePartnerCommandValidator : AbstractValidator<CreatePartnerCommand>
{
    public CreatePartnerCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
```

#### Infrastructure Layer
- Implements interfaces defined in Application layer
- EF Core, Redis, external API integrations
- Configuration management

#### API Layer
- Thin controllers (orchestration only)
- Proper HTTP status codes
- OpenAPI documentation
- Middleware for cross-cutting concerns

### Async/Await Guidelines

```csharp
// Always use async/await for I/O operations
public async Task<Event> GetEventAsync(Guid id, CancellationToken ct)
{
    return await _repository.GetByIdAsync(id, ct);
}

// Don't use async for CPU-bound work
public int CalculateTotal(List<int> numbers)
{
    return numbers.Sum(); // No async needed
}

// Pass CancellationToken through the call stack
public async Task ProcessAsync(CancellationToken cancellationToken)
{
    await _service.DoWorkAsync(cancellationToken);
}
```

### Error Handling

```csharp
// Use Result pattern for expected failures
public Result<Partner> CreatePartner(string name)
{
    if (string.IsNullOrEmpty(name))
        return Result.Failure<Partner>("Name is required");
    
    return Result.Success(new Partner(name));
}

// Use exceptions for unexpected errors
public async Task<Event> GetEventAsync(Guid id)
{
    var @event = await _repository.FindAsync(id);
    if (@event == null)
        throw new NotFoundException($"Event {id} not found");
    
    return @event;
}

// Custom exceptions for domain errors
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
```

## Commit Guidelines

We follow [Conventional Commits](https://www.conventionalcommits.org/):

### Commit Message Format

```
<type>(<scope>): <subject>

<body>

<footer>
```

### Types

- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes (formatting, missing semicolons, etc.)
- `refactor`: Code refactoring
- `perf`: Performance improvements
- `test`: Adding or updating tests
- `build`: Build system changes
- `ci`: CI/CD changes
- `chore`: Other changes (dependencies, etc.)

### Examples

```bash
feat(events): add filtering by event name

Implement event name filtering in GetEvents query.
Adds new optional parameter to filter events by exact name match.

Closes #123

---

fix(cache): resolve race condition in cache invalidation

The cache invalidation logic had a race condition when multiple
requests tried to invalidate the same key simultaneously.

Fixed by using Redis distributed lock.

---

docs(api): update API reference for event endpoints

Added examples for all event-related endpoints.
Improved parameter descriptions.
```

### Commit Best Practices

- Use present tense ("add feature" not "added feature")
- Use imperative mood ("move cursor to..." not "moves cursor to...")
- First line should be 50 characters or less
- Reference issues and pull requests after the first line
- Explain _what_ and _why_ vs. _how_

## Pull Request Process

### Before Submitting

1. **Ensure your code builds:**
   ```bash
   dotnet build
   ```

2. **Run tests (when available):**
   ```bash
   dotnet test
   ```

3. **Check code formatting:**
   ```bash
   dotnet format
   ```

4. **Update documentation** if needed

### PR Title Format

Follow the same convention as commit messages:
```
feat(events): add event filtering by date range
```

### PR Description Template

```markdown
## Description
Brief description of what this PR does.

## Type of Change
- [ ] Bug fix (non-breaking change which fixes an issue)
- [ ] New feature (non-breaking change which adds functionality)
- [ ] Breaking change (fix or feature that would cause existing functionality to not work as expected)
- [ ] Documentation update

## How Has This Been Tested?
Describe the tests you ran to verify your changes.

## Checklist
- [ ] My code follows the project's style guidelines
- [ ] I have performed a self-review of my code
- [ ] I have commented my code, particularly in hard-to-understand areas
- [ ] I have made corresponding changes to the documentation
- [ ] My changes generate no new warnings
- [ ] I have added tests that prove my fix is effective or that my feature works
- [ ] New and existing unit tests pass locally with my changes

## Related Issues
Closes #(issue number)
```

### Review Process

1. **Automated Checks:** All CI checks must pass
2. **Code Review:** At least one maintainer approval required
3. **Documentation:** Ensure docs are updated
4. **Breaking Changes:** Require special approval and version bump

### Merge Strategy

- **Squash and Merge:** For feature branches (keeps history clean)
- **Rebase and Merge:** For hotfixes (preserves commits)
- **Never use:** Regular merge commits

## Testing Guidelines

### Test Structure

```csharp
// Arrange - Set up test data and dependencies
var repository = new Mock<IEventRepository>();
var handler = new IngestEventCommandHandler(repository.Object);

// Act - Execute the code under test
var result = await handler.Handle(command, CancellationToken.None);

// Assert - Verify the outcome
result.Should().BeSuccessful();
result.Value.Should().NotBeEmpty();
```

### Test Naming Convention

```csharp
[Fact]
public async Task Handle_ValidEvent_ReturnsSuccess()
{
    // Test method name format: MethodName_Scenario_ExpectedBehavior
}

[Theory]
[InlineData("")]
[InlineData(null)]
public async Task Handle_InvalidEventName_ReturnsFailure(string eventName)
{
    // Theory for testing multiple scenarios
}
```

### Test Categories

- **Unit Tests:** Test individual components in isolation
- **Integration Tests:** Test multiple components together
- **E2E Tests:** Test complete user scenarios

### Code Coverage

- Aim for **80%+ coverage** on business logic
- Domain layer should have **90%+ coverage**
- Infrastructure can have lower coverage (focus on critical paths)

## Documentation

### Code Documentation

```csharp
/// <summary>
/// Processes incoming tracking events and stores them in the database.
/// </summary>
/// <param name="request">The event ingestion request containing event details</param>
/// <param name="cancellationToken">Cancellation token</param>
/// <returns>Result containing the event ID if successful</returns>
/// <exception cref="ValidationException">Thrown when event data is invalid</exception>
public async Task<Result<Guid>> Handle(
    IngestEventCommand request,
    CancellationToken cancellationToken)
{
    // Implementation
}
```

### API Documentation

- Use XML comments for controllers and DTOs
- OpenAPI/Swagger will auto-generate from comments
- Add example responses

```csharp
/// <summary>
/// Ingests a new tracking event
/// </summary>
/// <param name="request">Event details</param>
/// <returns>Event ID</returns>
/// <response code="200">Event ingested successfully</response>
/// <response code="400">Invalid event data</response>
/// <response code="401">Invalid API key</response>
[HttpPost]
[ProducesResponseType(typeof(IngestEventResult), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
public async Task<IActionResult> IngestEvent([FromBody] IngestEventRequest request)
{
    // Implementation
}
```

### README Updates

When adding new features:
1. Update the **Features** section
2. Add configuration examples
3. Update API examples
4. Add troubleshooting tips if needed

## Questions?

- Open an issue for bugs or feature requests
- Use Discussions for questions and ideas
- Contact maintainers: dev@volcanion.com

## Recognition

Contributors will be recognized in:
- GitHub Contributors page
- CHANGELOG.md for significant contributions
- Project documentation

---

Thank you for contributing to Volcanion Tracking! 🚀
