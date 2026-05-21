import type { Config } from "tailwindcss";

/**
 * Shared Tailwind preset for the Hub operator portal.
 *
 * Hub keeps a smaller token surface than LearnStack core's tenant-facing
 * frontend because the operator portal is a single-brand internal tool —
 * no tenant theming. Tokens are intentionally muted (slate-leaning) to
 * mirror the operator-tool aesthetic.
 */
export const learnstackHubTailwindPreset = {
  theme: {
    extend: {
      colors: {
        "hub-primary": "var(--hub-primary, #3b82f6)",
        "hub-bg": "var(--hub-bg, #f8fafc)",
        "hub-surface": "var(--hub-surface, #ffffff)",
        "hub-fg": "var(--hub-fg, #0f172a)",
        "hub-muted": "var(--hub-muted, #64748b)",
        "hub-warning": "var(--hub-warning, #f59e0b)",
        "hub-danger": "var(--hub-danger, #ef4444)",
      },
      fontFamily: {
        sans: [
          "var(--hub-font-sans, ui-sans-serif)",
          "system-ui",
          "sans-serif",
        ],
      },
    },
  },
} satisfies Partial<Config>;
