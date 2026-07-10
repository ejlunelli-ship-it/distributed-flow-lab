# ADR-014: Sprint 0 scaffolding — toolchain reality vs. canon

- **Status:** Accepted
- **Date:** 2026-07-10

## Context

Sprint 0 (repository scaffolding) is the first implementation increment. It stands up the
Clean Architecture .NET solution, the Vite SPA, the Docker Compose stack, and CI, so that
every later sprint builds on a compiling, testable base. While scaffolding, the concrete
tooling landscape had moved ahead of what the canon documents ([Technologies](../05-dev/technologies.md),
[Folder Structure](../05-dev/folder-structure.md)) were written against in July 2026. This
ADR records the deviations, why each was made, and confirms the canon-critical choices that
were held fixed. Per CLAUDE.md, "new dependencies / deviations require an ADR."

## Decision

### Held to canon (no deviation)

- **React 18 + TypeScript.** The `create-vite` template now defaults to React 19; we
  explicitly pinned **React 18.3** (and matching `@types`) to honor the canon and
  [ADR-010](./ADR-010-frontend-stack.md). React Flow v12 (`@xyflow/react`) supports React 18.
- **Clean Architecture layering.** `Api → Infrastructure → Application → Domain` project
  references, with `Domain` depending on nothing ([ADR-004](./ADR-004-clean-architecture.md)).
- **Full canonical frontend stack:** React Flow (`@xyflow/react`), Zustand,
  `@microsoft/signalr`, Tailwind, Framer Motion, Vitest + Testing Library + Playwright.
- **Backend stack targets:** ASP.NET 8 (`net8.0`), xUnit, and the documented test-project split.

### Deviations from the canon's written detail

1. **Root layout `src/` + `web/` (not `backend/` + `frontend/`).** The repo was initialized
   with empty `backend/{src,tests}` and `frontend/{src,tests}` folders, which conflicted with
   the canonical [Folder Structure](../05-dev/folder-structure.md) (`src/`, `tests/`, `web/`,
   `docker/` at the root). We adopted the **canon** layout and removed the divergent folders.
   *Rationale:* documentation is the source of truth; the rest of the canon (`.sln`, ADRs,
   Dockerfiles) references `src/` and `web/`.

2. **.NET SDK 9 building `net8.0`.** Only the .NET **9** SDK is installed on the build host,
   but the `net8.0` targeting pack is present. `global.json` pins the SDK with
   `rollForward: latestMinor`; all projects target `net8.0`. Runtime behavior is .NET 8 as the
   canon requires; the SDK is merely the build toolchain.

3. **Vite 8 + TypeScript 6.** The template ships Vite 8 and TS ~6. These are newer than the
   canon's implied versions but are backward-compatible for our usage. One consequence:
   `baseUrl` is deprecated in TS 6, so the `@/*` path alias is declared with `paths` only
   (resolved relative to `tsconfig`).

4. **ESLint flat config + Prettier (replacing the template's `oxlint`).** The canon names
   ESLint + Prettier and files `.eslintrc.cjs` / `.prettierrc`. Modern ESLint uses **flat
   config**, so the file is `eslint.config.js` (not `.eslintrc.cjs`); `.prettierrc` is as
   documented. We removed the template's `oxlint` to match the canon's linter choice.

5. **MVP Compose set only.** `docker/docker-compose.yml` contains `web`, `api`, `rabbitmq`,
   `postgres` — the MVP container set. Kafka and Redis are deferred to Version 1 exactly as
   the [backlog](../01-product/backlog.md) Epic 7 task "Add kafka & redis to stack" specifies.

## Alternatives

- **Downgrade Vite/TS to older majors to match the canon literally.** Rejected: fights the
  template, adds maintenance drag, and yields no behavioral benefit; the newer majors are
  compatible and the canon fixes *libraries*, not exact minor versions.
- **Keep `backend/` + `frontend/`.** Rejected: contradicts the authoritative folder-structure
  doc and the paths baked into the solution, Dockerfiles, and other ADRs.
- **Keep `oxlint`.** Rejected: the canon explicitly names ESLint + Prettier; consistency of the
  documented developer workflow outweighs `oxlint`'s speed at this stage.

## Consequences

### Positive
- A compiling, tested baseline: backend builds warnings-as-errors clean, unit tests pass,
  the SPA type-checks/lints/tests/builds, Docker images build, and Compose config validates.
- No drift between the physical repo and the canon's folder structure.
- Canon-critical invariants (React 18, Clean Architecture, full stack) are preserved.

### Negative
- Minor documentation drift remains: [Technologies](../05-dev/technologies.md) and
  [Folder Structure](../05-dev/folder-structure.md) still imply older tool versions and the
  `.eslintrc.cjs` filename. *Mitigation:* this ADR is the authoritative record until those
  docs are refreshed; the deviations are small and localized.
- Building `net8.0` on the .NET 9 SDK depends on the `net8.0` targeting pack being installed
  in every environment. *Mitigation:* `global.json` + CI `setup-dotnet` with
  `global-json-file` make the toolchain explicit and reproducible.

## Related documents

- [Technologies](../05-dev/technologies.md)
- [Folder Structure](../05-dev/folder-structure.md)
- [Docker](../05-dev/docker.md)
- [Roadmap](../01-product/roadmap.md)
- [Backlog](../01-product/backlog.md)
- [ADR-004: Clean Architecture](./ADR-004-clean-architecture.md)
- [ADR-010: Frontend stack](./ADR-010-frontend-stack.md)
- [ADR Index](./README.md)
