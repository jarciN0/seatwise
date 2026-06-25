# Seatwise Storefront (SPA) — TODO

🚧 **Not yet scaffolded.** A thin React 19 + TypeScript + Vite storefront lands
with milestone **M8** (blueprint §3.1). It is deliberately optional and the last
thing to trim if time compresses — the backend correctness story is the
non-negotiable value of this repo.

Planned scope (intentionally thin):
- OIDC login (`authorization_code` + PKCE) against the Identity service.
- Seat-map view for a Showing with a **live 120 s hold countdown**.
- Checkout flow → "Confirmed" with ticket code.
- The **oversell-race demo** visualization (the README hero GIF).
- One thin admin screen: create Showing + view sales.

Stack: React 19, TypeScript 5, Vite 8, Vitest, `oidc-client-ts`.
