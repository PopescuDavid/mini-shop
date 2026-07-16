# AI usage

I used an AI assistant (Claude Code) during this exercise. A short summary below.

## How I used AI
- Scaffolded the initial solution: project layout, EF Core setup, controllers, DTOs, the background job, and the first pass of the Web Components UI.
- Used it as a reviewer/pair to critique the code, spot gaps, and suggest improvements.
- Helped write the unit and integration tests.

## Corrections & adjustments I made to AI-generated code
- Fixed record validation attributes to target constructor parameters (ASP.NET rejects them on the generated properties).
- Removed an unnecessary, transitively-vulnerable package and pinned EF Core versions to fix an assembly conflict.
- Replaced HTML string interpolation in the front-end with `textContent`/DOM construction to avoid injection.
- Added a discount snapshot, constant-time login, DB check constraints, startup config validation, and a health endpoint after reviewing the first version.

## Technical decisions & planning
- Single project layered by responsibility (Controllers → Services → Data) instead of multi-project Clean Architecture, given the small domain.
- Snapshot unit price and discount so an order's totals stay stable if prices/coupons change later.
- Enforce ownership by scoping every order query to the JWT user; return 404 (not 403) for other users' orders.
- Reserve stock on the draft and maintain that invariant across the order lifecycle.
- Testcontainers (real PostgreSQL) for integration tests rather than an in-memory provider.

## Challenges
- Modelling stock consistency across the order lifecycle, and being explicit about its concurrency limits (documented the read-modify-write race and the production fix).
- Keeping the Web Components cleanly bounded (shared store + events) without a framework.
- Testing the background sweeper deterministically — solved by extracting it into an injectable service.
