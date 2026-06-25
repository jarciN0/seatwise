# ADR-0001: Record architecture decisions

## Status

Accepted

## Context

This is a flagship distributed-systems repository whose value is partly in
*demonstrating judgment*. Non-trivial choices (event sourcing scope, concurrency
model, messaging tech, dependency licensing) need a durable, reviewable record so
a reader — or an interviewer — can see *why*, not just *what*.

## Decision

We will capture every non-trivial architectural decision as a Markdown ADR in
`docs/adr/NNNN-title.md`, using the MADR format: **Context → Decision →
Consequences → Alternatives considered**. At least one ADR per repo records a
deliberate "we chose NOT to do X" (YAGNI) call.

## Consequences

- Decisions are discoverable and survive staff/context changes.
- Small ongoing cost: each significant PR may add or update an ADR.
- ADRs become interview talking points and onboarding material.

## Alternatives considered

- **No formal record (tribal knowledge / commit messages)** — rejected; rationale
  evaporates and can't be cited.
- **A single growing design doc** — rejected; harder to diff, review, and link a
  decision to the PR that made it.
