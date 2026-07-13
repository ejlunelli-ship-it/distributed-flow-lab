import { describe, it, expect, beforeEach } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { NodeInspector } from './NodeInspector'
import { useCanvasStore } from '@/state/canvasStore'

function seedNode(type: 'Queue' | 'Broker' = 'Queue') {
  useCanvasStore.getState().reset()
  useCanvasStore.getState().addNode(type, { x: 0, y: 0 })
  return useCanvasStore.getState().nodes[0]!
}

describe('NodeInspector', () => {
  beforeEach(() => {
    useCanvasStore.getState().reset()
  })

  it('renders a config form derived from the node type', () => {
    const node = seedNode('Queue')
    render(<NodeInspector node={node} />)
    expect(screen.getByLabelText('Label')).toBeInTheDocument()
    expect(screen.getByLabelText('Max length')).toBeInTheDocument()
    expect(screen.getByLabelText('Message TTL (ms)')).toBeInTheDocument()
  })

  it('persists a label edit to the store', async () => {
    const node = seedNode('Queue')
    const user = userEvent.setup()
    render(<NodeInspector node={node} />)

    const label = screen.getByLabelText('Label')
    await user.clear(label)
    await user.type(label, 'orders.q')

    expect(useCanvasStore.getState().nodes[0]!.data.label).toBe('orders.q')
  })

  it('shows an inline error and does not persist an out-of-range number', () => {
    const node = seedNode('Queue')
    render(<NodeInspector node={node} />)

    fireEvent.change(screen.getByLabelText('Max length'), {
      target: { value: '-5' },
    })

    expect(screen.getByText('Must be ≥ 0.')).toBeInTheDocument()
    // The invalid value is not written back to the authoritative store.
    expect(useCanvasStore.getState().nodes[0]!.data.config.maxLength).toBe(1000)
  })

  it('persists a valid number edit', () => {
    const node = seedNode('Queue')
    render(<NodeInspector node={node} />)

    fireEvent.change(screen.getByLabelText('Max length'), {
      target: { value: '500' },
    })

    expect(useCanvasStore.getState().nodes[0]!.data.config.maxLength).toBe(500)
  })

  it('notes when a node type has no configurable properties', () => {
    const node = seedNode('Broker')
    render(<NodeInspector node={node} />)
    expect(screen.getByText(/no configurable properties/i)).toBeInTheDocument()
  })
})
