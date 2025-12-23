# Requirements Document

## Introduction

This specification defines the requirements for implementing iframe-based HTML email rendering with theme-aware styling in the MessageDetailPanel component. The current implementation uses `v-html` to render email content, which has limitations in styling control and theme integration. The new implementation will use an iframe with preprocessed HTML to ensure proper theme support and better isolation of email content.

## Glossary

- **MessageDetailPanel**: The Vue component responsible for displaying detailed email message content
- **Email Body**: The HTML content of an email message returned from the backend API
- **Theme System**: The application's dark/light theme switching mechanism managed through `src/app/utils/theme.ts`
- **Iframe Renderer**: An iframe element used to render HTML email content in isolation
- **Theme-Aware Styling**: CSS styles that adapt based on the current application theme (dark or light)
- **HTML Preprocessing**: The process of injecting theme-specific styles into email HTML before rendering

## Requirements

### Requirement 1

**User Story:** As a user viewing emails in the message center, I want email content to automatically adapt to my selected theme (dark or light), so that the reading experience is consistent with the rest of the application.

#### Acceptance Criteria

1. WHEN the application theme is set to dark THEN the Email Body SHALL render with a dark background color (#1e1e1e) and light text color (#ddd)
2. WHEN the application theme is set to light THEN the Email Body SHALL render with a light background color (#fff) and dark text color (#000)
3. WHEN a user switches the application theme THEN the Email Body SHALL update its styling immediately to match the new theme
4. WHEN the Email Body contains hyperlinks THEN the links SHALL render with theme-appropriate colors (#6ea8fe for dark theme, #0d6efd for light theme)
5. WHEN the Email Body is rendered THEN all theme-specific styles SHALL be applied before the content becomes visible to prevent visual flashing

### Requirement 2

**User Story:** As a user viewing emails with complex HTML content, I want the email to render in an isolated environment, so that email styles do not interfere with the application's UI and vice versa.

#### Acceptance Criteria

1. WHEN an email message is displayed THEN the MessageDetailPanel SHALL render the Email Body within an Iframe Renderer
2. WHEN the Email Body contains CSS styles THEN those styles SHALL NOT affect elements outside the Iframe Renderer
3. WHEN the application has global CSS styles THEN those styles SHALL NOT affect the content inside the Iframe Renderer except for explicitly injected theme styles
4. WHEN the Iframe Renderer is created THEN it SHALL have no default margins or padding that could affect email layout
5. WHEN the Email Body is empty or null THEN the Iframe Renderer SHALL display an empty document without errors

### Requirement 3

**User Story:** As a developer maintaining the message center, I want the iframe rendering implementation to be reactive to theme changes, so that the code is maintainable and follows Vue.js best practices.

#### Acceptance Criteria

1. WHEN the Theme System state changes THEN the MessageDetailPanel SHALL detect the change through Vue reactivity
2. WHEN a theme change is detected THEN the MessageDetailPanel SHALL regenerate the iframe document with updated theme styles
3. WHEN the Email Body content changes THEN the MessageDetailPanel SHALL regenerate the iframe document with the current theme styles
4. WHEN the iframe document is regenerated THEN the MessageDetailPanel SHALL use the document.open(), document.write(), and document.close() methods in sequence
5. WHEN accessing the iframe's content document THEN the MessageDetailPanel SHALL verify the iframe reference exists before attempting to access its contentDocument property

### Requirement 4

**User Story:** As a user viewing emails, I want the email content to render with proper HTML structure and styling, so that emails display as intended by the sender while respecting my theme preference.

#### Acceptance Criteria

1. WHEN the Iframe Renderer document is created THEN it SHALL include a complete HTML structure with html, head, and body tags
2. WHEN theme styles are injected THEN they SHALL be placed within a style tag in the head section
3. WHEN the Email Body HTML is inserted THEN it SHALL be placed within the body tag of the iframe document
4. WHEN the iframe document is written THEN it SHALL include base styles for body background color, text color, and link colors
5. WHEN the Email Body contains inline styles THEN those styles SHALL take precedence over the injected theme styles according to CSS specificity rules

### Requirement 5

**User Story:** As a user viewing emails in the message center, I want the iframe to be properly sized and styled, so that email content is readable and fits naturally within the detail panel layout.

#### Acceptance Criteria

1. WHEN the Iframe Renderer is displayed THEN it SHALL have a width of 100% to fill its container
2. WHEN the Iframe Renderer is displayed THEN it SHALL have a minimum height that accommodates typical email content
3. WHEN the Iframe Renderer is displayed THEN it SHALL have no visible border
4. WHEN the Iframe Renderer is displayed THEN it SHALL have a transparent background until the iframe document is loaded
5. WHEN the Email Body content exceeds the iframe height THEN the Iframe Renderer SHALL allow vertical scrolling within the iframe

### Requirement 6

**User Story:** As a developer, I want to remove the current v-html implementation, so that the codebase uses a single, secure method for rendering email content.

#### Acceptance Criteria

1. WHEN the iframe implementation is complete THEN the MessageDetailPanel SHALL NOT use v-html directive for rendering the Email Body
2. WHEN the iframe implementation is complete THEN the MessageDetailPanel SHALL remove the div element that previously used v-html
3. WHEN the iframe implementation is complete THEN all email content rendering SHALL be handled exclusively through the Iframe Renderer
4. WHEN the MessageDetailPanel template is reviewed THEN there SHALL be no remaining v-html directives in the message content section
5. WHEN the Email Body is rendered THEN it SHALL maintain the same visual spacing and layout as the previous v-html implementation
