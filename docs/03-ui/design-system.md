# Design System

> **Scope.** The visual and interaction foundations of the DFL web client: color tokens (semantic +
> per-`NodeType` + per-event-type, light & dark), typography, spacing, iconography, motion principles
> (cross-linked to [animations.md](./animations.md)), and accessibility (WCAG 2.1 AA). Tokens are
> implemented as Tailwind CSS theme extensions + CSS custom properties (canon §2). Terms follow the
> [project Glossary](../01-product/glossary.md).

## 1. Principles

1. **The interface teaches.** Color and motion carry meaning — a red edge, a pulsing node, a
   circuit-breaker turning amber are *information*, never decoration.
2. **One system, two themes.** Every token has a light and dark value; components never hardcode hex.
3. **Semantic first.** Components consume semantic tokens (`--color-surface`, `--color-danger`), not
   raw palette values, so re-theming is a token swap.
4. **Accessible by construction.** Contrast, keyboard, ARIA, and reduced-motion are requirements, not
   add-ons.

## 2. Color tokens

### 2.1 Semantic palette

| Token | Role | Light | Dark |
|-------|------|-------|------|
| `--color-bg` | App background | `#F7F8FA` | `#0E1116` |
| `--color-surface` | Panels, cards, rails | `#FFFFFF` | `#161B22` |
| `--color-surface-2` | Nested surfaces, inputs | `#F0F2F5` | `#1C232C` |
| `--color-border` | Dividers, node borders | `#D8DEE6` | `#2C3542` |
| `--color-text` | Primary text | `#0E1116` | `#E6EDF3` |
| `--color-text-muted` | Secondary text | `#5A6572` | `#9AA7B4` |
| `--color-primary` | Brand, primary actions | `#2563EB` | `#3B82F6` |
| `--color-primary-contrast` | Text on primary | `#FFFFFF` | `#0E1116` |
| `--color-success` | Ack, healthy, closed CB | `#15803D` | `#22C55E` |
| `--color-warning` | Retry, half-open CB, latency | `#B45309` | `#F59E0B` |
| `--color-danger` | Failure, nack, open CB, DLQ | `#B91C1C` | `#EF4444` |
| `--color-info` | Neutral events, hints | `#0E7490` | `#22D3EE` |
| `--color-focus-ring` | Keyboard focus outline | `#2563EB` | `#60A5FA` |

### 2.2 Per-`NodeType` colors

Each canonical `NodeType` (canon §5) has an accent used for its node icon, border tint, and palette
chip. Accents are chosen for mutual distinguishability and AA contrast against both surfaces.

| NodeType | Accent (light) | Accent (dark) | Icon glyph (§4) |
|----------|----------------|---------------|-----------------|
| `Producer` | `#2563EB` | `#3B82F6` | upload/arrow-out |
| `Consumer` | `#0891B2` | `#22D3EE` | download/arrow-in |
| `Service` | `#6D28D9` | `#A78BFA` | cube / gear |
| `ApiGateway` | `#4338CA` | `#818CF8` | gateway / door |
| `LoadBalancer` | `#0D9488` | `#2DD4BF` | split arrows |
| `Exchange` | `#C2410C` | `#FB923C` | hub / router |
| `Queue` | `#B45309` | `#F59E0B` | stacked bars |
| `Topic` | `#9333EA` | `#C084FC` | layers |
| `Partition` | `#7E22CE` | `#D8B4FE` | slice |
| `Broker` | `#B91C1C` | `#F87171` | server-rack |
| `Database` | `#1D4ED8` | `#60A5FA` | cylinder |
| `Cache` | `#DB2777` | `#F472B6` | lightning / bolt |
| `DeadLetterQueue` | `#7F1D1D` | `#FCA5A5` | skull / trash-warning |
| `Client` | `#475569` | `#94A3B8` | user / laptop |

### 2.3 Per-event-type colors

Event-type colors are shared by the EventLog chips, TimelineScrubber markers, and animation accents so
one color always means one class of event. Grouped per canon §7 catalog.

| Event group | Representative types | Token | Light | Dark |
|-------------|----------------------|-------|-------|------|
| Lifecycle | `SimulationStarted`, `SimulationPaused`, `SimulationResumed`, `SimulationStopped`, `SimulationCompleted`, `TickAdvanced` | `--evt-lifecycle` | `#475569` | `#94A3B8` |
| Node | `NodeActivated`, `NodeStateChanged`, `NodeFailed`, `NodeRecovered`, `ConsumerRegistered` | `--evt-node` | `#0E7490` | `#22D3EE` |
| Messaging (normal) | `MessagePublished`, `MessageRouted`, `MessageEnqueued`, `MessageDequeued`, `MessageReceived`, `MessageProcessed`, `AckReceived` | `--evt-msg` | `#2563EB` | `#3B82F6` |
| Messaging (trouble) | `MessageNacked`, `RetryScheduled`, `MessageRetried`, `MessageExpired`, `MessageDropped` | `--evt-msg-warn` | `#B45309` | `#F59E0B` |
| Dead-letter | `DeadLettered` | `--evt-dlq` | `#B91C1C` | `#EF4444` |
| HTTP / RPC | `HttpRequestStarted`, `HttpResponseReceived`, `GrpcCallStarted`, `GrpcCallCompleted` | `--evt-http` | `#0891B2` | `#22D3EE` |
| HTTP / RPC (fail) | `HttpRequestFailed`, `HttpRequestTimedOut` | `--evt-http-fail` | `#B91C1C` | `#EF4444` |
| Resilience | `CircuitBreakerOpened`, `CircuitBreakerHalfOpened`, `CircuitBreakerClosed`, `SagaStarted`, `SagaStepCompleted`, `SagaCompensationTriggered`, `SagaCompleted`, `CacheHit`, `CacheMiss`, `CacheEvicted` | `--evt-resilience` | `#6D28D9` | `#A78BFA` |
| Fault injection | `FaultInjected`, `LatencyInjected`, `PartitionCreated`, `PartitionHealed` | `--evt-fault` | `#DB2777` | `#F472B6` |

> **Circuit-breaker state colors** (used for `CircuitBreakerOpened/HalfOpened/Closed` node state):
> Closed = `--color-success`, Half-open = `--color-warning`, Open = `--color-danger`. See
> [animations.md §Circuit breaker](./animations.md).

## 3. Typography

| Token | Family | Usage |
|-------|--------|-------|
| `--font-sans` | Inter, system-ui, sans-serif | UI text, labels, body |
| `--font-mono` | JetBrains Mono, ui-monospace, monospace | Event payloads, ids, JSON, metrics |

Type scale (rem, 1rem = 16px base):

| Token | Size | Line height | Weight | Usage |
|-------|------|-------------|--------|-------|
| `--text-xs` | 0.75 | 1.0 | 500 | Badges, event chips, timeline labels |
| `--text-sm` | 0.875 | 1.25 | 400/500 | Inspector fields, table cells |
| `--text-base` | 1.0 | 1.5 | 400 | Body, descriptions |
| `--text-lg` | 1.125 | 1.5 | 600 | Section headers, inspector titles |
| `--text-xl` | 1.5 | 1.4 | 600 | Screen titles, KPI numbers |
| `--text-2xl` | 2.0 | 1.3 | 700 | Landing / large KPIs |

Monospace is mandatory for `eventId`, `correlationId`, `traceId`, and `payload` to keep envelope
fields scannable (canon §6).

## 4. Spacing

A 4px base scale (Tailwind-aligned). Components compose these; no arbitrary pixel values.

| Token | Value | Usage |
|-------|-------|-------|
| `--space-1` | 4px | Icon-label gap, chip padding |
| `--space-2` | 8px | Compact control padding |
| `--space-3` | 12px | Form field spacing |
| `--space-4` | 16px | Default panel padding, card gap |
| `--space-6` | 24px | Section separation |
| `--space-8` | 32px | Rail padding, grid gutter |
| `--space-12` | 48px | Screen margins on wide breakpoints |

Radii: `--radius-sm` 4px (chips), `--radius-md` 8px (cards/inputs), `--radius-lg` 12px (panels),
`--radius-full` 9999px (status dots, tokens). Elevation: `--shadow-sm` for cards, `--shadow-md` for
drawers/popovers, `--shadow-lg` for the detail drawer/modal.

## 5. Icons & node iconography

- **Icon set.** [Lucide](https://lucide.dev) (tree-shakeable, consistent 24px grid, stroke-based).
  One family only, sized via `--space` tokens (16/20/24).
- **Node iconography.** Each `FlowNode` variant renders its glyph from the table in §2.2, tinted with
  the node accent. Icons are paired with a text label — never icon-only on the canvas — so meaning is
  not color-dependent (accessibility).
- **Event icons.** EventLog rows and EventInspector use a small glyph per event group (arrow-out for
  publish, arrow-in for receive, loop for retry, skull for dead-letter, bolt for cache, break-circle
  for circuit breaker) reinforcing the color chip.

## 6. Animation principles

Full catalog in [animations.md](./animations.md). Foundational tokens and rules here:

| Token | Value | Usage |
|-------|-------|-------|
| `--motion-fast` | 120ms | State color flips (CB, node status), hover |
| `--motion-base` | 240ms | Node pulse, badge count changes |
| `--motion-travel` | 600ms | Message token traversing an edge (scaled by playback speed) |
| `--motion-slow` | 900ms | DLQ drop, retry loop arc |
| `--ease-standard` | `cubic-bezier(0.4, 0, 0.2, 1)` | Most transitions |
| `--ease-emphasis` | `cubic-bezier(0.2, 0, 0, 1)` | Entrances, token arrival |
| `--ease-exit` | `cubic-bezier(0.4, 0, 1, 1)` | Drops, drop-to-DLQ, dequeue |

Rules: motion is implemented with **Framer Motion**; every animation is triggered by a backend
SimulationEvent and bracketed by frontend-only `AnimationStarted`/`AnimationFinished` (canon §1, §7);
durations scale with the playback speed multiplier from `uiStore`; concurrent animations coalesce per
frame for performance.

## 7. Accessibility (WCAG 2.1 AA)

### 7.1 Color & contrast
- All text meets AA: ≥4.5:1 for normal text, ≥3:1 for large text and UI/graphical indicators. The
  token pairs in §2.1 are chosen to satisfy this on both themes.
- **Never color-only.** Node type is conveyed by icon + label + shape, not just accent. Event severity
  is conveyed by icon + text label in addition to the color chip. Circuit-breaker state adds a text
  label (Closed/Half-open/Open) beside the color.

### 7.2 Keyboard navigation (including the canvas)
- Global: `Tab`/`Shift+Tab` traverse regions in reading order (top bar → palette → canvas → inspector
  → dock); a visible `--color-focus-ring` marks focus.
- **Canvas:** nodes and edges are focusable. Arrow keys move focus between connected nodes; `Enter`
  selects (opens inspector); `Space` toggles selection; `Delete` removes the selected node/edge in
  Design mode; `+`/`-` zoom, arrow-drag pans. React Flow's `nodesFocusable`/`edgesFocusable` are
  enabled and augmented with an `aria` roving-tabindex.
- Transport: `Space` toggles pause/resume when the dock has focus; `[`/`]` step the TimelineScrubber
  by one event in Replay mode.

### 7.3 ARIA & semantics
- Regions use landmark roles (`banner`, `navigation`, `main`, `complementary`, `contentinfo`).
- Canvas nodes expose `role="button"` with `aria-label` composed as "`{type}` node `{label}`,
  `{runtime summary}`". Edges expose `aria-label` "edge from `{source}` to `{target}`".
- The EventLog is an `aria-live="polite"` log region so new events are announced without stealing
  focus; high-frequency streams throttle announcements to avoid flooding assistive tech.
- Inspector tabs use the WAI-ARIA Tabs pattern; forms use associated `<label>`/`aria-describedby` for
  validation messages (mirrors backend FluentValidation).

### 7.4 Reduced motion
- Respect `prefers-reduced-motion`. When set (or when the user toggles it in `uiStore`), token travel
  is replaced by an instant highlight + count update, pulses become a brief static emphasis, and
  looping/retry arcs are suppressed. `AnimationStarted`/`AnimationFinished` still fire so downstream
  logic is unchanged — only the *rendering* degrades gracefully (canon §7: presentation-only).

## Related documents

- [Animations](./animations.md)
- [Components](./components.md)
- [Wireframes](./wireframes.md)
- [Screens & Routes](./screens.md)
- [User Flows](./user-flows.md)
