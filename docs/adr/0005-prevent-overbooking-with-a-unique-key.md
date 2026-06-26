# ADR-0005: Prevent overbooking with a unique database key, not a distributed lock

## Status

Accepted

## Context

The one rule seatwise must never break is that a seat can't be booked twice, even
when many people try to book the same seat at the same moment. The usual answers
are a distributed lock (e.g. Redis/RedLock) or pessimistic database locks — both
add moving parts and are easy to get subtly wrong.

## Decision

Store one row per held seat with the primary key `flightId:seat`, and **insert**
it (not upsert) when holding. A primary key is unique, so two simultaneous holds
for the same seat can't both succeed: the database rejects the second insert and
that request returns `409`. The database is the single source of truth.

## Consequences

- No Redis, no lock service — one fewer thing to run and reason about (KISS).
- Correct under concurrency by construction: the guarantee is the database's unique
  constraint, not application code that could race.
- A just-expired hold can still hold its key for a few seconds until the background
  sweeper deletes it; acceptable, and a confirmed seat is simply never freed.

## Alternatives considered

- **Redis distributed lock** — rejected: extra infrastructure and careful TTL/renewal
  handling for a guarantee the database already gives us for free.
- **`SELECT … FOR UPDATE` pessimistic locks** — rejected: more complex and easier to
  deadlock than letting a unique key do the job.
