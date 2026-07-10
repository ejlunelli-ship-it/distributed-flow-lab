import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import { App } from './App'

describe('App shell', () => {
  beforeEach(() => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true } as Response))
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('renders the product title', () => {
    render(<App />)
    expect(
      screen.getByRole('heading', { name: /distributed flow lab/i }),
    ).toBeInTheDocument()
  })

  it('reports API status once health is checked', async () => {
    render(<App />)
    await waitFor(() =>
      expect(screen.getByTestId('api-health')).toHaveTextContent('ok'),
    )
  })
})
