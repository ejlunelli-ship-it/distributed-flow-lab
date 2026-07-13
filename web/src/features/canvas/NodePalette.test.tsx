import { describe, it, expect, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { NodePalette } from './NodePalette'
import { useCanvasStore } from '@/state/canvasStore'
import { useUiStore } from '@/state/uiStore'

beforeEach(() => {
  useCanvasStore.getState().reset()
  useUiStore.setState({ paletteCollapsed: false })
})

describe('NodePalette', () => {
  it('renders every palette group heading', () => {
    render(<NodePalette />)
    for (const group of ['Compute', 'Networking', 'Messaging', 'Data']) {
      expect(screen.getByRole('heading', { name: group })).toBeInTheDocument()
    }
  })

  it('adds a node to the canvas store when an item is activated', async () => {
    const user = userEvent.setup()
    render(<NodePalette />)
    await user.click(screen.getByRole('button', { name: 'Add Producer node' }))

    const nodes = useCanvasStore.getState().nodes
    expect(nodes).toHaveLength(1)
    expect(nodes[0]!.data.type).toBe('Producer')
  })

  it('hides labels but keeps items actionable when collapsed', () => {
    useUiStore.setState({ paletteCollapsed: true })
    render(<NodePalette />)
    // Group headings are hidden when collapsed…
    expect(screen.queryByRole('heading', { name: 'Compute' })).toBeNull()
    // …but each node remains reachable via its accessible name.
    expect(
      screen.getByRole('button', { name: 'Add Queue node' }),
    ).toBeInTheDocument()
  })
})
