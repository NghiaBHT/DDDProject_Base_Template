# DDD Project Base Template (.NET 8)

This template provides a foundation for building highly decoupled monolithic .NET applications using Domain-Driven Design (DDD), Clean Architecture, and Vertical Slices. It aims to facilitate development with clear boundaries, making it adaptable for potential future transitions to microservices.

## Built With

*   .NET 8
*   ASP.NET Core 8
*   Entity Framework Core 8
*   Npgsql (PostgreSQL Provider for EF Core)
*   MediatR (CQRS Implementation)
*   FluentValidation (Validation)
*   AutoMapper (Object Mapping)
*   Scrutor (Dependency Injection Scanning)
*   NLog (Logging)
*   xUnit (Unit Testing)
*   FluentAssertions (Assertion Library)
*   Moq (Mocking Framework)
*   NetArchTest.Rules (Architecture Testing)
*   dotnet ef database update --project src/DDDProject.API/DDDProject.API.csproj
## Key Features

*   **Bounded Contexts Separation:** Each conceptual bounded context should ideally reside in separate projects (or folders within layers for simplicity) to minimize coupling.
*   **Clean Architecture Layers:** Strict separation between Domain, Application, Infrastructure, and Presentation (API) layers.
*   **Vertical Slices:** Features (Commands/Queries) are organized vertically within the Application layer (e.g., `Application/Users/CreateUser`).
*   **CQRS with MediatR:** Segregation of command and query responsibilities.
*   **Domain-Centric:** Emphasis on rich domain models with entities, value objects, aggregates, and domain events.
*   **Repository Pattern:** Abstraction over data persistence.
*   **Unit of Work:** Manages transactions and ensures consistency.
*   **Pipeline Behaviors:** Cross-cutting concerns like logging and validation handled via MediatR behaviors.
*   **Structured Logging:** Configured with NLog.
*   **Global Exception Handling:** Standardized error responses using middleware.
*   **Testability:** Includes setups for unit testing and architectural rule validation.
*   **Domain Model:** Rich domain models with entities, value objects, aggregates, and domain events (e.g., `User` entity, `Money` value object).
*   **Repositories:** Abstracting data access (e.g., `IUserRepository`).
*   **Unit of Work:** Coordinating transactions across multiple repository operations.
*   **Command/Query Responsibility Segregation (CQRS):** Separating read operations (Queries) from write operations (Commands).
*   **Domain Events:** Decoupling components by publishing events when significant domain actions occur (e.g., `UserCreatedDomainEvent`).
*   **Dependency Injection:** Managing dependencies throughout the application.
*   **Validation:** Using libraries like FluentValidation for command/input validation.
*   **Mapping:** Using libraries like AutoMapper to map between domain entities and DTOs.
*   **Testing:** Includes unit and potentially integration tests (though not fully fleshed out here).

## Project Structure

```
DDDProject/
├── src/
│   ├── DDDProject.API/           # ASP.NET Core Web API (Presentation Layer)
│   │   ├── Controllers/
│   │   ├── Middleware/
│   │   ├── appsettings.json
│   │   └── Program.cs
│   ├── DDDProject.Application/   # Application Logic (Use Cases, Commands, Queries)
│   │   ├── Behaviors/
│   │   ├── Exceptions/
│   │   ├── Users/                # Example Bounded Context / Feature Area
│   │   │   ├── CreateUser/
│   │   │   │   ├── CreateUserCommand.cs
│   │   │   │   ├── CreateUserCommandHandler.cs
│   │   │   │   └── CreateUserCommandValidator.cs
│   │   │   ├── GetUserById/
│   │   │   │   ├── GetUserByIdQuery.cs
│   │   │   │   ├── GetUserByIdQueryHandler.cs
│   │   │   │   └── UserResponse.cs
│   │   │   └── Mapping/              # AutoMapper profiles for this feature
│   │   │       └── UserProfile.cs
│   │   └── DependencyInjection.cs
│   ├── DDDProject.Domain/        # Core Domain Model (Entities, VOs, Events, Interfaces)
│   │   ├── Abstractions/
│   │   ├── Common/               # Shared application logic, interfaces, exceptions
│   │   ├── Entities/
│   │   ├── Events/
│   │   ├── Exceptions/
│   │   ├── Repositories/
│   │   └── ValueObjects/
│   └── DDDProject.Infrastructure/ # Implementation Details (Data Access, External Services)
│       ├── Persistence/
│       │   ├── Configurations/
│       │   ├── Repositories/
│       │   └── ApplicationDbContext.cs
│       ├── DependencyInjection.cs
│       └── nlog.config
├── tests/
│   ├── DDDProject.Application.UnitTests/ # Unit tests for Application layer
│   ├── DDDProject.Architecture.Tests/    # Architecture enforcement tests
│   └── DDDProject.Domain.UnitTests/      # Unit tests for Domain layer
└── DDDProject.sln                  # Solution File
```

## Getting Started

1.  **Configure Connection String:**
    *   Update the `DefaultConnection` string in `src/DDDProject.API/appsettings.json` and/or `src/DDDProject.API/appsettings.Development.json` to point to your PostgreSQL database.

2.  **Apply Migrations:**
    *   Ensure you have the EF Core tools installed (`dotnet tool install --global dotnet-ef`).
    *   **Using the script (Recommended for Linux/macOS/WSL):**
        *   Make the script executable: `chmod +x run_migrations.sh`
        *   Run the script: `./run_migrations.sh`
    *   **Manually (using PowerShell/cmd):**
        *   Navigate to the `src/DDDProject.Infrastructure` directory.
        *   Add a migration: `dotnet ef migrations add InitialCreate -p ../DDDProject.Infrastructure/DDDProject.Infrastructure.csproj -s ../DDDProject.API/DDDProject.API.csproj`
        *   Apply the migration: `dotnet ef database update -p ../DDDProject.Infrastructure/DDDProject.Infrastructure.csproj -s ../DDDProject.API/DDDProject.API.csproj`
        *   *(Note: Adjust relative paths if running from a different directory)*

3.  **Run the Application:**
    *   Set `DDDProject.API` as the startup project.
    *   Run the project (e.g., using `dotnet run --project src/DDDProject.API/DDDProject.API.csproj` or via your IDE).

## Current Status

This template is a foundational starting point. It demonstrates the core architectural patterns but requires further development for production use (see "Further Development").

## Design Principles & Guidelines

*   **Bounded Contexts:** Keep domains isolated.
*   **Domain Model:** Focus on rich entities and value objects.
*   **Application Layer:** Orchestrates use cases, contains no domain logic.
*   **Infrastructure Layer:** Handles external concerns (database, APIs, etc.).
*   **Dependencies:** Flow inwards (API -> Application -> Domain, Infrastructure -> Application & Domain).
*   **Validation:** Use FluentValidation at the application boundary (commands/queries).
*   **Error Handling:** Use custom exceptions and middleware for consistent responses.
*   **Testing:** Write unit tests for domain/application logic and architecture tests for dependency rules.

## Typical Feature Development Workflow (Example: Adding a New Command)

1.  **Domain Layer (`DDDProject.Domain`):**
    *   Define or update relevant Entities or Value Objects if needed.
    *   Define any new Domain Events triggered by the command.
    *   Define or update Repository interfaces if new data access methods are required.
2.  **Application Layer (`DDDProject.Application`):**
    *   Create a new folder for the feature (e.g., `Application/Products/CreateProduct/`).
    *   Define the `CreateProductCommand`.
    *   Implement the `CreateProductCommandHandler`, injecting necessary repository interfaces and domain services.
    *   Implement the `CreateProductCommandValidator` using FluentValidation.
    *   Define any specific DTOs (`ProductResponse`) if the command returns data.
    *   Optionally, define AutoMapper profiles if mapping is needed between Command/Entity/DTO.
    *   Optionally, implement Domain Event Handlers if the command publishes events.
3.  **Infrastructure Layer (`DDDProject.Infrastructure`):**
    *   Implement the repository interfaces defined in the Domain layer (e.g., inside `Persistence/Repositories/`).
    *   Update `ApplicationDbContext` if new entities require mapping or configuration.
    *   Add EF Core migrations if the database schema changes.
4.  **Presentation Layer (`DDDProject.API`):**
    *   Create a new Controller endpoint (or update an existing one) that maps the incoming HTTP request to the `CreateProductCommand`.
    *   Send the command using MediatR (`IMediator.Send(...)`).
    *   Handle the result (success or error) and return an appropriate HTTP response.
5.  **Testing:**
    *   Write Unit Tests for the Domain logic, Command Handler, and Validator.
    *   Write Architecture Tests to ensure dependencies rules are maintained.
    *   *(Optional)* Write Integration Tests to verify the feature end-to-end.

## Further Development

*   Add more Bounded Contexts.
*   Implement Authentication & Authorization.
*   Add Integration Tests.
*   Configure Health Checks.
*   Set up CI/CD pipelines.
*   Enhance logging and monitoring.
*   Add Swagger/OpenAPI documentation.
*   Refine error handling strategies.
*   Implement background job processing if needed. 