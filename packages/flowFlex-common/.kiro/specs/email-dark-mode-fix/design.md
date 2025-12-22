# Design Document

## Overview

This design document addresses the issue where email content displays incorrectly in dark mode. The problem occurs because email HTML (designed for light backgrounds) is not properly adapted when the application switches to dark mode, resulting in poor contrast and readability issues.

The solution involves enhancing the existing email dark mode utility (`src/app/utils/emailDarkMode.ts`) and ensuring it's correctly integrated into the iframe rendering process in the MessageDetailPanel component. The utility will intelligently transform colors while preserving brand colors, images, and the overall email structure.

## Architecture

### Current State

The application currently has:
1. An `applyEmailDarkMode` utility function in `src/app/utils/emailDarkMode.ts`
2. A MessageDetailPanel component that renders emails in an iframe
3. A theme system that manages light/dark mode switching

### Problem Analysis

From the provided screenshots, the issue is that in dark mode:
- Email backgrounds remain light (white/light gray)
- Text colors remain dark, making them hard to read on the application's dark background
- The overall email content doesn't adapt to the dark theme

### Solution Architecture

```
Email HTML → applyEmailDarkMode(html, isDark) → Transformed HTML → Iframe Rendering
                      ↑
                  Theme State
```

The solution ensures that:
1. The `applyEmailDarkMode` function is called with the correct theme state
2. The function properly transforms all color-related attributes and styles
3. The iframe regenerates when the theme changes
4. Brand colors and images are preserved

## Components and Interfaces

### Enhanced Email Dark Mode Utility

**Location**: `src/app/utils/emailDarkMode.ts`

**Key Functions**:

1. `applyEmailDarkMode(htmlContent: string, isDark: boolean): string`
   - Main entry point for email transformation
   - Returns transformed HTML for dark mode or original HTML for light mode

2. `processElementColors(element: HTMLElement, isDark: boolean): void`
   - Processes individual element's color attributes
   - Handles: backgroundColor, background, color, borderColor, bgcolor attribute, font color attribute

3. `mapColorToDark(color: string): string | null`
   - Maps a single color to its dark mode equivalent
   - Returns null if color should be preserved (brand colors)
   - Handles white→dark, black→light, grayscale inversion, extreme colors

4. `shouldProcessColor(rgb: RGB): boolean`
   - Determines if a color needs transformation
   - Preserves brand colors (high saturation)
   - Processes extreme colors and grayscale

### MessageDetailPanel Integration

**Location**: `src/app/views/messageCenter/components/MessageDetailPanel.vue`

**Current Implementation**:
```vue
<div v-html="applyEmailDarkMode(message.body, theme.theme == 'dark')"></div>
```

**Required Changes**:
The component needs to ensure that:
1. The iframe rendering uses the transformed HTML
2. The iframe regenerates when theme changes
3. The transformation is applied before iframe injection

## Data Models

### Color Transformation Rules

```typescript
interface ColorTransformationRule {
  condition: (rgb: RGB) => boolean;
  transform: (rgb: RGB) => RGB | null;
  description: string;
}

const transformationRules: ColorTransformationRule[] = [
  {
    condition: isWhitish,  // RGB > (240, 240, 240)
    transform: () => ({ r: 30, g: 30, b: 30 }),  // #1e1e1e
    description: "Pure white backgrounds → dark background"
  },
  {
    condition: isBlackish,  // RGB < (40, 40, 40)
    transform: () => ({ r: 224, g: 224, b: 224 }),  // #e0e0e0
    description: "Pure black text → light text"
  },
  {
    condition: isBrandColor,  // Saturation > 0.5, Lightness 0.25-0.8
    transform: () => null,  // Preserve
    description: "Brand colors → preserved"
  },
  {
    condition: isGrayscale,  // R≈G≈B (diff < 30)
    transform: invertLightness,
    description: "Grayscale → inverted lightness"
  },
  {
    condition: (rgb) => rgbToHsl(rgb).l > 0.75,
    transform: (rgb) => {
      const hsl = rgbToHsl(rgb);
      hsl.l = 0.2 + (1 - hsl.l) * 0.5;  // Map to 0.2-0.325
      return hslToRgb(hsl);
    },
    description: "Very light colors → dark colors"
  },
  {
    condition: (rgb) => rgbToHsl(rgb).l < 0.35,
    transform: (rgb) => {
      const hsl = rgbToHsl(rgb);
      hsl.l = 0.75 + hsl.l * 0.3;  // Map to 0.75-0.855
      return hslToRgb(hsl);
    },
    description: "Very dark colors → light colors"
  }
];
```

### HTML Attributes to Process

```typescript
interface HTMLColorAttributes {
  styleProperties: string[];  // ['backgroundColor', 'background', 'color', 'borderColor']
  htmlAttributes: string[];   // ['bgcolor']
  tagSpecific: Map<string, string[]>;  // { 'FONT': ['color'] }
}
```

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system—essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Light mode identity

*For any* email HTML content, when `applyEmailDarkMode` is called with `isDark = false`, the output SHALL be identical to the input.

**Validates: Requirements 1.5, 4.5**

### Property 2: Dark background transformation

*For any* email HTML containing elements with white or very light backgrounds (lightness > 0.9), when transformed for dark mode, those backgrounds SHALL have lightness < 0.3.

**Validates: Requirements 1.1, 1.3**

### Property 3: Text color transformation

*For any* email HTML containing text with dark colors (lightness < 0.35), when transformed for dark mode, those text colors SHALL have lightness > 0.7.

**Validates: Requirements 1.2, 1.4**

### Property 4: Brand color preservation

*For any* email HTML containing colors with saturation > 0.5 and lightness between 0.25 and 0.8, when transformed for dark mode, those colors SHALL remain unchanged.

**Validates: Requirements 2.1**

### Property 5: Image preservation

*For any* email HTML containing `<img>` tags, when transformed for dark mode, the `<img>` tags and their attributes SHALL remain unchanged.

**Validates: Requirements 2.3**

### Property 6: Grayscale inversion

*For any* email HTML containing grayscale colors (where |R-G| < 30, |G-B| < 30, |B-R| < 30), when transformed for dark mode, the lightness SHALL be inverted (new_lightness ≈ 1 - old_lightness).

**Validates: Requirements 2.5**

### Property 7: Structure preservation

*For any* email HTML, when transformed for dark mode, the DOM structure (tag names, nesting hierarchy, text content, non-color attributes) SHALL remain identical to the input.

**Validates: Requirements 4.4**

### Property 8: Comprehensive attribute handling

*For any* email HTML containing color specifications in inline styles, style attributes, bgcolor attributes, or font color attributes, when transformed for dark mode, all color specifications SHALL be processed according to the transformation rules.

**Validates: Requirements 4.3, 5.1, 5.3**

### Property 9: Nested element processing

*For any* email HTML with nested elements containing color styles at multiple depth levels, when transformed for dark mode, colors at all nesting levels SHALL be processed.

**Validates: Requirements 5.2**

### Property 10: Color format handling

*For any* email HTML containing colors in different formats (hex, rgb, rgba, named colors), when transformed for dark mode, all formats SHALL be correctly parsed and transformed.

**Validates: Requirements 5.4**

### Property 11: Transparency preservation

*For any* email HTML containing colors with alpha transparency (rgba), when transformed for dark mode, the alpha channel SHALL remain unchanged while RGB values are transformed.

**Validates: Requirements 5.5**

## Error Handling

### Invalid Color Format

**Scenario**: Email contains invalid or malformed color values

**Handling**:
- `parseColor` returns null for unparseable colors
- `mapColorToDark` returns null when input is null
- Original color value is preserved in the output
- No errors thrown, graceful degradation

```typescript
function parseColor(color: string): RGB | null {
  // ... parsing logic
  return null;  // If unparseable
}

function mapColorToDark(color: string): string | null {
  const rgb = parseColor(color);
  if (!rgb) return null;  // Preserve original
  // ... transformation logic
}
```

### Malformed HTML

**Scenario**: Email HTML is malformed or contains invalid structure

**Handling**:
- DOMParser handles malformed HTML gracefully
- Browser's HTML parser auto-corrects common issues
- Transformation proceeds with parsed structure
- No errors thrown

```typescript
const parser = new DOMParser();
const doc = parser.parseFromString(htmlContent, 'text/html');
// DOMParser handles malformed HTML
```

### Missing Theme Parameter

**Scenario**: `isDark` parameter is undefined or invalid

**Handling**:
- Function treats falsy values as light mode
- Returns original HTML without transformation
- No errors thrown

```typescript
export function applyEmailDarkMode(htmlContent: string, isDark: boolean): string {
  if (!isDark) return htmlContent;  // Handles undefined, false, null
  // ... transformation logic
}
```

### Null or Empty HTML Content

**Scenario**: Email body is null, undefined, or empty string

**Handling**:
- Function returns empty string for null/undefined
- Empty string passes through unchanged
- No errors thrown

```typescript
export function applyEmailDarkMode(htmlContent: string, isDark: boolean): string {
  if (!htmlContent) return htmlContent || '';
  // ... transformation logic
}
```

### DOM Access Errors

**Scenario**: Cannot access or modify DOM elements during transformation

**Handling**:
- Try-catch around DOM operations (if needed)
- Skip problematic elements
- Continue processing remaining elements
- Log warnings in development mode

## Testing Strategy

### Unit Testing

**Framework**: Jest

**Test Cases**:

1. **Light Mode Identity**
   - Input: Various email HTML samples
   - Call: `applyEmailDarkMode(html, false)`
   - Verify: Output === Input

2. **Color Transformation**
   - Test white backgrounds → dark backgrounds
   - Test black text → light text
   - Test grayscale inversion
   - Test brand color preservation

3. **Attribute Handling**
   - Test inline style transformation
   - Test bgcolor attribute transformation
   - Test font color attribute transformation
   - Test nested element transformation

4. **Edge Cases**
   - Test empty HTML
   - Test null/undefined input
   - Test invalid color formats
   - Test malformed HTML

5. **Structure Preservation**
   - Test that tag names remain unchanged
   - Test that text content remains unchanged
   - Test that non-color attributes remain unchanged
   - Test that nesting structure remains unchanged

### Property-Based Testing

**Framework**: fast-check (JavaScript property-based testing library)

**Configuration**: Minimum 100 iterations per property test

**Property Tests**:

1. **Property 1: Light Mode Identity**
   - Generate: Random HTML strings with various color formats
   - Action: Apply dark mode transformation with isDark=false
   - Verify: Output === Input
   - Tag: **Feature: email-dark-mode-fix, Property 1: Light mode identity**

2. **Property 2: Dark Background Transformation**
   - Generate: HTML with elements having light backgrounds (lightness > 0.9)
   - Action: Apply dark mode transformation
   - Verify: All backgrounds have lightness < 0.3
   - Tag: **Feature: email-dark-mode-fix, Property 2: Dark background transformation**

3. **Property 3: Text Color Transformation**
   - Generate: HTML with dark text colors (lightness < 0.35)
   - Action: Apply dark mode transformation
   - Verify: All text colors have lightness > 0.7
   - Tag: **Feature: email-dark-mode-fix, Property 3: Text color transformation**

4. **Property 4: Brand Color Preservation**
   - Generate: HTML with high-saturation colors (s > 0.5, l ∈ [0.25, 0.8])
   - Action: Apply dark mode transformation
   - Verify: Brand colors remain unchanged
   - Tag: **Feature: email-dark-mode-fix, Property 4: Brand color preservation**

5. **Property 5: Image Preservation**
   - Generate: HTML with img tags
   - Action: Apply dark mode transformation
   - Verify: img tags and attributes unchanged
   - Tag: **Feature: email-dark-mode-fix, Property 5: Image preservation**

6. **Property 6: Grayscale Inversion**
   - Generate: HTML with grayscale colors (R≈G≈B)
   - Action: Apply dark mode transformation
   - Verify: Lightness is inverted (new_l ≈ 1 - old_l)
   - Tag: **Feature: email-dark-mode-fix, Property 6: Grayscale inversion**

7. **Property 7: Structure Preservation**
   - Generate: Random HTML structures
   - Action: Apply dark mode transformation
   - Verify: DOM structure (tags, nesting, text, non-color attrs) unchanged
   - Tag: **Feature: email-dark-mode-fix, Property 7: Structure preservation**

8. **Property 8: Comprehensive Attribute Handling**
   - Generate: HTML with colors in various attributes (style, bgcolor, font color)
   - Action: Apply dark mode transformation
   - Verify: All color attributes are processed
   - Tag: **Feature: email-dark-mode-fix, Property 8: Comprehensive attribute handling**

9. **Property 9: Nested Element Processing**
   - Generate: Deeply nested HTML with colors at multiple levels
   - Action: Apply dark mode transformation
   - Verify: Colors at all nesting levels are processed
   - Tag: **Feature: email-dark-mode-fix, Property 9: Nested element processing**

10. **Property 10: Color Format Handling**
    - Generate: HTML with colors in different formats (hex, rgb, rgba, named)
    - Action: Apply dark mode transformation
    - Verify: All formats are correctly parsed and transformed
    - Tag: **Feature: email-dark-mode-fix, Property 10: Color format handling**

11. **Property 11: Transparency Preservation**
    - Generate: HTML with rgba colors
    - Action: Apply dark mode transformation
    - Verify: Alpha channel unchanged, RGB transformed
    - Tag: **Feature: email-dark-mode-fix, Property 11: Transparency preservation**

### Integration Testing

**Test Scenarios**:

1. **MessageDetailPanel Dark Mode Rendering**
   - Open email in dark mode
   - Verify email displays with dark background and light text
   - Verify brand colors are preserved

2. **Theme Switching**
   - Open email in light mode
   - Switch to dark mode
   - Verify email updates to dark mode styling
   - Switch back to light mode
   - Verify email returns to original styling

3. **Various Email Types**
   - Test plain text emails
   - Test HTML emails with tables
   - Test branded marketing emails
   - Test emails with images
   - Test emails with complex layouts

### Manual Testing Checklist

- [ ] Email displays correctly in dark mode (dark background, light text)
- [ ] Email displays correctly in light mode (original colors)
- [ ] Theme switching updates email immediately
- [ ] Brand colors (buttons, logos) remain recognizable in dark mode
- [ ] Images display correctly without color inversion
- [ ] Links are readable in both modes
- [ ] Table-based layouts display correctly
- [ ] Nested elements display correctly
- [ ] No console errors during transformation
- [ ] No visual flashing during theme switch

## Implementation Details

### Current Code Issues

Based on the existing `emailDarkMode.ts` file, the implementation appears comprehensive but may have integration issues. The key areas to verify:

1. **Integration Point**: Ensure the utility is called correctly in MessageDetailPanel
2. **Theme Detection**: Verify the theme state is correctly passed to the utility
3. **Iframe Regeneration**: Ensure the iframe regenerates when theme changes

### Required Changes

#### 1. Verify MessageDetailPanel Integration

The component currently has:
```vue
<div v-html="applyEmailDarkMode(message.body, theme.theme == 'dark')"></div>
```

However, based on the message-detail-panel spec, it should be using iframe rendering. We need to verify:
- Is the iframe implementation complete?
- Is `applyEmailDarkMode` being called before iframe injection?
- Does the iframe regenerate on theme changes?

#### 2. Fix Deprecated substr Usage

The current code uses deprecated `substr` method:
```typescript
// Current (deprecated)
hex.substr(0, 2)

// Should be
hex.substring(0, 2)
```

#### 3. Enhance Color Parsing

Add support for more color formats if needed:
- hsl/hsla colors
- CSS color keywords (more than the basic set)
- Percentage-based rgb values

#### 4. Add Logging for Debugging

Add optional debug logging to help diagnose transformation issues:
```typescript
const DEBUG = false;  // Enable for debugging

function mapColorToDark(color: string): string | null {
  const rgb = parseColor(color);
  if (!rgb) {
    if (DEBUG) console.log('Could not parse color:', color);
    return null;
  }
  
  if (!shouldProcessColor(rgb)) {
    if (DEBUG) console.log('Preserving color:', color, rgb);
    return null;
  }
  
  // ... transformation logic
  
  if (DEBUG) console.log('Transformed:', color, '→', result);
  return result;
}
```

### Integration with Iframe Rendering

The MessageDetailPanel should integrate the dark mode utility as follows:

```typescript
const renderEmailInIframe = (htmlContent: string, currentTheme: string) => {
  if (!emailIframeRef.value) return;
  
  const iframe = emailIframeRef.value;
  const doc = iframe.contentDocument;
  
  if (!doc) return;
  
  // Apply dark mode transformation to email HTML
  const isDark = currentTheme === 'dark';
  const transformedHtml = applyEmailDarkMode(htmlContent, isDark);
  
  // Define base theme colors for iframe
  const colors = {
    dark: {
      background: '#1e1e1e',
      text: '#ddd',
      link: '#6ea8fe'
    },
    light: {
      background: '#fff',
      text: '#000',
      link: '#0d6efd'
    }
  };
  
  const themeColors = colors[currentTheme] || colors.dark;
  
  // Generate complete HTML document with transformed content
  const fullHtml = `
    <html>
      <head>
        <style>
          body {
            background: ${themeColors.background};
            color: ${themeColors.text};
            margin: 0;
            padding: 16px;
            font-family: inherit;
          }
          a {
            color: ${themeColors.link};
          }
        </style>
      </head>
      <body>
        ${transformedHtml}
      </body>
    </html>
  `;
  
  // Write to iframe
  doc.open();
  doc.write(fullHtml);
  doc.close();
};
```

## Performance Considerations

### Transformation Performance

**Concern**: DOM parsing and traversal could be slow for large emails

**Mitigation**:
- DOMParser is highly optimized by browsers
- Transformation is done once per email/theme change
- Typical email sizes (< 100KB) process in < 50ms

**Measurement**:
```typescript
const start = performance.now();
const result = applyEmailDarkMode(html, true);
const duration = performance.now() - start;
console.log(`Transformation took ${duration}ms`);
```

### Memory Usage

**Concern**: Creating temporary DOM structures could consume memory

**Mitigation**:
- Temporary DOM is garbage collected after transformation
- No persistent references to parsed documents
- Single transformation per email display

### Caching Considerations

**Future Enhancement**: Cache transformed HTML to avoid re-transformation

```typescript
const transformCache = new Map<string, string>();

function applyEmailDarkModeWithCache(htmlContent: string, isDark: boolean): string {
  const cacheKey = `${isDark ? 'dark' : 'light'}:${hashCode(htmlContent)}`;
  
  if (transformCache.has(cacheKey)) {
    return transformCache.get(cacheKey)!;
  }
  
  const result = applyEmailDarkMode(htmlContent, isDark);
  transformCache.set(cacheKey, result);
  
  // Limit cache size
  if (transformCache.size > 50) {
    const firstKey = transformCache.keys().next().value;
    transformCache.delete(firstKey);
  }
  
  return result;
}
```

## Security Considerations

### XSS Prevention

**Risk**: Malicious HTML in email content

**Mitigation**:
- Iframe provides isolation boundary
- Sandbox attribute restricts capabilities
- Backend should sanitize email content (out of scope)
- Transformation only modifies colors, not structure

### Color Injection Attacks

**Risk**: Malicious color values designed to break transformation

**Handling**:
- Color parsing returns null for invalid values
- Invalid colors are preserved as-is
- No code execution possible through color values
- Regex-based parsing prevents injection

### DOM Manipulation Safety

**Risk**: Transformation could introduce vulnerabilities

**Mitigation**:
- Only color attributes are modified
- No new elements or scripts are added
- Structure and content remain unchanged
- DOMParser handles malformed HTML safely

## Future Enhancements

### 1. User Preferences

Allow users to customize dark mode transformation:
- Adjust transformation intensity
- Preserve/transform specific color ranges
- Custom brand color definitions

### 2. Automatic Brand Color Detection

Enhance brand color detection:
- Analyze email to identify brand colors
- Learn from user feedback
- Preserve logo colors automatically

### 3. Image Handling

Add optional image processing:
- Detect images with light backgrounds
- Apply subtle darkening filter
- Preserve photo content

### 4. Performance Optimization

- Implement transformation caching
- Use Web Workers for large emails
- Lazy transform off-screen content

### 5. Accessibility Improvements

- Ensure WCAG contrast ratios
- Add high-contrast mode option
- Support for color blindness modes

These enhancements are not part of the current scope but may be considered for future iterations.
