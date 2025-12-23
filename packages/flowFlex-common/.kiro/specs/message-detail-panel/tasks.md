# Implementation Plan

- [x] 1. Set up iframe infrastructure and theme integration
  - Add iframe element to MessageDetailPanel template with proper attributes (ref, class, sandbox)
  - Import useTheme utility from @/utils/theme.ts
  - Create emailIframeRef template ref for iframe DOM access
  - Create theme constant using useTheme() composable
  - _Requirements: 2.1, 5.1, 5.2, 5.3_

- [ ]* 1.1 Write property test for safe iframe access
  - **Property 6: Safe iframe access**
  - **Validates: Requirements 3.5**

- [x] 2. Implement theme color configuration
  - Define ThemeColors interface with dark and light color schemes
  - Create colors constant object with background, text, and link colors for both themes
  - Ensure colors match requirements (#1e1e1e, #ddd, #6ea8fe for dark; #fff, #000, #0d6efd for light)
  - _Requirements: 1.1, 1.2, 1.4_

- [x] 3. Implement iframe HTML generation and injection method
  - Create renderEmailInIframe function that accepts htmlContent and currentTheme parameters
  - Add null safety checks for iframe ref and contentDocument
  - Implement theme color selection logic with fallback to dark theme
  - Generate complete HTML document string with html, head, style, and body tags
  - Inject theme-specific styles for body (background, color, margin, padding, font-family) and links
  - Use document.open(), document.write(), and document.close() sequence to write HTML to iframe
  - _Requirements: 3.5, 4.1, 4.2, 4.3, 4.4_

- [ ]* 3.1 Write property test for theme color application consistency
  - **Property 1: Theme color application consistency**
  - **Validates: Requirements 1.1, 1.2, 1.4**

- [ ]* 3.2 Write property test for complete HTML structure generation
  - **Property 5: Complete HTML structure generation**
  - **Validates: Requirements 4.1**

- [x] 4. Implement reactive rendering with watch
  - Create watch that monitors message.value?.body and theme.theme
  - Configure watch with immediate: true flag for initial render
  - Add conditional check to ensure both emailBody and emailIframeRef exist before rendering
  - Call renderEmailInIframe with current email body and theme when either changes
  - _Requirements: 1.3, 3.1, 3.2, 3.3_

- [ ]* 4.1 Write property test for iframe document regeneration on theme change
  - **Property 2: Iframe document regeneration on theme change**
  - **Validates: Requirements 1.3**

- [ ]* 4.2 Write property test for iframe document regeneration on content change
  - **Property 3: Iframe document regeneration on content change**
  - **Validates: Requirements 3.3**

- [x] 5. Remove v-html implementation
  - Locate and remove the div element with v-html="message.body" directive
  - Remove whitespace-pre-wrap class if no longer used elsewhere
  - Verify no other v-html directives remain in the message content section
  - _Requirements: 6.1, 6.2, 6.3, 6.4_

- [ ]* 5.1 Write unit test to verify v-html removal
  - Verify template does not contain v-html directive in email content section
  - **Validates: Requirements 6.1**

- [ ]* 6. Write property test for style isolation preservation
  - **Property 4: Style isolation preservation**
  - **Validates: Requirements 2.2, 2.3**

- [x] 7. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.
