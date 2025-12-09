# Requirements Document

## Introduction

This feature adds a dialog interface to display integration attachments retrieved from external systems, allowing users to select files for import into the onboarding document system. When users click the "Import from Integration" button, the system will display a list of available attachments from connected integration systems in a modal dialog with selection capabilities.

## Glossary

- **Integration System**: External systems (CRM, ERP, etc.) connected to the WFE platform that contain attachments
- **Attachment**: A file stored in an external integration system that can be imported
- **Import Dialog**: A modal window that displays available attachments from integration systems
- **Selection Interface**: UI components (checkboxes) that allow users to select one or more attachments
- **Documents Component**: The Vue component responsible for managing onboarding documents and file uploads
- **Source**: The integration system name where the attachment originates (e.g., CRM, BNP, WMS)
- **Module**: The specific module or entity type within the integration system (e.g., Deals, Leads, Invoices)

## Requirements

### Requirement 1

**User Story:** As a user, I want to see a dialog displaying available attachments from integration systems when I click "Import from Integration", so that I can review what files are available before selecting them.

#### Acceptance Criteria

1. WHEN a user clicks the "Import from Integration" button AND the integration query returns successfully, THEN the system SHALL display a modal dialog titled "Import Attachments from Integration Systems"
2. WHEN the import dialog opens, THEN the system SHALL display a subtitle text "Select attachments from your connected integration systems to import into"
3. WHEN the integration query returns no attachments, THEN the system SHALL display an empty state message in the dialog
4. WHEN the integration query fails, THEN the system SHALL display an error message to the user
5. WHEN the dialog is displayed, THEN the system SHALL provide a close button (X icon) in the top-right corner

### Requirement 2

**User Story:** As a user, I want to see attachment details in a table format, so that I can understand what each file contains before selecting it.

#### Acceptance Criteria

1. WHEN attachments are displayed in the dialog, THEN the system SHALL present them in a table with columns for "File Name", "Source", and "Module"
2. WHEN displaying the file name, THEN the system SHALL show a document icon followed by the file name text
3. WHEN displaying the source, THEN the system SHALL show the integration system name as a badge with appropriate styling
4. WHEN displaying the module, THEN the system SHALL show the module name in plain text
5. WHEN the table contains multiple rows, THEN the system SHALL apply alternating row styling for readability

### Requirement 3

**User Story:** As a user, I want to select individual attachments using checkboxes, so that I can choose which files to import.

#### Acceptance Criteria

1. WHEN the attachment table is displayed, THEN the system SHALL provide a checkbox in the leftmost column of each row
2. WHEN a user clicks a checkbox, THEN the system SHALL toggle the selection state of that attachment
3. WHEN a user clicks a table row, THEN the system SHALL toggle the checkbox for that row
4. WHEN an attachment is selected, THEN the system SHALL provide visual feedback indicating the selected state
5. WHEN multiple attachments exist, THEN the system SHALL allow users to select multiple attachments simultaneously

### Requirement 4

**User Story:** As a user, I want the dialog to have a clean, modern interface consistent with the application design, so that the experience feels cohesive.

#### Acceptance Criteria

1. WHEN the dialog is displayed, THEN the system SHALL use a dark theme background consistent with the application's design system
2. WHEN displaying the table header, THEN the system SHALL use a darker background color to distinguish it from table rows
3. WHEN displaying source badges, THEN the system SHALL use colored badges with appropriate contrast (purple/blue tones)
4. WHEN the dialog is open, THEN the system SHALL apply a backdrop overlay to focus user attention on the dialog
5. WHEN displaying file icons, THEN the system SHALL use document icons with appropriate color (purple/blue tone)

### Requirement 5

**User Story:** As a developer, I want the dialog component to be reusable and maintainable, so that it can be easily extended with additional functionality.

#### Acceptance Criteria

1. WHEN implementing the dialog, THEN the system SHALL create a separate Vue component for the import dialog
2. WHEN the dialog component is created, THEN the system SHALL accept props for controlling visibility and passing attachment data
3. WHEN the dialog component emits events, THEN the system SHALL use typed event definitions for selection changes
4. WHEN the dialog is closed, THEN the system SHALL emit a close event to the parent component
5. WHEN attachments are selected, THEN the system SHALL maintain the selection state within the component
