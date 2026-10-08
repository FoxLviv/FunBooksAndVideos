# FunBooksAndVideos

Back end for the *FunBooksAndVideos* code kata: an e-commerce shop selling books, online videos and
club memberships, exposed as a REST API with Swagger, built on .NET 10.

Submitting a purchase order runs the **Purchase Order Processor**, which applies the business rules:

| Rule | Behaviour |
|------|-----------|
| **BR1** | If the order contains a membership, it is activated in the customer account immediately. |
| **BR2** | If the order contains a physical product, a shipping slip is generated. |

## Quick start

```bash
dotnet run --project src/FunBooksAndVideos.Api
```

Open <http://localhost:5080> (redirects to Swagger UI). Demo data is pre-loaded: customer `4567890`
(Jane Doe, with a shipping address), customer `4567891` (no address), products `1`–`4` and the three
membership plans. The first submitted order receives id `3344656`, so the example from the kata can be
reproduced one-to-one:

```http
POST /api/v1/purchase-orders
{
  "customerId": 4567890,
  "lines": [
    { "type": "Product", "productId": 1 },
    { "type": "Product", "productId": 2 },
    { "type": "Membership", "membershipType": "BookClub" }
  ],
  "expectedTotal": 48.50
}
```

The response (`201 Created`, `Location: /api/v1/purchase-orders/3344656`) contains the processed order
with total `48.50`, the membership activation (BR1), the shipping slip for the book (BR2) and the list of
rules that applied. More ready-made requests are in `src/FunBooksAndVideos.Api/FunBooksAndVideos.Api.http`.

```bash
dotnet test                                   # everything (~500 tests)
dotnet test --filter Category=Regression      # end-to-end regression suite only
dotnet test --filter Category!=Regression     # unit and component tests only
```

## Solution layout

```
src/
  FunBooksAndVideos.Domain          entities, value objects, invariants, persistence contracts (no dependencies)
  FunBooksAndVideos.Application     use cases, the purchase order processor and the business rules
  FunBooksAndVideos.Infrastructure  in-memory persistence (unit of work, repositories, optimistic concurrency), demo data
  FunBooksAndVideos.Api             controllers, request contracts, problem-details error handling, Swagger
tests/
  FunBooksAndVideos.TestKit         shared builders and fixtures
  FunBooksAndVideos.Domain.Tests    unit tests of the model
  FunBooksAndVideos.Application.Tests
  FunBooksAndVideos.Infrastructure.Tests
  FunBooksAndVideos.Api.Tests       integration tests over the real HTTP pipeline, incl. the regression suite
```

Dependencies point inwards only (Api → Application → Domain; Infrastructure → Application). The
composition root is `Program.cs`.

## Domain model

* **Product** (abstract) → `PhysicalProduct` → `Book`; `DigitalProduct` → `Video`.
  Whether something ships is decided by the hierarchy (`RequiresShipping`), never by `switch`es in rules.
* **MembershipPlan** – a sellable membership (`BookClub`, `VideoClub`, `Premium`) with a price.
* **Customer** (aggregate root) – name, optional shipping address and the activated memberships.
  Club access is a flag set (`Club.Book | Club.Video`); activation is idempotent: a membership that adds no
  new club reports `AlreadyActive` and leaves the account untouched, premium on top of book club is an upgrade.
* **PurchaseOrder** (aggregate root) – customer, item lines, total (always derived from the lines), status.
  Lines are immutable snapshots of catalog data (`ProductLine`, `MembershipLine`), so later price changes
  never alter an existing order. Lines implement a **Visitor**.
* **ShippingSlip** (aggregate root) – order, customer, address and the items to pack (repeated products are
  grouped into quantities). Built by the domain service `ShippingSlipFactory`.
* **Money** – non-negative, at most two decimals, always normalised to two decimals.
* Strongly typed identifiers (`CustomerId`, `ProductId`, `PurchaseOrderId`, `ShippingSlipId`).

## The purchase order processor

```
SubmitPurchaseOrderHandler
   ├─ load customer, resolve lines from the catalog (OrderLineFactory)
   ├─ PurchaseOrder.Create(...)                     ← domain invariants
   ├─ IPurchaseOrderProcessor.ProcessAsync(order, customer)
   │     for each IPurchaseOrderRule in registration order:
   │        if rule.AppliesTo(order) → rule.ApplyAsync(context)
   │     order.MarkProcessed()
   ├─ repository.Add(order)
   └─ IUnitOfWork.CommitAsync()                     ← everything or nothing
```

* `IPurchaseOrderRule` is the extension point: a rule declares when it applies and what it does. Adding a
  rule means adding a class and one registration line; the processor never changes (Open/Closed).
* Rules only *stage* changes through repositories and report what they did as typed `ProcessingEffect`s.
  Nothing is persisted until the handler commits, so a failing rule (e.g. BR2 for a customer without an
  address) leaves no partial state behind, even if BR1 already ran.
* All rules of one run share one timestamp (`ProcessedAt`).

## Design principles and patterns

| Pattern | Where |
|---------|-------|
| Strategy / Chain of rules | `IPurchaseOrderRule`, `PurchaseOrderProcessor` |
| Command / Query handlers | `ICommandHandler<,>`, `IQueryHandler<,>` – one class per use case |
| Decorator | `ConcurrencyRetryDecorator` re-runs a command after an optimistic concurrency conflict; `IdempotentSubmitPurchaseOrderDecorator` replays results for a repeated `Idempotency-Key` |
| Simple Factory | `ProductFactory` (kind → concrete product), `ShippingSlipFactory`, `OrderLineFactory` – static/injected factories, not the GoF Factory Method |
| Visitor | `IOrderLineVisitor<T>` – maps line types without runtime type switches |
| Builder | `CustomerBuilder`, `PurchaseOrderBuilder` (test data) |
| Repository + Unit of Work | `Domain.Persistence` contracts, in-memory implementations |
| Aggregate / Value Object | `Customer`, `PurchaseOrder`, `ShippingSlip`; `Money`, `ShippingAddress`, `Membership` |

SOLID: every handler, rule, repository and mapper has one reason to change; behaviour is extended by adding
classes (rules, product types, line types); `PhysicalProduct`/`DigitalProduct` are substitutable for
`Product` everywhere; interfaces are small and role-specific; all dependencies are abstractions injected
through the container.

## API

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/api/v1/purchase-orders` | Submit and process a purchase order |
| `GET` | `/api/v1/purchase-orders/{id}` | The order |
| `GET` | `/api/v1/purchase-orders/{id}/shipping-slip` | Shipping slip generated by BR2 |
| `POST` | `/api/v1/customers` | Register a customer |
| `GET` | `/api/v1/customers/{id}` | Customer with memberships (BR1) and active clubs |
| `GET` | `/api/v1/products` | Catalog |
| `GET` | `/api/v1/products/{id}` | A product |
| `POST` | `/api/v1/products` | Add a book or video |
| `GET` | `/api/v1/membership-plans` | Memberships that can be bought |
| `GET` | `/health` | Liveness probe (`200 Healthy`); not part of the OpenAPI document |

Conventions: JSON only (also for errors, whatever the `Accept` header says), enums as names, money with two
decimals, unknown request members rejected, `201 Created` with a `Location` header for every create, and every
error is an RFC 9457 problem document (`application/problem+json`) with a stable machine-readable `code`
extension and a `traceId`. `POST /api/v1/purchase-orders` accepts an
optional `Idempotency-Key` header (1–128 visible ASCII characters): repeating the request with the same key
and body creates no second order but replays the original `201` (same body and `Location`) with the response
header `Idempotent-Replayed: true`; the same key with a different body is `409 idempotency.payload_mismatch`.

| Status | When | `code` examples |
|--------|------|-----------------|
| 400 | Malformed request (missing fields, unknown enum value, empty lines) | `request.invalid` + `errors` per field |
| 404 | Unknown route resource | `purchase_order.not_found`, `customer.not_found`, `shipping_slip.not_found` |
| 409 | Concurrency conflict after retries were exhausted, duplicate entity, idempotency key reused with another body | `persistence.concurrency_conflict`, `idempotency.payload_mismatch` |
| 413 | Request body larger than 256 KB | `request.too_large` |
| 422 | Semantically invalid: unknown customer/product, overlapping memberships, total mismatch, missing address | `validation.failed` + `errors`, `purchase_order.memberships.overlap`, `customer.shipping_address.missing`, `money.precision` |

## Corner cases covered

* Empty line list, null entries (`"lines": [null]` → 400 keyed `lines[0]`), unknown product or membership plan
  (all reported at once, keyed by position).
* Unknown JSON members are rejected (`"quantity": 3` on a line → 400 keyed by its JSON path), so the request
  schemas in Swagger (`additionalProperties: false`) describe what the server really accepts.
* Error responses are problem documents even when the client sends `Accept: text/html`: the API is JSON-only.
* A present but empty `Idempotency-Key` header is a 400, never a silent "no key".
* Enums accept exactly one declared name, case-insensitively: `"BookClub, VideoClub"`, `"3"` or a number are
  rejected with 400 instead of being combined into `Premium` by the default enum converter.
* A product price is mandatory (a missing `price` is 400, an explicit `0` is allowed).
* An order may contain at most 100 item lines (the 101st line → 400 keyed `lines`) and a request body at most
  256 KB (Kestrel limit), so a single request cannot cause unbounded parsing or processing work.
* A line may not carry the property of the other branch, not even as `null`
  (`{"type":"Product","productId":1,"membershipType":null}` → 400), exactly as the `oneOf` schema states.
* The idempotency fingerprint ignores how a client writes a number: `48.5`, `48.50` and `48.500` are the same
  payload, so a retry that formats the total differently is still replayed instead of rejected. Keys live in one
  global namespace (use UUIDs); without authentication there is no per-client scope to attach them to.
* Prices come from the catalog, never from the client; the optional `expectedTotal` guards against stale
  prices on the client and is rejected on mismatch.
* The same membership twice, or `Premium` together with `BookClub`/`VideoClub`, in one order → rejected.
* Membership already active → `AlreadyActive`, no duplicate membership, no needless write.
* `Premium` after `BookClub` → upgrade; `BookClub` after `Premium` → already covered.
* Digital-only order → no shipping slip, the slip endpoint answers 404 with a distinct code.
* The same book twice → one slip item with quantity 2.
* Physical product for a customer without an address → 422 and *nothing* persisted (atomic unit of work).
* Processing an order twice is refused by both the aggregate and the processor.
* Concurrent orders for one customer → optimistic concurrency (`Version`) detects lost updates; the command
  is retried transparently; the regression suite fires 24 parallel orders and checks each club is activated once.
* Money: negative or >2 decimals rejected; `10`, `10.0`, `10.00` and `10.000` are one value and always
  serialise as `10.00`.
* Trimming and length limits on every text field; ids must be positive (route constraint `min(1)`), and a
  `default` strongly typed id is rejected by every entity and foreign reference.
* Network retries of an order: with an `Idempotency-Key` header the same key and body replay the stored result
  (`201`, same order id and `Location`, header `Idempotent-Replayed: true`) without creating a second order or
  activating a membership twice; the same key with a different body → 409 `idempotency.payload_mismatch`; an
  invalid key (longer than 128 characters, containing whitespace, control or non-ASCII characters) → 400 keyed `Idempotency-Key`. Two
  concurrent requests with one key: the loser's commit fails on the duplicate key and it replays the winner's result.
* Cancellation tokens are honoured; a cancelled commit discards the staged changes like a failed one.
* Unexpected exceptions are logged at Error level with the stack trace while the client only sees a generic 500.

## Tests

| Project | Kind | Focus |
|---------|------|-------|
| Domain.Tests | unit | invariants, activation matrix, totals, slip grouping, value objects |
| Application.Tests | unit (NSubstitute) | processor ordering/skipping/failure, both rules, handlers, retry decorator, mappers, DI composition |
| Infrastructure.Tests | component | atomic commits, duplicate detection, optimistic concurrency, snapshot isolation, seeding |
| Api.Tests | integration + **regression** | the kata scenario, every business-rule outcome through HTTP, concurrency, error contract, Swagger |

Regression tests carry `[Trait("Category", "Regression")]`. Most run against the real HTTP pipeline via
`WebApplicationFactory`; the deterministic concurrency test builds the service container directly so that it
can inject a competing commit. Timestamps are deterministic (`TimeProvider` is injected).

## Performance notes

Measured on one developer machine (Release build, Python client on the same host, so the numbers are a
lower bound of what the server can do): a single sequential client submits an order in about 1.0–1.7 ms (p50)
and 1.9–2.9 ms (p95) depending on warm-up; with 32 concurrent clients the API sustains roughly 2,200–2,700
requests/second for both order submission and reads with p95 under 20 ms and no 409/500 responses. Requests
whose body exceeds the 256 KB limit are answered with `413 request.too_large`. Processing an order (rules, snapshots,
commit) is microseconds; the global store lock only guards dictionary operations, mapping happens outside it,
shipping slips are looked up in O(1) by order id, and request work is bounded by the 100-line and 256 KB limits.
Per-rule logging is at Debug level; one Information event per committed order remains. What would not scale
as-is: unbounded retention of orders, slips and idempotency records in memory, and the unpaginated product list.
