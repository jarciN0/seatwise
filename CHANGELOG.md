# Changelog

All notable changes to this project are documented here.
Format: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versioning: [SemVer](https://semver.org/).

## [Unreleased]

### Added
- Solution scaffold: 8 projects under `src/` + `tests/Seatwise.Ordering.Tests`.
- Central Package Management, `Directory.Build.props`, `.editorconfig`, CI skeleton.
- Event-sourced **Order** aggregate (Hold → Reserve → Confirm, expiry/cancel) with unit tests.
- Ordering API (Marten + Wolverine) with hold / reserve / checkout / get endpoints.
- Skeletons: Catalog (EF Core), Identity (OpenIddict), Gateway (YARP), Payments + Notifications stubs.
- AI-FIRST layer: `CLAUDE.md`, `AGENTS.md`, `.claude/` config + commands, `/prompts`.
- ADRs 0001 (record decisions), 0005 (YAGNI: no Kafka), 0010 (licensing).
