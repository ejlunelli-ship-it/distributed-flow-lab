import type { NodeType } from './nodeType'

/**
 * Node / Edge configuration model mirrored on the client (.docs/02-architecture/data-model.md
 * §2.2–2.3). A `Node` carries a `type`-specific `config` — a flat bag of scalar values whose
 * shape is described declaratively by the field descriptors in `features/canvas/nodeCatalog.ts`.
 * Keeping `config` a `Record<string, NodeConfigValue>` (rather than a bespoke interface per type)
 * lets the palette, defaults, and the inspector form all be driven from one catalog — new
 * `NodeType`s are added by extending the catalog, never by editing a switch (OCP).
 */
export type NodeConfigValue = string | number

export type NodeConfig = Record<string, NodeConfigValue>

/** Kind of input the inspector renders for a config field. */
export type ConfigFieldKind = 'number' | 'text' | 'select'

/**
 * Declarative descriptor for one configurable field of a `NodeType`. Drives the default
 * `config`, the inspector form control, and client-side validation (which mirrors the backend
 * FluentValidation rules — coding-standards §4.4).
 */
export interface NodeConfigField {
  key: string
  label: string
  kind: ConfigFieldKind
  defaultValue: NodeConfigValue
  /** For `number` fields: inclusive bounds enforced by the inspector. */
  min?: number
  max?: number
  /** For `select` fields: the allowed values. */
  options?: readonly string[]
  /** Short educational hint shown under the control. */
  help?: string
}

/**
 * The `data` payload of a React Flow node. The Zustand `canvasStore` is the authoritative
 * owner of this state (ADR-001); React Flow renders it and never becomes a second source of
 * truth. `type` selects the visual variant and the config schema.
 */
export interface FlowNodeData extends Record<string, unknown> {
  type: NodeType
  label: string
  config: NodeConfig
}
