---
description: Create a new Architecture Decision Record in docs/adr using the MADR format
---

Create a new ADR in `docs/adr/`.

1. Find the highest existing `NNNN-*.md` number in `docs/adr/` and use the next
   one (zero-padded to 4 digits).
2. Ask the user for the decision title if not provided.
3. Write `docs/adr/NNNN-<kebab-title>.md` using the MADR format with these
   sections: **Status** (Proposed/Accepted), **Context**, **Decision**,
   **Consequences**, **Alternatives considered**.
4. Keep it tight — an ADR is high-signal, low-effort. One screen is ideal.

Reuse `prompts/adr-from-decision.md` for consistent phrasing.
