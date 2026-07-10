# ADR-015: OSS dependency pinning — MediatR 12.x and FluentAssertions 7.x

- **Status:** Accepted
- **Date:** 2026-07-10

## Context

Sprint 1 introduced the first backend runtime dependencies: **MediatR** (the canonical CQRS
mediator, [ADR-008](./ADR-008-cqrs-mediatr.md)) and **FluentValidation** (canonical input
validation), plus test-only dependencies **FluentAssertions**, **NSubstitute**, and
`Microsoft.Extensions.TimeProvider.Testing`. Two of these changed their licensing after the
canon was written:

- **MediatR 13+** moved to a commercial license (Lucky Penny); 12.x remains Apache 2.0.
- **FluentAssertions 8+** moved to a paid Xceed license for commercial use; 7.x remains
  Apache 2.0.

DFL is developed as a real SaaS product; unplanned commercial license obligations in core
libraries are not acceptable without an explicit decision.

## Decision

- Pin **MediatR to 12.x** (currently 12.5.0) — the last fully open-source line. The
  Application layer uses only stable 12.x surface (`IRequest`, `IRequestHandler`,
  `IPipelineBehavior`, `AddMediatR`).
- Pin **FluentAssertions to 7.x** (currently 7.2.0) in all test projects.
- FluentValidation (11.x), NSubstitute (5.x), and the Microsoft.Extensions packages remain
  on their current open-source lines; no restriction needed.
- Version upgrades that would cross a license boundary require a new ADR.

## Alternatives

### Adopt MediatR 13+ / FluentAssertions 8+ under commercial licenses
**Rejected for now:** no feature of the newer majors is needed; paying for licenses at this
stage adds cost without value.

### Replace MediatR with a hand-rolled mediator or direct handler injection
**Rejected:** the canon and [ADR-008](./ADR-008-cqrs-mediatr.md) fix MediatR as the CQRS
mechanism; 12.x satisfies it fully. Re-evaluating the mediator itself is out of scope here
and would warrant its own ADR if 12.x ever becomes untenable.

### Replace FluentAssertions with xUnit built-in asserts or Shouldly
**Rejected:** the canon names FluentAssertions; 7.x is feature-complete for our assertion
style and remains maintained for security fixes.

## Consequences

### Positive
- Zero commercial license exposure; all dependencies remain Apache/MIT.
- Stable, well-documented APIs; both pinned lines are mature.

### Negative
- No access to MediatR 13+ / FluentAssertions 8+ features. Mitigation: none needed today;
  a future ADR can revisit if a concrete need appears.
- Pinned majors eventually stop receiving fixes. Mitigation: revisit at each phase boundary
  (V1, V2, …) as part of dependency review.

## Related documents

- [Technologies](../05-dev/technologies.md)
- [Testing](../05-dev/testing.md)
- [ADR-008: CQRS via MediatR](./ADR-008-cqrs-mediatr.md)
- [ADR-014: Sprint 0 scaffolding](./ADR-014-scaffolding-toolchain-reality.md)
- [ADR Index](./README.md)
