---
description: Scaffold a new versioned integration event end-to-end (record + consumer + contract test + changelog)
---

Scaffold a new versioned integration event across the codebase. This automates
the single most repetitive multi-file chore in an event-driven system.

Input from the user: **event name**, **fields** (name + type), and the
**producer / consumer services**.

Steps:

1. Add a `sealed record <Name>V1(...)` to `src/Seatwise.Contracts/IntegrationEventsV1.cs`
   in the `Seatwise.Contracts.V1` namespace. Every message MUST include a
   `Guid CorrelationId` and use UTC `DateTimeOffset` timestamps. Additive-only
   within V1 (ADR-0007).
2. In each consumer service, generate a MassTransit consumer skeleton
   (`IConsumer<<Name>V1>`) with inbox registration and a `// TODO` body.
3. Add a contract test asserting additive compatibility (a field added to V1 must
   not break existing consumers).
4. Append a `CHANGELOG.md` entry under "Unreleased".

Remind the user: domain events stay private to their aggregate; only V1 records
cross the bus. Run `dotnet build && dotnet test` before finishing.
