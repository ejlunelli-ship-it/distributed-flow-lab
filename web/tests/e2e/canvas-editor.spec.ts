import { test, expect, type Page } from '@playwright/test'

/**
 * Sprint 3 exit criterion (execution-roadmap S3): a user composes a
 * `Producer → Exchange → Queue → Consumer` pipeline on the canvas, and an illegal edge is
 * rejected inline. This drives the real editor (no backend needed — the canvas is pure frontend).
 */

/** Drags from a node's source handle to another node's target handle to create an edge. */
async function connect(page: Page, sourceId: string, targetId: string) {
  const from = page.locator(
    `[data-id="${sourceId}"] .react-flow__handle.source`,
  )
  const to = page.locator(`[data-id="${targetId}"] .react-flow__handle.target`)
  const a = await from.boundingBox()
  const b = await to.boundingBox()
  if (!a || !b) throw new Error('handle not found')
  const ax = a.x + a.width / 2
  const ay = a.y + a.height / 2
  const bx = b.x + b.width / 2
  const by = b.y + b.height / 2

  await page.mouse.move(ax, ay)
  await page.mouse.down()
  await page.mouse.move((ax + bx) / 2, (ay + by) / 2, { steps: 6 })
  await page.mouse.move(bx, by, { steps: 6 })
  await page.mouse.move(bx, by)
  await page.mouse.up()
  await page.waitForTimeout(150)
}

test('composes Producer→Exchange→Queue→Consumer and rejects an illegal edge', async ({
  page,
}) => {
  await page.goto('/editor/new')

  // Add the four nodes from the palette (click-to-place, left→right).
  for (const type of ['Producer', 'Exchange', 'Queue', 'Consumer']) {
    await page.getByRole('button', { name: `Add ${type} node` }).click()
  }
  await expect(page.locator('.react-flow__node')).toHaveCount(4)

  // Frame all four nodes so every handle is on-screen and hittable regardless of zoom.
  await page.getByRole('button', { name: 'Fit View' }).click()
  await page.waitForTimeout(300)

  // Legal chain: three edges.
  await connect(page, 'node-producer-1', 'node-exchange-2')
  await connect(page, 'node-exchange-2', 'node-queue-3')
  await connect(page, 'node-queue-3', 'node-consumer-4')
  await expect(page.locator('.react-flow__edge')).toHaveCount(3)

  // Illegal edge: Consumer→Producer is rejected inline and adds no edge.
  await connect(page, 'node-consumer-4', 'node-producer-1')
  await expect(page.getByRole('alert')).toContainText(/cannot connect/i)
  await expect(page.locator('.react-flow__edge')).toHaveCount(3)
})
