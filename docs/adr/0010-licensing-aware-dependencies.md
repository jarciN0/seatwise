# ADR-0010: Avoid the .NET libraries that became paid in 2025–26

## Status

Accepted

## Context

During 2025–26 several staple .NET libraries moved to **commercial licenses**:
**MediatR**, **AutoMapper**, **MassTransit** (v9+), and **FluentAssertions** (v8+).
Pulling a now-paid package into an open-source portfolio repo is a poor look and
an avoidable risk.

## Decision

- **Messaging (both in-process and across services):** use **Wolverine**, which is
  open source and also provides its own RabbitMQ transport — so we need neither
  MediatR nor MassTransit.
- **Mapping:** hand-write the few small mappings instead of using AutoMapper.
- **Test assertions:** pin **FluentAssertions 7.2.x**, the last free version.

## Consequences

- No commercial-license exposure in the dependency graph.
- One messaging library (Wolverine) covers in-process handlers and the RabbitMQ
  saga — fewer concepts than splitting MediatR + MassTransit.
- Wolverine needs the `WolverineFx.RuntimeCompilation` package (or pre-generated
  code) because it no longer ships a runtime compiler; we use runtime compilation
  for simplicity in this demo.

## Alternatives considered

- **Pay for MediatR / AutoMapper / MassTransit v9** — rejected for an OSS repo.
- **NServiceBus** — also commercial; rejected.
- **Raw RabbitMQ client** — rejected; loses Wolverine's handler/retry ergonomics.
