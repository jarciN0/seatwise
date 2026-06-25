# ADR-0005: YAGNI — RabbitMQ, not Kafka

## Status

Accepted

## Context

Seatwise is event-driven, so a message broker is core. Kafka is the reflexive
"serious distributed systems" pick, but it brings partitions, consumer-group
offset management, and replay/retention machinery that carry real operational
weight. Our actual scale is a booking platform: thousands of bookings per day,
not millions of events per second. We need routing, dead-lettering, and retries
far more than we need a high-throughput replayable log.

## Decision

Use **RabbitMQ** (via MassTransit 8) as the broker. We explicitly do **not**
adopt Kafka. Revisit only if a *measured* throughput ceiling forces it.

## Consequences

- Lower operational overhead; RabbitMQ + MassTransit gives routing, DLQ, and
  retry policies out of the box, matching the team's existing RabbitMQ experience.
- We forgo Kafka's log-replay/event-replay-from-broker model — acceptable because
  our event *store* (Marten) already holds the authoritative, replayable history.
- If throughput ever exceeds RabbitMQ's comfortable range, a migration is needed;
  the integration-event contracts (V1 records) insulate services from the transport.

## Alternatives considered

- **Kafka / Redpanda** — rejected as premature; partition/offset machinery is
  overhead we don't need at this scale (the YAGNI call).
- **Azure Service Bus / SQS** — rejected for local-first portability; would couple
  the demo to a cloud account.
