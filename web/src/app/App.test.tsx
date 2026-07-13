import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { AppRoutes } from './AppRoutes'

describe('AppRoutes', () => {
  beforeEach(() => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true } as Response))
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('serves the Sprint 2 dev harness at /dev and reports API health', async () => {
    render(
      <MemoryRouter initialEntries={['/dev']}>
        <AppRoutes />
      </MemoryRouter>,
    )
    expect(
      screen.getByRole('heading', { name: /distributed flow lab/i }),
    ).toBeInTheDocument()
    await waitFor(() =>
      expect(screen.getByTestId('api-health')).toHaveTextContent('ok'),
    )
  })

  it('shows a NotFound screen for unknown routes', () => {
    render(
      <MemoryRouter initialEntries={['/does-not-exist']}>
        <AppRoutes />
      </MemoryRouter>,
    )
    expect(
      screen.getByRole('heading', { name: /page not found/i }),
    ).toBeInTheDocument()
  })
})
