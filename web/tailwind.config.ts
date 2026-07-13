import type { Config } from 'tailwindcss'

/**
 * Tailwind design tokens for Distributed Flow Lab.
 *
 * Semantic colors, per-`NodeType` accents, typography and radii map to the CSS
 * custom properties defined in src/index.css, which are the light/dark source
 * of truth (.docs/03-ui/design-system.md §2). Components consume these semantic
 * names — never raw hex — so re-theming is a single token swap.
 */
const config: Config = {
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  darkMode: ['selector', '[data-theme="dark"]'],
  theme: {
    extend: {
      colors: {
        page: 'var(--color-bg)',
        surface: {
          DEFAULT: 'var(--color-surface)',
          // `surface-muted` / `surface-2` both map to the nested-surface token.
          muted: 'var(--color-surface-2)',
          2: 'var(--color-surface-2)',
        },
        border: 'var(--color-border)',
        fg: {
          DEFAULT: 'var(--color-text)',
          muted: 'var(--color-text-muted)',
        },
        primary: {
          DEFAULT: 'var(--color-primary)',
          contrast: 'var(--color-primary-contrast)',
        },
        // Kept as an alias of primary for pre-Sprint-3 components (dev panel).
        accent: {
          DEFAULT: 'var(--color-primary)',
        },
        success: 'var(--color-success)',
        warning: 'var(--color-warning)',
        danger: 'var(--color-danger)',
        info: 'var(--color-info)',
        focus: 'var(--color-focus-ring)',
        // Per-NodeType accents (design-system.md §2.2).
        node: {
          producer: 'var(--node-producer)',
          consumer: 'var(--node-consumer)',
          service: 'var(--node-service)',
          apigateway: 'var(--node-apigateway)',
          loadbalancer: 'var(--node-loadbalancer)',
          exchange: 'var(--node-exchange)',
          queue: 'var(--node-queue)',
          topic: 'var(--node-topic)',
          partition: 'var(--node-partition)',
          broker: 'var(--node-broker)',
          database: 'var(--node-database)',
          cache: 'var(--node-cache)',
          deadletterqueue: 'var(--node-deadletterqueue)',
          client: 'var(--node-client)',
        },
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', 'sans-serif'],
        mono: ['JetBrains Mono', 'ui-monospace', 'monospace'],
      },
      borderRadius: {
        md: '8px',
        lg: '12px',
      },
    },
  },
  plugins: [],
}

export default config
