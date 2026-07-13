import { describe, it, expect } from 'vitest'
import { NODE_TYPES } from './nodeType'
import {
  NODE_CATALOG,
  PALETTE_GROUPS,
  defaultConfigFor,
  nodeTypesInGroup,
} from './nodeCatalog'

describe('node catalog', () => {
  it('has an entry for every canonical NodeType, keyed consistently', () => {
    for (const type of NODE_TYPES) {
      expect(NODE_CATALOG[type]).toBeDefined()
      expect(NODE_CATALOG[type].type).toBe(type)
      expect(PALETTE_GROUPS).toContain(NODE_CATALOG[type].group)
    }
  })

  it('builds a default config carrying every field default', () => {
    const config = defaultConfigFor('Queue')
    for (const field of NODE_CATALOG.Queue.configFields) {
      expect(config[field.key]).toEqual(field.defaultValue)
    }
  })

  it('partitions all node types across the palette groups', () => {
    const grouped = PALETTE_GROUPS.flatMap((group) => nodeTypesInGroup(group))
    expect(grouped.slice().sort()).toEqual(NODE_TYPES.slice().sort())
  })

  it('gives select fields a default drawn from their options', () => {
    for (const meta of Object.values(NODE_CATALOG)) {
      for (const field of meta.configFields) {
        if (field.kind === 'select') {
          expect(field.options).toBeDefined()
          expect(field.options).toContain(field.defaultValue)
        }
      }
    }
  })
})
