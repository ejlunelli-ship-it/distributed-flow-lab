import { describe, it, expect, beforeEach } from 'vitest'
import { useCanvasStore } from './canvasStore'

const store = () => useCanvasStore.getState()

beforeEach(() => {
  useCanvasStore.getState().reset()
})

describe('canvasStore', () => {
  it('adds a node with default config, selects it, and marks dirty', () => {
    store().addNode('Queue', { x: 0, y: 0 })
    const s = store()
    expect(s.nodes).toHaveLength(1)
    expect(s.nodes[0]!.data.type).toBe('Queue')
    expect(s.nodes[0]!.data.config.maxLength).toBe(1000)
    expect(s.selectedNodeId).toBe(s.nodes[0]!.id)
    expect(s.dirty).toBe(true)
  })

  it('connects a legal Producer→Exchange edge', () => {
    store().addNode('Producer', { x: 0, y: 0 })
    store().addNode('Exchange', { x: 200, y: 0 })
    const [producer, exchange] = store().nodes
    const result = store().connect({
      source: producer!.id,
      target: exchange!.id,
      sourceHandle: null,
      targetHandle: null,
    })
    expect(result.ok).toBe(true)
    expect(store().edges).toHaveLength(1)
  })

  it('rejects an illegal Consumer→Producer edge with a reason and adds no edge', () => {
    store().addNode('Consumer', { x: 0, y: 0 })
    store().addNode('Producer', { x: 200, y: 0 })
    const [consumer, producer] = store().nodes
    const result = store().connect({
      source: consumer!.id,
      target: producer!.id,
      sourceHandle: null,
      targetHandle: null,
    })
    expect(result.ok).toBe(false)
    expect(result.reason).toMatch(/cannot connect/)
    expect(store().edges).toHaveLength(0)
  })

  it('rejects self-loops and duplicate edges', () => {
    store().addNode('Producer', { x: 0, y: 0 })
    store().addNode('Exchange', { x: 200, y: 0 })
    const [producer, exchange] = store().nodes
    const conn = {
      source: producer!.id,
      target: exchange!.id,
      sourceHandle: null,
      targetHandle: null,
    }

    expect(store().connect({ ...conn, target: producer!.id }).reason).toMatch(
      /itself/,
    )

    expect(store().connect(conn).ok).toBe(true)
    const dup = store().connect(conn)
    expect(dup.ok).toBe(false)
    expect(dup.reason).toMatch(/already connected/)
    expect(store().edges).toHaveLength(1)
  })

  it('updates node label and config', () => {
    store().addNode('Queue', { x: 0, y: 0 })
    const id = store().nodes[0]!.id
    store().updateNodeLabel(id, 'orders.q')
    store().updateNodeConfig(id, { maxLength: 500 })
    const node = store().nodes[0]!
    expect(node.data.label).toBe('orders.q')
    expect(node.data.config.maxLength).toBe(500)
  })

  it('removes the selected node and its incident edges', () => {
    store().addNode('Producer', { x: 0, y: 0 })
    store().addNode('Exchange', { x: 200, y: 0 })
    const [producer, exchange] = store().nodes
    store().connect({
      source: producer!.id,
      target: exchange!.id,
      sourceHandle: null,
      targetHandle: null,
    })

    store().setSelection({ nodeId: producer!.id })
    store().removeSelected()

    expect(store().nodes).toHaveLength(1)
    expect(store().nodes[0]!.id).toBe(exchange!.id)
    expect(store().edges).toHaveLength(0)
    expect(store().selectedNodeId).toBeNull()
  })
})
