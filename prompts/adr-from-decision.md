# Prompt: ADR from a decision

> **Version:** 1.0.0 · **Updated:** 2026-06-25
> **Changelog:**
> - 1.0.0 — initial version; used to author Seatwise ADRs 0001–0011.

## Template

```
You are drafting an Architecture Decision Record for the Seatwise repository.
Match the existing style in docs/adr/ (MADR format).

Inputs:
- Context: <the forces / problem / constraints at play>
- Decision: <what we are choosing to do>
- Alternatives considered: <other options + why rejected>

Produce a markdown ADR with these sections, in order:
1. Title line: `# ADR-NNNN: <decision>`
2. **Status** — Proposed | Accepted | Superseded
3. **Context** — the forces; why a decision is needed now
4. **Decision** — what we will do, stated plainly
5. **Consequences** — positive AND negative; what this commits us to
6. **Alternatives considered** — each option + the reason it lost

Keep it to roughly one screen. Be concrete and honest about trade-offs —
restraint and named downsides are senior signals.
```
