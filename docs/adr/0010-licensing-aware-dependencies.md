# ADR-0010: Licensing-aware dependency choices (Wolverine over MediatR; MassTransit 8; no AutoMapper)

## Status

Accepted

## Context

During 2025–26 several staple .NET libraries moved to **commercial licenses**:
**MediatR** (v13+), **AutoMapper**, and **MassTransit** (v9+). FluentAssertions
also went commercial at v8. A senior portfolio built in 2026 must demonstrate
*awareness of this shift and a deliberate response* — not naively pull a
now-paid package into an OSS showcase.

## Decision

- **In-proc CQRS / mediation:** use **Wolverine** (OSS, JasperFx family) instead
  of MediatR. It co-locates the transactional outbox with the message handler and
  integrates natively with Marten — fewer moving parts for our saga.
- **Bus:** pin **MassTransit 8.x** (still Apache-2.0) rather than v9 (commercial).
  Document the EOL/relicense risk explicitly and track it.
- **Mapping:** hand-written mapping / `record` projections instead of AutoMapper —
  the shapes are simple (KISS) and it sidesteps the license entirely.
- **Assertions:** pin **FluentAssertions 7.2.x** (last free version).

## Consequences

- Zero commercial-license exposure in the dependency graph today.
- MassTransit 8 is a known future risk — if it follows v9 commercially we migrate;
  the `Seatwise.Contracts.V1` records insulate services from the bus library.
- Wolverine is less ubiquitous than MediatR, a small learning-curve cost — but its
  Marten/outbox integration is a net win here and a defensible interview story.

## Alternatives considered

- **Pay for MediatR/AutoMapper/MassTransit v9** — rejected for an OSS portfolio repo;
  also misses the chance to demonstrate the judgment this ADR records.
- **Hand-roll a dispatcher** — rejected; reinvents Wolverine without the outbox
  integration.
- **Switch bus to NServiceBus / raw RabbitMQ client** — rejected; NServiceBus is
  also commercial, and the raw client loses the saga/outbox/retry ergonomics.
