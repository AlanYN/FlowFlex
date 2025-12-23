# Implementation Plan

- [x] 1. Fix deprecated substr usage in emailDarkMode.ts
  - Replace all `substr()` calls with `substring()` method
  - Update hex color parsing in parseColor function (3 occurrences)
  - _Requirements: N/A (code quality improvement)_

- [x] 2. Verify and fix MessageDetailPanel iframe integration
  - Check if iframe rendering is implemented in MessageDetailPanel.vue
  - Verify that applyEmailDarkMode is called before iframe content injection
  - Ensure transformed HTML is passed to iframe, not original HTML
  - Update renderEmailInIframe method to call applyEmailDarkMode with correct theme parameter
  - _Requirements: 4.1, 4.2_

- [ ]* 2.1 Write property test for light mode identity
  - **Property 1: Light mode identity**
  - **Validates: Requirements 1.5, 4.5**

- [x] 3. Verify theme reactivity in MessageDetailPanel
  - Check that watch or computed property monitors theme.theme changes
  - Ensure iframe regenerates when theme changes
  - Verify theme state is correctly passed to applyEmailDarkMode
  - _Requirements: 3.1, 3.2, 4.2_

- [ ]* 3.1 Write property test for structure preservation
  - **Property 7: Structure preservation**
  - **Validates: Requirements 4.4**

- [x] 4. Test and verify color transformation rules
  - Test white background → dark background transformation
  - Test black text → light text transformation
  - Test brand color preservation (high saturation colors)
  - Test grayscale color inversion
  - Test extreme light colors → dark colors
  - Test extreme dark colors → light colors
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 2.1, 2.5_

- [ ]* 4.1 Write property test for dark background transformation
  - **Property 2: Dark background transformation**
  - **Validates: Requirements 1.1, 1.3**

- [ ]* 4.2 Write property test for text color transformation
  - **Property 3: Text color transformation**
  - **Validates: Requirements 1.2, 1.4**

- [ ]* 4.3 Write property test for brand color preservation
  - **Property 4: Brand color preservation**
  - **Validates: Requirements 2.1**

- [ ]* 4.4 Write property test for grayscale inversion
  - **Property 6: Grayscale inversion**
  - **Validates: Requirements 2.5**

- [x] 5. Verify comprehensive attribute handling
  - Test inline style (style="color: ...") transformation
  - Test bgcolor attribute transformation
  - Test font tag color attribute transformation
  - Test background shorthand property transformation
  - Test borderColor transformation
  - _Requirements: 4.3, 5.1, 5.3_

- [ ]* 5.1 Write property test for comprehensive attribute handling
  - **Property 8: Comprehensive attribute handling**
  - **Validates: Requirements 4.3, 5.1, 5.3**

- [x] 6. Verify nested element processing
  - Test deeply nested HTML structures with colors at multiple levels
  - Verify all levels are processed by processDomTree recursion
  - Test table-based layouts with nested elements
  - _Requirements: 5.2_

- [ ]* 6.1 Write property test for nested element processing
  - **Property 9: Nested element processing**
  - **Validates: Requirements 5.2**

- [x] 7. Verify color format parsing
  - Test hex color parsing (#RGB and #RRGGBB)
  - Test rgb() and rgba() parsing
  - Test named color parsing
  - Test invalid color format handling (should preserve original)
  - _Requirements: 5.4_

- [ ]* 7.1 Write property test for color format handling
  - **Property 10: Color format handling**
  - **Validates: Requirements 5.4**

- [x] 8. Verify transparency preservation
  - Test rgba colors maintain alpha channel after transformation
  - Test that only RGB values are transformed, not alpha
  - Test semi-transparent colors (alpha between 0 and 1)
  - _Requirements: 5.5_

- [ ]* 8.1 Write property test for transparency preservation
  - **Property 11: Transparency preservation**
  - **Validates: Requirements 5.5**

- [x] 9. Verify image preservation
  - Test that img tags are skipped in processDomTree
  - Verify img attributes remain unchanged
  - Test emails with multiple images
  - _Requirements: 2.3_

- [ ]* 9.1 Write property test for image preservation
  - **Property 5: Image preservation**
  - **Validates: Requirements 2.3**

- [x] 10. Add error handling and edge case tests
  - Test null/undefined HTML input
  - Test empty string HTML input
  - Test malformed HTML handling
  - Test invalid color values
  - Verify no errors are thrown for edge cases
  - _Requirements: N/A (robustness)_

- [x] 11. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 12. Manual testing with real email samples
  - Test with plain text emails
  - Test with HTML table-based emails
  - Test with branded marketing emails
  - Test with emails containing images
  - Test theme switching behavior
  - Verify no visual flashing during theme switch
  - _Requirements: 3.1, 3.2, 3.3, 3.4_

- [x] 13. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.
