import type { NodeType } from '@/domain'

/**
 * The CSS custom property holding a `NodeType`'s accent color (design-system.md §2.2), e.g.
 * `Producer → var(--node-producer)`. Accents are theme-aware tokens defined in index.css, so
 * components tint by variable and never hardcode hex. Derived mechanically from the type name,
 * which keeps the mapping in one place (the token file) rather than duplicated per component.
 */
export function nodeAccentVar(type: NodeType): string {
  return `var(--node-${type.toLowerCase()})`
}

/** The transferable MIME key carrying a `NodeType` during a palette drag-and-drop. */
export const NODE_DND_MIME = 'application/dfl-node-type'
