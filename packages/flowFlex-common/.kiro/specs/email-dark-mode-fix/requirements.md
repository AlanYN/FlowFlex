# Requirements Document

## Introduction

This specification addresses the issue where email content displays incorrectly in dark mode. Currently, when the application is in dark mode, email messages show with poor contrast and readability issues because the email HTML content (which is designed for light backgrounds) is not properly adapted to dark mode styling.

## Glossary

- **Email Content**: The HTML body of an email message received from external sources
- **Dark Mode**: The application's dark theme setting where the UI uses dark backgrounds and light text
- **Light Mode**: The application's light theme setting where the UI uses light backgrounds and dark text
- **Email Dark Mode Utility**: The utility function in `src/app/utils/emailDarkMode.ts` that transforms email HTML for dark mode display
- **MessageDetailPanel**: The component that displays email message details including the email body
- **Iframe Renderer**: The iframe element used to display email HTML content in isolation

## Requirements

### Requirement 1

**User Story:** As a user viewing emails in dark mode, I want email content to be readable with proper contrast, so that I can comfortably read messages without straining my eyes.

#### Acceptance Criteria

1. WHEN the application is in dark mode AND an email is displayed THEN the email background SHALL be dark (#1e1e1e or similar dark color)
2. WHEN the application is in dark mode AND an email contains light-colored text THEN the text SHALL be converted to light colors suitable for dark backgrounds
3. WHEN the application is in dark mode AND an email contains white or very light backgrounds THEN those backgrounds SHALL be converted to dark colors
4. WHEN the application is in dark mode AND an email contains dark text on light backgrounds THEN the text SHALL be converted to light colors
5. WHEN the application is in light mode THEN email content SHALL display with its original colors without modification

### Requirement 2

**User Story:** As a user viewing branded emails with company colors, I want brand colors and logos to remain recognizable in dark mode, so that I can still identify the sender and important visual elements.

#### Acceptance Criteria

1. WHEN an email contains high-saturation brand colors (buttons, logos, headers) THEN those colors SHALL be preserved in dark mode
2. WHEN an email contains colored links THEN the link colors SHALL be adjusted for dark mode readability while maintaining their distinctiveness
3. WHEN an email contains images THEN the images SHALL display without color inversion or modification
4. WHEN an email contains colored backgrounds for emphasis (not pure white) THEN those backgrounds SHALL be adjusted while preserving the color intent
5. WHEN an email contains grayscale colors THEN those colors SHALL be inverted appropriately for dark mode

### Requirement 3

**User Story:** As a user switching between light and dark modes, I want email content to update immediately, so that the display is always appropriate for my current theme setting.

#### Acceptance Criteria

1. WHEN a user switches from light mode to dark mode THEN the currently displayed email SHALL update to dark mode styling within 500ms
2. WHEN a user switches from dark mode to light mode THEN the currently displayed email SHALL update to light mode styling within 500ms
3. WHEN an email is opened in dark mode THEN the email SHALL render with dark mode styling from the initial display
4. WHEN an email is opened in light mode THEN the email SHALL render with original styling from the initial display
5. WHEN theme switching occurs THEN there SHALL be no visible flashing or intermediate incorrect color states

### Requirement 4

**User Story:** As a developer maintaining the email display functionality, I want the dark mode transformation to be applied correctly in the iframe rendering, so that all emails benefit from proper dark mode support.

#### Acceptance Criteria

1. WHEN email HTML is rendered in the iframe THEN the Email Dark Mode Utility SHALL be applied to the HTML content before injection
2. WHEN the theme changes THEN the iframe SHALL regenerate with the Email Dark Mode Utility applied according to the new theme
3. WHEN the Email Dark Mode Utility processes HTML THEN it SHALL handle inline styles, style attributes, and bgcolor attributes
4. WHEN the Email Dark Mode Utility processes HTML THEN it SHALL preserve the HTML structure and content
5. WHEN the Email Dark Mode Utility is called with light mode THEN it SHALL return the original HTML without modifications

### Requirement 5

**User Story:** As a user viewing emails with complex HTML layouts, I want the dark mode transformation to handle various HTML structures, so that all types of emails display correctly.

#### Acceptance Criteria

1. WHEN an email contains table-based layouts with bgcolor attributes THEN those colors SHALL be transformed for dark mode
2. WHEN an email contains nested elements with multiple style attributes THEN all relevant styles SHALL be transformed
3. WHEN an email contains font tags with color attributes THEN those colors SHALL be transformed for dark mode
4. WHEN an email contains CSS color values in different formats (hex, rgb, rgba, named colors) THEN all formats SHALL be correctly parsed and transformed
5. WHEN an email contains transparent or semi-transparent colors THEN those colors SHALL be handled appropriately without breaking the transparency

