# CLAUDE.md — project context for AI agents

Seatwise is an **event-sourced ticket-booking platform** whose entire reason to
exist is to prove **zero seat oversell under concurrent load**. Read this before
touching code; it makes you (agent or human) productive in minutes.

## Domain glossary (canonical — use these exact terms everywhere)

- **Venue** — a physical place with a fixed seat map.
- **Showing** — a performance at a Venue at a time. We say *Showing*, **never
  "Event"**, to avoid colliding with event-sourcing "event" (ADR-0009).
- **Seat** — an addressable position in a Showing (`section/row/number`).
- **Hold** — a short-lived (default **120 s**) exclusive claim on seats, pending checkout.
- **Reservation** — a hold committed to an Order (survives until payment outcome).
- **Order** — the event-sourced aggregate tracking a purchase across its lifecycle.

**Order lifecycle:** `Drafting → Reserved → AwaitingPayment → Confirmed`, with
`Expired` (TTL) and `PaymentDeclined → Cancelled` (compensation) branches.

## Architecture

Bounded contexts: **Identity** (OpenIddict), **Gateway** (YARP), **Catalog**
(EF Core read model), **Ordering** (Marten event store — the crown jewel),
**Payments** (stub), **Notifications** (stub). See README Mermaid diagram.

> **Rule:** bounded contexts never reference each other's internals. Only
> `Seatwise.Contracts.V1` records cross the message bus.

## The invariant — stated as law

> **Never weaken the oversell guarantee.** Any change touching holds, locks,
> reservations, or the event stream MUST keep the (forthcoming) k6 oversell test
> green: N concurrent requests, M < N seats ⇒ exactly M sold, 0 oversold.

Three layers enforce it (blueprint §2.6): (1) per-seat Redis RedLock guarding
only the short critical section; (2) a TTL'd Redis hold record carrying the 120 s
exclusivity; (3) optimistic-concurrency (expected-version) appends on the stream.

## Build / test commands

```bash
docker compose up -d                              # Postgres + RabbitMQ + Redis
dotnet build                                      # whole solution
dotnet test                                       # unit tests (Order aggregate)
dotnet run --project src/Seatwise.Ordering.Api    # the core service
# TODO: k6 run tests/load/oversell.js             # the headline proof (M5)
# TODO: Testcontainers integration tests (M6)
```

## Conventions (delta from STANDARDS.md)

- `record` types for domain events and integration contracts; immutable.
- `CancellationToken` plumbed through every async method.
- `ProblemDetails` (RFC 9457) is the uniform error shape; `409` on seat conflict
  carries `conflictingSeatIds`.
- Hand-written mapping / record projections — **no AutoMapper**.

## Never do X

- **Never use MediatR, AutoMapper, or MassTransit v9** — all commercial as of
  2025–26 (ADR-0010). Use **Wolverine**, hand-mapping, **MassTransit 8**.
- **Never hold the seat lock for the whole 120 s hold window** — lock the
  critical section only; the TTL'd hold record carries exclusivity.
- **Never publish a domain event on the bus** — only `Seatwise.Contracts.V1`
  integration events cross service boundaries.
- **Never trust a client-supplied price** — the server is source of truth.
- **Never bypass the `Idempotency-Key`** on a mutating endpoint.
- **Never merge AI-generated code without a green pipeline + a human read.**
