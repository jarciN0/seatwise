# AGENTS.md

Vendor-neutral mirror of [`CLAUDE.md`](CLAUDE.md). See that file for the full
domain glossary, architecture, the oversell invariant, build/test commands, and
the "never do X" list. Kept in sync so any AI tool can onboard, not just Claude.

Quick start: `docker compose up -d` then `dotnet build && dotnet test`. The
crown jewel is `src/Seatwise.Ordering.Api` (event-sourced Order aggregate).
