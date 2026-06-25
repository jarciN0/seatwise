# Prompts

Versioned, reusable prompt templates — treated as code (AI-FIRST Layer A).

## Organization

- One file per prompt, named by intent (`adr-from-decision.md`).
- Each prompt carries a **changelog header** (version + date + what changed) so
  prompt evolution is auditable, same as any other dependency.
- Breaking changes to a prompt bump its version; the changelog records why.

## Index

| Prompt | Purpose |
|---|---|
| [`adr-from-decision.md`](adr-from-decision.md) | Draft a MADR ADR from a context + decision + alternatives. Used to author ADRs 0001–0011 consistently. |

> Planned (per blueprint §3.7): `saga-failure-case-review.md` — enumerate untested
> saga failure/interleaving cases and propose Testcontainers test names.
