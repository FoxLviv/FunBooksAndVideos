# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A .NET 10 solution implementing the "FunBooksAndVideos" purchase-order kata: an e-commerce back end (books,
online videos, club memberships) with a rule-based purchase order processor (BR1: activate memberships
immediately, BR2: generate a shipping slip for physical products), exposed as a REST API with Swagger.
Persistence is in-memory by design. README.md is the authoritative description of behaviour, API conventions,
corner cases and performance notes; keep it in sync when behaviour changes.

## Commands

```bash
dotnet build FunBooksAndVideos.sln                 # warnings are errors (TreatWarningsAsErrors + latest-recommended analyzers)
dotnet test FunBooksAndVideos.sln                  # all ~500 tests (4 test projects)
dotnet test FunBooksAndVideos.sln -c Release       # use when the Debug API is running: avoids file locks on bin/Debug
dotnet test --filter Category=Regression           # end-to-end regression suite only (Api.Tests)
dotnet test --filter Category!=Regression          # unit + component tests only
dotnet test tests/FunBooksAndVideos.Domain.Tests   # one project
dotnet test --filter "FullyQualifiedName~KataExampleRegressionTests"            # one class
dotnet test --filter "FullyQualifiedName~MoneyTests.Of_rejects_negative_amounts" # one test
dotnet test --collect:"XPlat Code Coverage"        # coverlet cobertura reports under TestResults/
dotnet format FunBooksAndVideos.sln --verify-no-changes   # style/whitespace/import ordering (CI-style check)
dotnet run --project src/FunBooksAndVideos.Api     # http://localhost:5080, "/" redirects to Swagger UI
dotnet stryker --project FunBooksAndVideos.Domain.csproj  # mutation testing (global tool: dotnet tool install -g dotnet-stryker); run from inside a tests/*.Tests folder
```

`src/FunBooksAndVideos.Api/FunBooksAndVideos.Api.http` holds ready-made requests for the running API; the OpenAPI
document is at `/swagger/v1/swagger.json`, liveness at `/health`. Demo data is seeded at start-up unless
`DemoData:Seed=false`; HTTPS redirection is active outside the Development environment only. Package versions
live only in `Directory.Packages.props` (central package management). Line endings are LF (`.gitattributes`).
`global.json` pins the .NET 10 SDK. `InvariantGlobalization` is on, so culture-specific formatting is unavailable.
Missing XML docs are not errors (CS1591 suppressed), but Api and Application docs feed Swagger: document new
endpoints, contracts and DTOs with `<summary>`.

## Architecture (dependency rule: Api → Application → Domain; Infrastructure → Application)

- **Domain** (no package dependencies): aggregates `Customer`, `PurchaseOrder`, `ShippingSlip` (all
  `AggregateRoot<TId>` with an optimistic-concurrency `Version`), the `Product → PhysicalProduct/DigitalProduct →
  Book/Video` hierarchy (shipping is decided by the type, never by a flag in rules), value objects (`Money` with
  two-decimal normalisation, `ShippingAddress`, strongly typed ids that reject `default`), and the persistence
  contracts (`I*Repository`, `IUnitOfWork`). Every domain failure is a `DomainValidationException` /
  `BusinessRuleViolationException` with a stable snake_case `Code`; the API exposes those codes verbatim.
  Aggregates have `Create(...)` for new instances and `Rehydrate(...)` for persistence only.
- **Application**: one handler class per use case behind `ICommandHandler<,>` / `IQueryHandler<,>`.
  `SubmitPurchaseOrderHandler` is the transaction script: load customer → `OrderLineFactory` resolves lines and
  catalog prices → `PurchaseOrder.Create` → `IPurchaseOrderProcessor` → stage order (+ idempotency record) →
  `IUnitOfWork.CommitAsync`. Nothing is persisted before the commit, so a failing rule leaves nothing behind.
  The processor runs `IPurchaseOrderRule`s in DI registration order (`ApplicationServiceCollectionExtensions`);
  rules stage writes through repositories and report typed `ProcessingEffect`s via `PurchaseOrderProcessingContext`.
  **Adding a business rule = one class + one `AddScoped<IPurchaseOrderRule, ...>()` line.** The submit handler is
  composed as `IdempotentSubmitPurchaseOrderDecorator(ConcurrencyRetryDecorator(SubmitPurchaseOrderHandler))`;
  keep that order (replay must never reach the handler, retries must re-run the whole handler).
- **Infrastructure**: `InMemoryDataStore` (singleton, one `Lock`) holds immutable snapshot records; repositories
  map record ↔ aggregate so every load is an independent instance (snapshot isolation). Writes are staged as
  `InsertOperation`/`UpdateOperation` on the scoped `InMemoryUnitOfWork`; `CommitAsync` validates the whole batch
  (duplicate keys, stale `Version`) before applying anything and consumes the batch on every attempt (success,
  failure or cancellation). `DuplicateEntityException` / `ConcurrencyConflictException` are the only persistence
  errors; the retry decorator reacts to the latter. Shipping slips are keyed by purchase order id (one slip per
  order). `DemoDataSeeder` loads the kata data (customer 4567890, products 1–4, plans; first order id 3344656).
- **Api**: controllers delegate to handlers; request contracts (`Api/Contracts`) carry DataAnnotations and
  `ToCommand()`; responses are the Application DTOs. Error contract: every error is RFC 9457 problem+json with a
  `code` extension and `traceId`. `ApiExceptionHandler` maps exception types to statuses (400 model binding, 404
  route resources, 409 conflicts/idempotency mismatch, 413 oversized body, 422 semantic/domain failures, 500
  generic + logged at Error). `RequestValidationProblemDetails` (model-state 400s) and `ProblemResponseWriter`
  write JSON directly so that `[Produces]`/content negotiation cannot alter the error media type: do **not** add
  `[Produces]` back to controllers; declare media types per `ProducesResponseType` instead. Model-state keys are
  camelCased by `ModelStateKeys`. JSON: `StrictStringEnumConverterFactory` (one declared enum name, case-
  insensitive, no numbers/comma lists), `OrderLineRequestModelJsonConverter` (rejects unknown members and the
  other branch's property even when null), `[JsonUnmappedMemberHandling(Disallow)]` on all contracts. Swagger is
  built from XML docs of the Api **and** Application projects (both generate documentation files); the
  `OrderLineRequestSchemaFilter` documents order lines as a `oneOf` with discriminator `type`.

## Testing conventions

- xunit 2.9 + FluentAssertions 7.x (kept on 7 for licensing) + NSubstitute; test names are
  `Snake_case_sentences`; builders and fixtures live in `tests/FunBooksAndVideos.TestKit`
  (`CustomerBuilder`, `PurchaseOrderBuilder`, `TestClock`, `TestProducts`, `TestPlans`).
- Api.Tests use `ApiFactory` (`WebApplicationFactory<Program>`, frozen `FakeTimeProvider`, logging cleared). Each
  test class gets its own factory and therefore its own in-memory store; tests create their own customers rather
  than mutating the seeded ones. Tests that need an exact order id (3344656) must construct a fresh factory.
- End-to-end tests carry `[Trait("Category", "Regression")]`. `ConcurrencyRetryComponentTests` builds the DI
  container directly to inject a competing commit; resolve handlers through DI there, never construct them.
- Time comes from the injected `TimeProvider` everywhere; never use `DateTime.UtcNow`.
- Internals are visible to `FunBooksAndVideos.Infrastructure.Tests` and `FunBooksAndVideos.Api.Tests` only.

## Gotchas

- A body over 256 KB (Kestrel limit) and more than 100 order lines are rejected on purpose; order quantities are
  expressed by repeating a line, the shipping slip groups them.
- Idempotency keys live in one global namespace, records never expire, and the fingerprint is computed from the
  parsed command with decimal scale stripped (`48.5` == `48.50`).
- `[LoggerMessage]` source-generated logging is used throughout; add new log methods the same way and keep
  per-rule/per-item events at Debug, one Information event per committed order.
- The in-memory TestHost drops headers with empty values; tests for "present but empty" headers must use
  `TestServer.SendAsync` and add the header through the `IDictionary` interface (see `IdempotencyRegressionTests`).
