# CLAUDE.md — seatwise

Context for AI agents (and humans). seatwise is a flight-seat booking backend.
Its one rule that must never break: **a seat can never be booked twice.**

## The two services
- **Booking** (`src/Seatwise.Ordering.Api`) — flights, seat holds, bookings. Uses
  Marten (Postgres) to store each booking as a stream of events.
- **Payments** (`src/Seatwise.Payments.Api`) — a mock card processor.

They communicate only by sending the records in `Seatwise.Contracts.V1` over
RabbitMQ (via Wolverine). Don't make one service call the other directly.

## Words we use
- **Flight** — a scheduled flight with a fixed list of seat numbers ("1A", "1B"…).
- **SeatHold** — one row per claimed seat, keyed `flightId:seat`. Its unique key is
  what prevents overbooking.
- **Booking** — a customer's purchase: Held → AwaitingPayment → Confirmed, or
  Cancelled / Expired.

## How overbooking is prevented (don't weaken this)
Holding seats inserts SeatHold rows. Because the row id is `flightId:seat` and ids
are unique, two simultaneous holds for the same seat can't both succeed — Postgres
rejects the second and that request gets a 409. Keep using `Insert` (not `Store`)
for new holds so the database stays the source of truth.

## Build / run / test
```bash
docker compose up --build      # whole stack; API at http://localhost:8081/swagger
dotnet build                   # compile
dotnet test                    # booking-rule unit tests (no database needed)
```

## Conventions
- `record` types for events and messages; keep them immutable.
- Pass `CancellationToken` through async methods.
- Hand-write small mappings; no AutoMapper.
- Booking rules live in the `Booking` class as plain methods that return events —
  no database calls in there, so they stay easy to test.

## Don't
- Don't add MediatR, AutoMapper, or MassTransit v9 — they're paid now. Use Wolverine.
- Don't let one service read another's database or call its HTTP API — go through
  RabbitMQ messages instead.
- Name Wolverine message-handler classes ending in `Handler` or Wolverine won't find them.
