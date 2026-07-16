# Mini Online Shop — Products & Orders

A small but complete slice of an online shop: a .NET 10 Web API backed by PostgreSQL, a native Web Components front-end, JWT auth, and a background job — all runnable with a single command.

## Tech stack

- **Backend:** .NET 10 Web API (controllers)
- **Data:** PostgreSQL 16 + EF Core 10 (code-first, migrations applied on startup)
- **Auth:** JWT bearer (HMAC-SHA256), one pre-seeded user
- **Front-end:** native Web Components (Custom Elements + Shadow DOM), no framework
- **Background job:** hosted `BackgroundService` that expires abandoned draft orders
- **Tests:** xUnit — unit (pricing) + integration (full HTTP stack against a throwaway Postgres via Testcontainers)

## Prerequisites

- **Docker + Docker Compose** — the only requirement for the default run path.
- **.NET 10 SDK** — only needed to run the app outside Docker or to run the tests.

## Run everything (Docker)

```bash
docker compose up --build
```

This starts PostgreSQL and the API. On startup the API waits for the database, applies EF Core migrations, and seeds data idempotently.

- **App + UI:** http://localhost:8080
- **PostgreSQL:** localhost:5432 (`shop` / `shop`)

Open http://localhost:8080 and sign in with the seeded credentials below.

**Follow the logs** (handy for watching the background job run):

```bash
docker compose logs -f api
```

## Seeded credentials

| Email | Password |
| --- | --- |
| `demo@shop.test` | `Passw0rd!` |

**Coupons** (to exercise the discount flow):

| Code | Effect |
| --- | --- |
| `SAVE10` | 10% off |
| `WELCOME25` | 25% off |
| `FLAT15` | $15 off (fixed) |
| `EXPIRED5` | inactive — demonstrates rejection |

## Using the UI

1. Log in as the seeded user (the form is pre-filled).
2. Browse the paged, sortable product list.
3. Add products with quantities to build an order, optionally apply a coupon, and create it.
4. View the created order with its computed totals, **place** it (Draft → Placed), or delete it.

## API

All `/orders` endpoints require a bearer token and operate only on orders owned by the caller.

| Method & path | Purpose |
| --- | --- |
| `POST /auth/login` | Authenticate the seeded user, return a JWT. |
| `GET /products` | List products. `?page=&pageSize=&sortBy=&sortDir=` (sort by `name`, `price`, `stock`, `category`, `createdAt`). |
| `POST /orders` | Create a draft order with one or more line items and an optional coupon. |
| `GET /orders/{id}` | Fetch a single order with items and computed subtotal/discount/total. |
| `PUT /orders/{id}` | Replace items, change quantities, apply/remove a coupon, or place the order. |
| `DELETE /orders/{id}` | Soft-delete (cancel) an order and release its reserved stock. |
| `GET /health` | Readiness probe — returns `Healthy` and checks DB connectivity (the Docker container uses a lightweight TCP liveness probe). |

Example requests for every endpoint are in [`requests.http`](./requests.http) (VS Code REST Client / Rider). Quick curl:

```bash
TOKEN=$(curl -s -X POST http://localhost:8080/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"demo@shop.test","password":"Passw0rd!"}' | jq -r .token)

curl -s "http://localhost:8080/products?page=1&pageSize=5&sortBy=price&sortDir=desc"
```

## Run locally (without Docker for the API)

```bash
docker compose up -d db                 # PostgreSQL only
dotnet run --project src/Shop.Api        # API on http://localhost:5182
```

## Tests

```bash
dotnet test
```

- **Unit** (`OrderPricingServiceTests`): subtotal, percentage/fixed coupons, clamping, rounding — the pure pricing logic.
- **Integration** (`OrdersControllerTests`, `OrderLifecycleTests`): drive the real HTTP stack against a disposable PostgreSQL container (Testcontainers) — login, auth required (401), ownership isolation (404), stock validation (409) and computed totals; plus the full order lifecycle: quantity increase/decrease, add/remove line with stock restoration, apply/remove coupon on update, duplicate-line consolidation, invalid quantity (400), Draft→Placed, rejecting edits to a placed order (409), delete releasing stock, and the abandoned-draft sweeper expiring a stale draft and releasing its reserved stock.

Integration tests require Docker to be running. If Docker Hub credentials on the machine are stale (Testcontainers fails to pull its reaper image with an authentication error), either run `docker logout` or set `TESTCONTAINERS_RYUK_DISABLED=true` before `dotnet test`.

## Project structure

```
mini-shop/
├── docker-compose.yml            # Postgres + API
├── requests.http                 # example requests for every endpoint
├── src/Shop.Api/
│   ├── Controllers/              # Auth, Products, Orders (thin HTTP layer)
│   ├── Entities/                 # User, Product, Order, OrderItem, Coupon
│   ├── Dtos/                     # request/response records + PagedResult<T>
│   ├── Services/                 # TokenService, OrderService, OrderPricingService
│   ├── Data/                     # ShopDbContext, DbSeeder, Migrations
│   ├── Jobs/                     # AbandonedOrderSweeper
│   ├── ExceptionHandling/        # domain exceptions + ProblemDetails handler
│   └── wwwroot/                  # Web Components UI
└── tests/Shop.Api.Tests/
```

## Architecture & key decisions

- **One project, layered by responsibility** (Controllers → Services → EF Core). The domain is small; a multi-project Clean Architecture would add ceremony without payoff. Controllers stay thin; the rules live in services.
- **Pricing is a pure service** (`OrderPricingService`) with no I/O, so it is exhaustively unit-testable in isolation.
- **Unit price and the applied discount are snapshotted** — the unit price on each order item and the resolved discount amount on the order — so an order's total stays stable even if the catalogue price or the coupon is changed later. The subtotal is still derived on read from the snapshotted line prices; only the coupon-dependent discount needed freezing.
- **Money** is `decimal` mapped to `numeric(18,2)`; discounts round half-away-from-zero and the total is clamped at zero.
- **Ownership** is enforced by filtering every order query on the authenticated user id (from the JWT `sub` claim). Accessing another user's order returns **404**, not 403, so the API does not leak the existence of other users' resources.
- **Login runs in constant time** — a password hash is always verified (against a dummy hash when the email is unknown), so response timing does not reveal which emails are registered.
- **Stock and coupon values are guarded at the database** with `CHECK` constraints (`StockQuantity >= 0`, coupon `Value >= 0`), so even a bug or a concurrent write cannot persist impossible data.
- **Errors** are returned as RFC-7807 `ProblemDetails` via a single global exception handler mapping domain exceptions to 400/404/409.
- **Paging is deterministic**: every product sort has a unique tiebreaker (`ThenBy(Id)`), so rows never shuffle between pages when the primary key has duplicates (e.g. equal category or price).
- **Configuration is validated at startup** (`ValidateDataAnnotations().ValidateOnStart()` for `Jwt`/`Sweeper`), so a missing signing key or nonsensical interval fails fast with a clear message instead of surfacing later.
- **The front-end never uses HTML string interpolation for data**: dynamic values (product names, order fields, coupons, error messages, the reflected order-id) are written via `textContent`/DOM construction, not `innerHTML`, so untrusted or reflected strings cannot inject markup.
- **The front-end is served by the API** from `wwwroot`, so everything is same-origin — no CORS, and one command runs the whole stack.

### Order lifecycle & stock

Stock is treated as a first-class invariant: **an order holds its line items' reserved stock while it is `Draft` or `Placed`; `Expired` and `Cancelled` orders hold none.** Every transition maintains that invariant.

```
POST /orders     → Draft        reserve stock for each line
PUT  /orders/{id} → edit Draft   diff items, apply the stock delta, recompute totals
                  → place        Draft → Placed (validated); placed orders are read-only
DELETE /orders/{id} → Cancelled  soft-delete and release reserved stock
sweeper           → Expired      stale drafts release their stock
```

### Background job

`AbandonedOrderSweeper` (a `BackgroundService` driven by `PeriodicTimer`) runs on a configurable interval, finds `Draft` orders untouched for longer than a configurable threshold, marks them `Expired`, releases their reserved stock, and logs what it did on every run. Configure via `Sweeper__IntervalSeconds` and `Sweeper__DraftExpiryMinutes`; the code defaults are 60s / 30min, and the Docker stack sets a **1-minute** expiry so the job is easy to observe. Watch it with `docker compose logs -f api` — each run logs either `Sweep complete: no abandoned drafts.` or `Sweep complete: expired N draft(s), released M reserved unit(s).`.

## NOTES

**Trade-offs**
- Stock reservation happens on the draft. Under concurrent writes this is a read-modify-write race; it is currently guarded by a single atomic `SaveChanges`. The production fix is optimistic concurrency (a `xmin` rowversion on `Product`) with a retry — deliberately left out for time and called out here.
- The JWT signing key and DB credentials live in configuration for a one-command local run. In a real deployment they would come from environment variables / a secret manager.
- The client holds the token in memory (not `localStorage`) to avoid XSS exfiltration; the trade-off is that a page refresh requires re-login.

**Skipped (out of scope for this slice)**
- Checkout/payment, refresh tokens, product/coupon CRUD, order listing/pagination, and OpenAPI/Swagger UI.

**With more time**
- Optimistic concurrency on stock (an `xmin` rowversion on `Product` with a retry) to close the documented read-modify-write race; a scale-safe (single-owner) sweeper for multi-instance deployments; request-validation via FluentValidation; login rate-limiting; observability (structured tracing/metrics); and a CI pipeline running the test suite.

## AI usage

- Used an AI assistant (Claude Code) to scaffold the solution, EF configuration, DTOs, controllers, the background job, and the first pass of the Web Components.
- Reviewed and reworked the generated code throughout: consistent naming, removed comments, tightened method boundaries, and verified `async`/nullable correctness.
- Decisions I directed (with AI as a sounding board): the single-project layered structure, reserve-stock-on-draft invariant, snapshotting unit price, 404-over-403 for ownership, in-memory client token, and Testcontainers for integration tests.
- Corrections I made to AI output: fixed record validation attributes to target constructor parameters (ASP.NET Core rejects them on the generated properties); removed a transitively-vulnerable OpenAPI package that wasn't needed; pinned EF Core package versions to resolve an assembly conflict; and used the non-obsolete Testcontainers builder constructor.
- Challenges: modelling stock consistency across the order lifecycle and being explicit about its concurrency limits, and keeping the Web Components cleanly bounded (shared store + custom events) without a framework.
