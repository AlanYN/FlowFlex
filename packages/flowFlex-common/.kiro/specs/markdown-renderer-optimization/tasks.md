# Implementation Plan

- [x] 1. Install required dependencies
  - Install markdown-it, @types/markdown-it
  - Install markdown-it-task-lists, @types/markdown-it-task-lists
  - Install markdown-it-anchor, @types/markdown-it-anchor
  - Install highlight.js, @types/highlight.js
  - Install github-markdown-css
  - Install fast-check for property-based testing
  - _Requirements: 2.1, 2.2, 2.3, 2.5_

- [x] 2. Create MarkdownRenderer component
  - [x] 2.1 Create component file structure
    - Create `src/app/components/common/MarkdownRenderer.vue`
    - Define TypeScript interfaces for props (MarkdownRendererProps, MarkdownOptions)
    - Set up basic component template with markdown-body wrapper
    - _Requirements: 4.1, 4.2, 4.5_

  - [x] 2.2 Implement markdown-it initialization
    - Initialize markdown-it instance with configuration (html: true, linkify: true, typographer: true)
    - Configure highlight.js integration for code syntax highlighting
    - Add markdown-it-task-lists plugin for checkbox support
    - Add markdown-it-anchor plugin for heading anchors
    - _Requirements: 2.1, 2.2_

  - [x] 2.3 Implement rendering logic
    - Create computed property to render Markdown to HTML using markdown-it
    - Integrate DOMPurify to sanitize rendered HTML
    - Configure DOMPurify whitelist (ADD_ATTR, ALLOWED_TAGS, ALLOWED_ATTR)
    - Add error handling with try-catch for rendering failures
    - _Requirements: 1.1, 1.5, 5.1, 5.2_

  - [x] 2.4 Add external link security
    - Configure markdown-it to add target="_blank" to external links
    - Ensure DOMPurify preserves rel="noopener noreferrer" attributes
    - _Requirements: 5.3_

  - [x] 2.5 Import and apply GitHub Markdown CSS
    - Import github-markdown-css in component style section
    - Apply markdown-body class to wrapper div
    - Support custom CSS class via props
    - _Requirements: 2.3, 3.1_

  - [ ]* 2.6 Write property test for Markdown syntax completeness
    - **Property 1: Markdown syntax completeness**
    - **Validates: Requirements 1.1, 1.3, 1.4**
    - Generate random Markdown with headings, lists, links, tables, task lists
    - Verify all structural elements are present in rendered HTML

  - [ ]* 2.7 Write property test for code highlighting
    - **Property 2: Code highlighting consistency**
    - **Validates: Requirements 1.2**
    - Generate random code blocks with various supported languages
    - Verify highlight.js classes are applied to all code blocks

  - [ ]* 2.8 Write property test for HTML security
    - **Property 3: HTML security**
    - **Validates: Requirements 1.5, 5.4**
    - Generate random content with malicious scripts and attributes
    - Verify all dangerous content is removed from output

  - [ ]* 2.9 Write property test for external link security
    - **Property 4: External link security attributes**
    - **Validates: Requirements 5.3**
    - Generate random external links
    - Verify all links have rel="noopener noreferrer" attributes

  - [ ]* 2.10 Write property test for component reusability
    - **Property 5: Component reusability**
    - **Validates: Requirements 4.2**
    - Test component with random content in different mounting scenarios
    - Verify consistent rendering regardless of parent component

  - [ ]* 2.11 Write property test for styling consistency
    - **Property 6: Styling consistency**
    - **Validates: Requirements 3.1**
    - Generate random Markdown content
    - Verify markdown-body class is always applied

  - [ ]* 2.12 Write unit tests for MarkdownRenderer
    - Test empty content rendering
    - Test basic Markdown elements (headings, bold, italic, links)
    - Test code blocks with and without language specification
    - Test table rendering
    - Test task list rendering with checkboxes
    - Test XSS prevention (script tags, onclick attributes)
    - Test error handling for invalid content
    - Test props configuration (highlight, taskLists, customClass options)
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5_

- [x] 3. Update inbound-settings.vue component
  - [x] 3.1 Import MarkdownRenderer component
    - Add import statement for MarkdownRenderer
    - _Requirements: 4.4_

  - [x] 3.2 Remove custom Markdown rendering code
    - Remove markdownToHtml function
    - Remove renderedMarkdown computed property
    - _Requirements: 2.4_

  - [x] 3.3 Update template to use MarkdownRenderer
    - Replace v-html with MarkdownRenderer component in API Documentation Dialog
    - Pass attachmentApiMd as content prop
    - _Requirements: 4.2_

  - [ ]* 3.4 Write integration tests
    - Test MarkdownRenderer integration in dialog
    - Test loading state handling
    - Test copy functionality with rendered content
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5_

- [ ] 4. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 5. Update project documentation
  - [ ] 5.1 Add usage examples to component documentation
    - Document MarkdownRenderer props and options
    - Provide code examples for common use cases
    - Document supported Markdown syntax

  - [ ] 5.2 Update component README
    - Add MarkdownRenderer to components list
    - Document security features and XSS prevention
