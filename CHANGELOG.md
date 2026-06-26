# Changelog

All notable changes to this project are documented here.
Format: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versioning: [SemVer](https://semver.org/).

## [Unreleased]

### Added
- Flight-booking flow that works end-to-end via `docker compose up --build`:
  browse flights, hold seats, pay, confirm — with tickets issued on success.
- No-overbooking guarantee enforced by a unique `flightId:seat` key in Postgres.
- Cross-service payment saga: Booking ↔ Payments over RabbitMQ (Wolverine).
- Background job that expires unpaid holds and frees the seats.
- Event-sourced **Booking** aggregate (Marten) with in-memory unit tests.
- Dockerfiles for both services; verified the whole stack runs.

### Changed
- Reframed the domain from generic "ticketing" to **flight booking** (reusable base).
- Slimmed from 8 projects to 4: dropped the empty Gateway / Identity / Notifications /
  Catalog stubs to follow KISS.
- Replaced MassTransit + Redis with Wolverine's RabbitMQ transport + a unique-key
  guard — fewer moving parts.
- Rewrote code comments to plainly describe what each piece does.

### ADRs
- 0001 (record decisions), 0005 (prevent overbooking with a unique key),
  0010 (avoid the .NET libraries that became paid).
