---
name: design-token-spec
description: Generate a design token specification (colors, typography, spacing, radius, shadows) based on product type and brand style description. Use when UX needs to define a consistent visual language before Dev implements the UI. Integrates with ui-ux-pro-max for data-driven recommendations.
inclusion: manual
---

# Design Token Spec

Generate a complete design token specification covering colors, typography, spacing, border radius, and shadows — based on product type and style description.

## When to Use

- UX has confirmed the visual style direction and needs to formalize design tokens
- Dev needs CSS variables or design system config to implement the UI
- Establishing a consistent visual language across all pages

## Integration with ui-ux-pro-max

For data-driven token recommendations, run the design system search first:

```bash
python3 steering/shared-skills/ui-ux-pro-max/scripts/search.py "<product_type> <style_keywords>" --design-system -p "Project Name"
```

Then use the output to populate the tokens below.

## Input

```
productType: "SaaS dashboard"
styleKeywords: "professional, clean, minimal, dark mode support"
brandColors: "#3B82F6 (primary)"  # optional
```

## Output Format

```markdown
## Design Tokens: {Project Name}

### Colors
| Token | Light Value | Dark Value | Usage |
|-------|-------------|------------|-------|
| `--color-primary` | `#3B82F6` | `#60A5FA` | Buttons, links, active states |
| `--color-primary-hover` | `#2563EB` | `#3B82F6` | Button hover |
| `--color-danger` | `#EF4444` | `#F87171` | Errors, destructive actions |
| `--color-success` | `#22C55E` | `#4ADE80` | Success states |
| `--color-warning` | `#F59E0B` | `#FBBF24` | Warnings, cautions |
| `--color-text-primary` | `#111827` | `#F9FAFB` | Body text |
| `--color-text-muted` | `#6B7280` | `#9CA3AF` | Secondary text, placeholders |
| `--color-bg-page` | `#F9FAFB` | `#111827` | Page background |
| `--color-bg-surface` | `#FFFFFF` | `#1F2937` | Cards, panels |
| `--color-border` | `#E5E7EB` | `#374151` | Borders, dividers |

### Typography
| Token | Value | Usage |
|-------|-------|-------|
| `--font-family-base` | `'Inter', sans-serif` | Body text |
| `--font-family-heading` | `'Inter', sans-serif` | Headings |
| `--font-size-xs` | `12px` | Labels, captions |
| `--font-size-sm` | `14px` | Secondary text |
| `--font-size-base` | `16px` | Body text |
| `--font-size-lg` | `18px` | Large body, subheadings |
| `--font-size-xl` | `24px` | Section headings |
| `--font-size-2xl` | `32px` | Page titles |
| `--font-weight-normal` | `400` | Body text |
| `--font-weight-medium` | `500` | Emphasis |
| `--font-weight-bold` | `600` | Headings, buttons |
| `--line-height-tight` | `1.25` | Headings |
| `--line-height-base` | `1.5` | Body text |

### Spacing
| Token | Value | Usage |
|-------|-------|-------|
| `--spacing-1` | `4px` | Tight gaps (icon + label) |
| `--spacing-2` | `8px` | Compact padding |
| `--spacing-3` | `12px` | Small component padding |
| `--spacing-4` | `16px` | Standard padding |
| `--spacing-6` | `24px` | Section gaps |
| `--spacing-8` | `32px` | Large section gaps |
| `--spacing-12` | `48px` | Page section spacing |
| `--spacing-16` | `64px` | Hero / major section spacing |

### Border Radius
| Token | Value | Usage |
|-------|-------|-------|
| `--radius-sm` | `4px` | Inputs, tags, badges |
| `--radius-md` | `8px` | Cards, dropdowns |
| `--radius-lg` | `12px` | Modals, large panels |
| `--radius-full` | `9999px` | Pills, avatars |

### Shadows
| Token | Value | Usage |
|-------|-------|-------|
| `--shadow-sm` | `0 1px 2px rgba(0,0,0,0.05)` | Subtle elevation |
| `--shadow-md` | `0 4px 6px rgba(0,0,0,0.07)` | Cards, dropdowns |
| `--shadow-lg` | `0 10px 15px rgba(0,0,0,0.1)` | Modals, popovers |
```

## Light/Dark Mode Rules

- Always define both light and dark values for color tokens
- Light mode: minimum contrast ratio 4.5:1 for text on background
- Never use `bg-white/10` opacity for cards in light mode — use `bg-white/80` or higher
- Border tokens must be visible in both modes

## Execution Rules

1. Always output all five token categories: colors, typography, spacing, radius, shadows
2. Every color token must have both light and dark values
3. Token names use CSS custom property format: `--category-name`
4. Include usage notes for every token — no undocumented tokens
5. If `ui-ux-pro-max` data is available, use it to inform color palette and font choices
