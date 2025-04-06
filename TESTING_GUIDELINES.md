# Testing Guidelines

This document outlines the patterns and practices for writing automated tests in this project. We distinguish between Unit Tests (for isolated business logic) and Integration Tests (for components interacting with infrastructure like databases).

## Philosophy

- **Test Behavior, Not Implementation:** Focus tests on verifying the observable behavior and outcomes of a unit or component, rather than tightly coupling to its internal implementation details.
- **Isolation:** Unit tests should test components in isolation, mocking external dependencies. Integration tests verify the interaction between components.
- **Readability:** Tests should be clear, concise, and easy to understand. Use descriptive names and fluent assertion libraries.
- **Maintainability:** Structure tests consistently and leverage helpers to reduce boilerplate code.

## Unit Testing (Application & Domain Layers)

Unit tests focus on testing individual classes (e.g., Command Handlers, Query Handlers, Domain Entities, Value Objects) in isolation.

**Key Tools & Patterns:**

1.  **Test Framework:** [Xunit](https://xunit.net/) (`[Fact]`, `[Theory]`)
2.  **Mocking:** [Moq](https://github.com/moq/moq)
3.  **Assertions:** [FluentAssertions](https://fluentassertions.com/)
4.  **Structure:** Arrange-Act-Assert (AAA) Pattern
5.  **Dependency Mocking:** Use the static `TestHelpers.MockFactory` class located in `tests/DDDProject.Application.UnitTests/TestHelpers/MockFactory.cs`.
    -   It provides methods like `GetMockUnitOfWork()` and `GetMockProductRepository()` to create `Mock<T>` instances with common default setups.
    -   Example: `var repoMock = TestHelpers.MockFactory.GetMockProductRepository();`
6.  **Test Data Simulation:** Use helper methods within `TestHelpers.MockFactory` to generate test data.
    -   Example: `GetSampleProducts(count: 5)` returns a list of `Product` entities.
    -   This is useful for configuring mock repository methods (e.g., `repoMock.Setup(r => r.GetAllAsync(...)).ReturnsAsync(TestHelpers.MockFactory.GetSampleProducts());`)
7.  **Dependency Injection:** Production code should use constructor injection for dependencies, allowing mocks created by the factory to be easily injected during tests.

**Structure (AAA):**

```csharp
using TestHelpers = DDDProject.Application.UnitTests.TestHelpers; // Alias for clarity

// ... inside test class ...

// Fields initialized using the factory
private readonly Mock<IProductRepository> _productRepositoryMock = TestHelpers.MockFactory.GetMockProductRepository();
private readonly Mock<IUnitOfWork> _unitOfWorkMock = TestHelpers.MockFactory.GetMockUnitOfWork();
private readonly ClassUnderTest _instance;

public YourTestsConstructor()
{
    _instance = new ClassUnderTest(_productRepositoryMock.Object, _unitOfWorkMock.Object);
}

[Fact]
public async Task MethodUnderTest_Condition_ExpectedResult()
{
    // Arrange:
    // 1. Mocks are already created by the factory (in fields above).
    // 2. Customize mock setups IF NEEDED for this specific test case (override factory defaults).
    //    _productRepositoryMock.Setup(r => r.SkuExistsAsync(...)).ReturnsAsync(true);
    // 3. Create input data (Commands, Value Objects, Entities).
    //    var command = new CreateProductCommand(...);

    // Act:
    // Execute the method being tested using _instance.
    // var result = await _instance.SomeMethodAsync(command, CancellationToken.None);

    // Assert:
    // Use FluentAssertions to verify outcomes and interactions.
    // result.Should().Be(expectedValue);
    // _productRepositoryMock.Verify(r => r.AddAsync(...), Times.Once);
    // _unitOfWorkMock.Verify(u => u.SaveChangesAsync(...), Times.Once);
}
```

**Example:** See `tests/DDDProject.Application.UnitTests/Products/Commands/CreateProduct/CreateProductCommandHandlerTests.cs`

## Integration Testing (Infrastructure Layer)

Integration tests verify the interaction of components, particularly focusing on infrastructure concerns like database interactions via concrete repository implementations.

**Key Tools & Patterns:**

1.  **Test Framework:** [Xunit](https://xunit.net/)
2.  **Database:** [EF Core InMemory Provider](https://learn.microsoft.com/en-us/ef/core/providers/in-memory/)
3.  **Assertions:** [FluentAssertions](https://fluentassertions.com/)
4.  **Structure:** Arrange-Act-Assert (AAA)
5.  **DbContext Setup:** Use the `DbContextFactory` located in `tests/DDDProject.Infrastructure.Tests/DbContextFactory.cs` to create isolated `ApplicationDbContext` instances configured for the InMemory provider.
6.  **Test Target:** Instantiate *concrete* repository classes (e.g., `ProductRepository`) with the InMemory DbContext.

**Structure (AAA):**

```csharp
[Fact]
public async Task RepositoryMethod_Condition_ExpectedDatabaseResult()
{
    // Arrange:
    // 1. Create an InMemory DbContext instance.
    //    await using var context = DbContextFactory.CreateInMemoryDbContext();
    // 2. Seed data if necessary for the test condition.
    //    context.Products.Add(...);
    //    await context.SaveChangesAsync(); // Save seed data
    // 3. Instantiate the concrete repository with the context.
    //    var repository = new ProductRepository(context);
    // 4. Create input data for the repository method.
    //    var newProduct = Product.Create(...);

    // Act:
    // Execute the repository method being tested.
    // await repository.AddAsync(newProduct);
    // await context.SaveChangesAsync(); // Simulate UoW save

    // Assert:
    // Use FluentAssertions to verify the state of the data in the context.
    // var productInDb = await context.Products.FindAsync(newProduct.Id);
    // productInDb.Should().NotBeNull();
    // productInDb.Name.Should().Be(newProduct.Name);
    // context.Products.Should().HaveCount(expectedCount);
}
```

**Example:** See `tests/DDDProject.Infrastructure.Tests/Persistence/Repositories/ProductRepositoryTests.cs`

## Running Tests

You can run tests using:

- **Visual Studio Test Explorer**
- **dotnet CLI:**
  ```bash
  # Run all tests in the solution
  dotnet test DDDProject.sln

  # Run tests in a specific project
  dotnet test tests/DDDProject.Application.UnitTests/DDDProject.Application.UnitTests.csproj
  dotnet test tests/DDDProject.Infrastructure.Tests/DDDProject.Infrastructure.Tests.csproj

  # Run tests with filtering (e.g., by class)
  dotnet test --filter DisplayName~MySpecificTestsClass
  ``` 