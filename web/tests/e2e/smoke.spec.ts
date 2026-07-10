import { test, expect } from '@playwright/test'

test('app shell loads and shows the product title', async ({ page }) => {
  await page.goto('/')
  await expect(
    page.getByRole('heading', { name: /distributed flow lab/i }),
  ).toBeVisible()
})
