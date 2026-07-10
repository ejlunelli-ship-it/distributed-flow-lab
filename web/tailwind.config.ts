import type { Config } from 'tailwindcss'

/**
 * Tailwind design tokens for Distributed Flow Lab.
 *
 * This is the scaffolding baseline. The full palette (node colors, event
 * colors, typography and spacing scales) is defined in
 * .docs/03-ui/design-system.md and will be mapped into this theme as the
 * canvas and inspector features are built (Sprint 3+).
 */
const config: Config = {
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  theme: {
    extend: {
      colors: {
        surface: {
          DEFAULT: '#0f172a',
          muted: '#1e293b',
        },
        accent: {
          DEFAULT: '#6366f1',
        },
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', 'sans-serif'],
        mono: ['JetBrains Mono', 'ui-monospace', 'monospace'],
      },
    },
  },
  plugins: [],
}

export default config
