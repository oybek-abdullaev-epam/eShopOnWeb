# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```bash
# Restore and build
dotnet restore
dotnet build ./eShopOnWeb.sln --configuration Release

# Run tests (all)
dotnet test ./eShopOnWeb.sln

# Run a single test project
dotnet test tests/UnitTests/UnitTests.csproj

# Run tests matching a class or method name
dotnet test tests/UnitTests/UnitTests.csproj --filter "FullyQualifiedName~OrderItemsReserver"

# Run with coverage
dotnet test ./eShopOnWeb.sln --collect:"XPlat Code Coverage" --logger trx --results-directory coverage

# Run the web app (browse to https://localhost:5001)
cd src/Web && dotnet run --launch-profile https

# Run the API
cd src/PublicApi && dotnet run

# Run via .NET Aspire (orchestrates Web + PublicApi + Seq)
cd src/eShopWeb.AppHost && dotnet run

# Run the Azure Function locally (requires Azure Functions Core Tools)
cd src/OrderItemsReserver && func start
# Listens on http://localhost:7071 — matches appsettings.Development.json

# Docker
docker-compose build && docker-compose up
# Web: localhost:5106, PublicApi: localhost:5200
```

## Database Migrations

Run from `src/Web/`:
```bash
dotnet tool restore
dotnet ef database update -c catalogcontext -p ../Infrastructure/Infrastructure.csproj -s Web.csproj
dotnet ef database update -c appidentitydbcontext -p ../Infrastructure/Infrastructure.csproj -s Web.csproj
```

Set `"UseOnlyInMemoryDatabase": true` in `appsettings.json` to skip SQL Server entirely.

## Architecture

This is a **Clean Architecture** reference application. Dependencies flow inward:

```
Web / PublicApi  →  ApplicationCore  ←  Infrastructure
BlazorAdmin      →  BlazorShared     ←  ApplicationCore
OrderItemsReserver  (standalone Azure Function, no shared project references)
```

**`ApplicationCore`** — innermost ring, no infrastructure dependencies. Contains:
- Domain entities (`Basket`, `Order`, `CatalogItem`, etc.) with aggregate roots and value objects
- Interfaces (`IRepository<T>`, `IBasketService`, `IOrderService`)
- Specifications (Ardalis.Specification pattern)
- Domain events (`OrderCreatedEvent`) published via MediatR

**`Infrastructure`** — implements `ApplicationCore` interfaces using EF Core + SQL Server + ASP.NET Identity. `EfRepository<T>` wraps `RepositoryBase<T>` from `Ardalis.Specification.EntityFrameworkCore`. Two DbContexts: `CatalogContext` and `AppIdentityDbContext`.

**`Web`** — ASP.NET Core MVC + Razor Pages storefront. Uses MediatR for CQRS-lite: query/handler pairs live in `src/Web/Features/`. Hosts the BlazorAdmin WASM app. Cookie-based authentication.

**`PublicApi`** — REST API using **FastEndpoints** (REPR pattern, not controllers). Each endpoint is a class in its own folder. JWT bearer auth. Swagger via `FastEndpoints.Swagger`. AutoMapper is present but marked for eventual removal.

**`BlazorAdmin`** — Blazor WebAssembly admin SPA served through the Web project; communicates with PublicApi.

**`BlazorShared`** — DTOs and FluentValidation validators shared between BlazorAdmin and server-side code.

**`eShopWeb.AppHost`** — .NET Aspire orchestrator for local development. Orchestrates: Web, PublicApi, and Seq (structured logging). **Does not include OrderItemsReserver** — start it separately with `func start`.

**`OrderItemsReserver`** — Azure Functions v4 isolated worker (`net10.0`). Exposes a single HTTP-triggered function `POST api/reserve` that writes order reservation data as a JSON blob to Azure Blob Storage. Configured via `OrderItemsStorage` (connection string) and `BlobContainerName` (default: `order-items`).

## Order Placement Flow (cross-project)

When an order is placed, `OrderService.CreateOrderAsync` publishes `OrderCreatedEvent` via MediatR. Two handlers fire:

1. `OrderCreatedHandler` (ApplicationCore) — sends a confirmation email via `IEmailSender`.
2. `OrderItemsReserverNotificationHandler` (Web/EventHandlers/) — calls `IOrderItemsReserverClient.ReserveAsync`, which POSTs to the Azure Function. Errors are caught and logged; the order is **not** rolled back if the Function is unavailable.

The `OrderItemsReserver:FunctionUrl` config key is required in the Web project — startup throws if it is missing. Development default is `http://localhost:7071/`.

## Key Patterns

- **Repository + Specification**: services accept `IRepository<T>` / `IReadRepository<T>`; queries are expressed as specification classes rather than raw LINQ in services.
- **Guard clauses**: use `Ardalis.GuardClauses` for input validation at service boundaries.
- **Result pattern**: service methods return `Ardalis.Result<T>` instead of throwing.
- **Caching**: `CachedCatalogViewModelService` is a decorator over `CatalogViewModelService` using `IMemoryCache`.
- **Domain events**: `OrderCreatedEvent` extends `DomainEventBase` (from `NimblePros.SharedKernel`) and is published via MediatR.
- **Typed HttpClient**: `IOrderItemsReserverClient` is registered as a typed `HttpClient` in `ConfigureWebServices.cs`; base address comes from `OrderItemsReserver:FunctionUrl`.

## Package Management

All NuGet versions are centrally managed in `Directory.Packages.props` (`ManagePackageVersionsCentrally=true`). Individual `.csproj` files reference packages **without version numbers**. Always add/update versions in `Directory.Packages.props`.

Target framework: `net10.0`. SDK pinned via `global.json` (`rollForward: latestFeature`).

## Test Projects

| Project | Scope |
|---|---|
| `tests/UnitTests` | ApplicationCore domain logic, Web view model services |
| `tests/IntegrationTests` | Infrastructure data layer (EF InMemory) |
| `tests/FunctionalTests` | HTTP-level tests via `WebApplicationFactory` |
| `tests/PublicApiIntegrationTests` | FastEndpoints endpoint tests for PublicApi |

Test framework: **xunit.v3**. Mocking: **NSubstitute**. Filter syntax: `--filter "FullyQualifiedName~<ClassName>"`.
