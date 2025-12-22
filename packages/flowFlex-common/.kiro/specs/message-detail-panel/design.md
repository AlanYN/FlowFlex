# Design Document

## Overview

This design document describes the implementation of iframe-based HTML email rendering with theme-aware styling for the MessageDetailPanel component. The solution replaces the current `v-html` approach with an iframe-based renderer that provides better isolation and automatic theme adaptation.

The implementation leverages Vue 3's reactivity system to watch for changes in both the email content and the application theme, automatically regenerating the iframe document with appropriate styles when either changes.

## Architecture

### Component Structure

The MessageDetailPanel component will be enhanced with:

1. **Template Layer**: An iframe element replacing the current v-html div
2. **Script Layer**: 
   - A ref to access the iframe DOM element
   - A computed property or watch to react to theme changes
   - A method to generate and inject themed HTML into the iframe
3. **Integration Layer**: Import and use the theme utility from `src/app/utils/theme.ts`

### Data Flow

```
Theme State (theme.ts) ──┐
                         ├──> Watch Trigger ──> Generate HTML ──> Write to Iframe
Email Body (props) ──────┘
```

When either the theme or email body changes, the watch triggers regeneration of the iframe document with updated styles.

## Components and Interfaces

### Modified Component: MessageDetailPanel.vue

**New Template Elements:**
```vue
<iframe 
  ref="emailIframeRef"
  class="w-full min-h-[400px] border-0"
  sandbox="allow-same-origin"
/>
```

**New Script Additions:**
```typescript
import { ref, watch } from 'vue';
import { useTheme } from '@/utils/theme';

const emailIframeRef = ref<HTMLIFrameElement | null>(null);
const theme = useTheme();
```

**Key Methods:**

1. `renderEmailInIframe(htmlContent: string, currentTheme: string): void`
   - Purpose: Generate and inject themed HTML into iframe
   - Parameters:
     - `htmlContent`: The email body HTML from the message object
     - `currentTheme`: Current theme value ('dark' or 'light')
   - Returns: void
   - Side effects: Writes complete HTML document to iframe

### Theme Utility Integration

**Import:**
```typescript
import { useTheme } from '@/utils/theme';
```

**Usage:**
```typescript
const theme = useTheme();
// Access current theme: theme.theme (readonly reactive property)
```

## Data Models

### Theme Colors Configuration

```typescript
interface ThemeColors {
  dark: {
    background: string;  // '#1e1e1e'
    text: string;        // '#ddd'
    link: string;        // '#6ea8fe'
  };
  light: {
    background: string;  // '#fff'
    text: string;        // '#000'
    link: string;        // '#0d6efd'
  };
}
```

### Iframe Document Structure

```html
<html>
  <head>
    <style>
      body {
        background: ${backgroundColor};
        color: ${textColor};
        margin: 0;
        padding: 16px;
        font-family: inherit;
      }
      a {
        color: ${linkColor};
      }
    </style>
  </head>
  <body>
    ${emailBodyHtml}
  </body>
</html>
```

## Implementation Details

### 1. Iframe Reference Setup

Create a template ref to access the iframe element:

```typescript
const emailIframeRef = ref<HTMLIFrameElement | null>(null);
```

### 2. Theme State Access

Import and use the theme utility:

```typescript
import { useTheme } from '@/utils/theme';

const theme = useTheme();
```

### 3. Reactive Email Rendering

Implement a watch that triggers on theme or email body changes:

```typescript
watch(
  [() => message.value?.body, () => theme.theme],
  ([emailBody, currentTheme]) => {
    if (emailBody && emailIframeRef.value) {
      renderEmailInIframe(emailBody, currentTheme);
    }
  },
  { immediate: true }
);
```

### 4. HTML Generation and Injection

Implement the rendering method:

```typescript
const renderEmailInIframe = (htmlContent: string, currentTheme: string) => {
  if (!emailIframeRef.value) return;
  
  const iframe = emailIframeRef.value;
  const doc = iframe.contentDocument;
  
  if (!doc) return;
  
  // Define theme-specific colors
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
  
  // Generate complete HTML document
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
        ${htmlContent}
      </body>
    </html>
  `;
  
  // Write to iframe
  doc.open();
  doc.write(fullHtml);
  doc.close();
};
```

### 5. Template Modification

Replace the v-html div with iframe:

**Before:**
```vue
<div v-html="message.body" class="whitespace-pre-wrap"></div>
```

**After:**
```vue
<iframe 
  ref="emailIframeRef"
  class="w-full min-h-[400px] border-0"
  sandbox="allow-same-origin"
/>
```

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system—essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Theme color application consistency

*For any* theme value ('dark' or 'light') and any email body HTML content, when the iframe document is generated, the background color, text color, and link color in the injected styles SHALL match the predefined colors for that theme.

**Validates: Requirements 1.1, 1.2, 1.4**

### Property 2: Iframe document regeneration on theme change

*For any* email body content currently displayed, when the theme changes from dark to light or light to dark, the iframe document SHALL be regenerated with the new theme's color values.

**Validates: Requirements 1.3, 3.2**

### Property 3: Iframe document regeneration on content change

*For any* theme setting, when the email body content changes to a different HTML string, the iframe document SHALL be regenerated with the current theme's color values and the new content.

**Validates: Requirements 3.3**

### Property 4: Style isolation preservation

*For any* email body HTML containing CSS styles, when rendered in the iframe, those styles SHALL NOT affect any DOM elements outside the iframe element.

**Validates: Requirements 2.2, 2.3**

### Property 5: Complete HTML structure generation

*For any* email body content and theme value, the generated iframe document SHALL contain a complete HTML structure with html, head (containing a style tag), and body tags.

**Validates: Requirements 4.1, 4.2, 4.3**

### Property 6: Safe iframe access

*For any* attempt to render email content, when the iframe reference is null or the contentDocument is inaccessible, the rendering function SHALL return early without throwing errors.

**Validates: Requirements 3.5**

### Property 7: V-html removal completeness

*For any* state of the MessageDetailPanel component, the template SHALL NOT contain any v-html directives in the email content rendering section.

**Validates: Requirements 6.1, 6.2, 6.3, 6.4**

## Error Handling

### Null Reference Handling

**Scenario**: Iframe ref is null when rendering is attempted

**Handling**: 
- Check `emailIframeRef.value` before accessing
- Return early if null
- Log warning in development mode

```typescript
if (!emailIframeRef.value) {
  console.warn('Email iframe ref is not available');
  return;
}
```

### Content Document Access Failure

**Scenario**: iframe.contentDocument is null or inaccessible

**Handling**:
- Check contentDocument existence before writing
- Return early if inaccessible
- This can occur during iframe initialization

```typescript
const doc = iframe.contentDocument;
if (!doc) {
  console.warn('Cannot access iframe content document');
  return;
}
```

### Empty or Invalid Email Body

**Scenario**: Email body is null, undefined, or empty string

**Handling**:
- Watch condition checks for truthiness of email body
- Render empty iframe document if body is empty
- No error thrown, graceful degradation

```typescript
watch(
  [() => message.value?.body, () => theme.theme],
  ([emailBody, currentTheme]) => {
    if (!emailBody) return; // Graceful exit
    // ... render logic
  }
);
```

### Theme Value Edge Cases

**Scenario**: Theme value is neither 'dark' nor 'light'

**Handling**:
- Default to 'dark' theme colors
- Use fallback in color selection logic

```typescript
const themeColors = colors[currentTheme] || colors.dark;
```

## Testing Strategy

### Unit Testing

**Framework**: Jest with Vue Test Utils

**Test Cases**:

1. **Component Mounting**
   - Verify iframe element is rendered
   - Verify iframe has correct attributes (class, sandbox)

2. **Theme Color Application**
   - Test dark theme applies correct colors
   - Test light theme applies correct colors
   - Test default fallback to dark theme

3. **Null Safety**
   - Test behavior when iframe ref is null
   - Test behavior when contentDocument is null
   - Test behavior when email body is empty/null

4. **V-html Removal**
   - Verify template does not contain v-html directive
   - Verify no div with v-html in message content section

### Property-Based Testing

**Framework**: fast-check (JavaScript property-based testing library)

**Configuration**: Minimum 100 iterations per property test

**Property Tests**:

1. **Property 1: Theme Color Consistency**
   - Generate: Random theme values ('dark', 'light', invalid strings)
   - Generate: Random HTML content strings
   - Verify: Injected styles match expected theme colors
   - Tag: **Feature: message-detail-panel, Property 1: Theme color application consistency**

2. **Property 2: Theme Change Reactivity**
   - Generate: Random email body HTML
   - Action: Toggle theme multiple times
   - Verify: Iframe document regenerated each time with correct colors
   - Tag: **Feature: message-detail-panel, Property 2: Iframe document regeneration on theme change**

3. **Property 3: Content Change Reactivity**
   - Generate: Sequence of different HTML content strings
   - Action: Update email body multiple times
   - Verify: Iframe document regenerated with each new content
   - Tag: **Feature: message-detail-panel, Property 3: Iframe document regeneration on content change**

4. **Property 4: Style Isolation**
   - Generate: HTML with various CSS styles (inline, style tags)
   - Action: Render in iframe
   - Verify: No styles leak to parent document
   - Tag: **Feature: message-detail-panel, Property 4: Style isolation preservation**

5. **Property 5: HTML Structure Completeness**
   - Generate: Random email body content
   - Generate: Random theme value
   - Verify: Generated HTML contains html, head, style, and body tags
   - Tag: **Feature: message-detail-panel, Property 5: Complete HTML structure generation**

6. **Property 6: Safe Access**
   - Generate: Various states (null ref, null doc, valid)
   - Verify: No errors thrown, graceful handling
   - Tag: **Feature: message-detail-panel, Property 6: Safe iframe access**

### Integration Testing

**Test Scenarios**:

1. **Full Message Display Flow**
   - Load message with HTML body
   - Verify iframe renders content
   - Switch theme
   - Verify iframe updates

2. **Message Navigation**
   - Display message A
   - Navigate to message B
   - Verify iframe updates with new content

3. **Theme Persistence**
   - Set theme to light
   - Reload component
   - Verify iframe uses light theme

### Manual Testing Checklist

- [ ] Email displays correctly in dark theme
- [ ] Email displays correctly in light theme
- [ ] Theme switch updates email immediately
- [ ] No visual flashing during theme switch
- [ ] Email styles don't affect surrounding UI
- [ ] Links in email have correct theme colors
- [ ] Empty emails don't cause errors
- [ ] Complex HTML emails render properly
- [ ] Inline styles in emails work correctly
- [ ] No console errors during normal operation

## Performance Considerations

### Iframe Document Regeneration

**Concern**: Frequent regeneration could impact performance

**Mitigation**:
- Watch only triggers on actual changes (Vue's reactivity handles this)
- Document write operations are fast for typical email sizes
- No unnecessary re-renders due to Vue's efficient diffing

### Memory Management

**Concern**: Iframe elements can consume memory

**Mitigation**:
- Single iframe per detail panel (not per message)
- Iframe content replaced, not accumulated
- Component cleanup handled by Vue lifecycle

### Initial Render Performance

**Concern**: First render might be slow

**Mitigation**:
- Immediate watch flag ensures quick initial render
- Minimal style injection keeps HTML small
- No external resource loading in iframe

## Security Considerations

### Iframe Sandbox Attribute

**Purpose**: Restrict iframe capabilities

**Configuration**:
```html
sandbox="allow-same-origin"
```

**Rationale**:
- `allow-same-origin`: Required to access contentDocument
- No `allow-scripts`: Prevents JavaScript execution in email
- No `allow-forms`: Prevents form submission
- No `allow-popups`: Prevents popup windows

### XSS Prevention

**Risk**: Malicious HTML in email body

**Mitigation**:
- Iframe provides isolation boundary
- Sandbox restrictions prevent script execution
- Backend should sanitize email content (out of scope)

### Content Security Policy

**Consideration**: Iframe content should respect CSP

**Implementation**:
- No inline scripts in injected HTML
- Only inline styles (necessary for theming)
- No external resource loading

## Migration Path

### Phase 1: Implementation
1. Add iframe element to template
2. Implement rendering logic
3. Add theme watching

### Phase 2: Testing
1. Run unit tests
2. Run property-based tests
3. Perform manual testing

### Phase 3: Cleanup
1. Remove v-html div
2. Remove unused whitespace-pre-wrap class
3. Update component documentation

### Phase 4: Deployment
1. Deploy to staging
2. Verify in production-like environment
3. Deploy to production
4. Monitor for issues

## Future Enhancements

### Potential Improvements

1. **Dynamic Height Adjustment**
   - Automatically resize iframe based on content height
   - Eliminate scrollbars for better UX

2. **Enhanced Styling**
   - Support for custom email fonts
   - Better handling of email-specific CSS

3. **Performance Optimization**
   - Debounce rapid theme changes
   - Lazy load email content for large messages

4. **Accessibility**
   - Add ARIA labels to iframe
   - Ensure keyboard navigation works

5. **Content Sanitization**
   - Frontend HTML sanitization layer
   - Remove potentially dangerous tags/attributes

These enhancements are not part of the current scope but may be considered for future iterations.
