# Requirements Document

## Introduction

This feature enhances the FlowFlex onboarding system with two file-related improvements:
1. **Questionnaire File Upload Preview/Download** — Add preview and download capabilities for files already uploaded via the questionnaire `file_upload` question type.
2. **File Management Component Configuration** — Allow configuring Title, Description, and Required properties for the `files` (File Management) stage component in the Stage Editor.

## Glossary

- **Dynamic_Form**: The Vue component (`dynamicForm.vue`) that renders questionnaire questions and collects answers, including file upload fields.
- **File_Upload_Question**: A question type in the questionnaire system where users upload files. Uploaded files store metadata including `accessUrl`, `fullAccessUrl`, `fileId`, `uploadedBy`, and `uploadDate`.
- **Stage_Editor**: The workflow configuration UI (`StageComponentsSelector.vue`) where administrators select and configure stage components (fields, checklists, questionnaires, files, quick links).
- **Files_Component**: The stage component with key `'files'` that manages file attachments within a stage. Displayed as the Documents section in Case Detail.
- **Documents_Component**: The Vue component (`Documents.vue`) that renders file management UI in the Case Detail page, showing uploaded files with preview, download, and upload capabilities.
- **Stage_Component_Configuration**: A JSON object stored in the `components_json` JSONB column on the `ff_stage` table, containing component settings including the new `title`, `description`, and `isRequired` fields for the files component.
- **Preview_File**: The existing `vuePreviewFile` component and `previewOnboardingFile` API that renders file previews in a modal viewer.

## Requirements

### Requirement 1: File Preview in Questionnaire File Upload

**User Story:** As a user viewing a questionnaire, I want to preview uploaded files directly from the file list, so that I can verify file content without leaving the questionnaire form.

#### Acceptance Criteria

1. WHEN a file has been uploaded to a file_upload question and the file has an `accessUrl` or `fullAccessUrl`, THE Dynamic_Form SHALL display a preview button next to that file entry.
2. WHEN a user clicks the preview button, THE Dynamic_Form SHALL open the file in the existing Preview_File viewer component.
3. WHILE the questionnaire is in any status (Draft, Submitted, or other states), THE Dynamic_Form SHALL display the preview button for all uploaded files that have valid access URLs.
4. IF the file does not have a valid `accessUrl` or `fullAccessUrl`, THEN THE Dynamic_Form SHALL hide the preview button for that file entry.

### Requirement 2: File Download in Questionnaire File Upload

**User Story:** As a user viewing a questionnaire, I want to download uploaded files from the file list, so that I can save files locally for offline use.

#### Acceptance Criteria

1. WHEN a file has been uploaded to a file_upload question and the file has an `accessUrl` or `fullAccessUrl`, THE Dynamic_Form SHALL display a download button next to that file entry.
2. WHEN a user clicks the download button, THE Dynamic_Form SHALL trigger a browser file download using the file's access URL and original file name.
3. WHILE the questionnaire is in any status (Draft, Submitted, or other states), THE Dynamic_Form SHALL display the download button for all uploaded files that have valid access URLs.
4. IF the file does not have a valid `accessUrl` or `fullAccessUrl`, THEN THE Dynamic_Form SHALL hide the download button for that file entry.

### Requirement 3: File Management Component Title Configuration

**User Story:** As an administrator configuring a stage, I want to set a custom title for the File Management component, so that the Documents section heading reflects my organization's terminology.

#### Acceptance Criteria

1. WHEN the files component is enabled in the Stage_Editor, THE Stage_Editor SHALL display a text input field for the component title.
2. THE Stage_Editor SHALL allow the title field to be empty (optional).
3. WHEN a title value is saved in the Stage_Component_Configuration, THE Documents_Component SHALL display that title in place of the hardcoded "Documents" heading.
4. IF the title field is empty or not present in the configuration, THEN THE Documents_Component SHALL display the default "Documents" heading.
5. WHEN the title is configured, THE Stage_Editor SHALL display the configured title in the Selected Items panel (right panel) instead of the hardcoded "File Attachments" name.

### Requirement 4: File Management Component Description Configuration

**User Story:** As an administrator configuring a stage, I want to add a description to the File Management component, so that users see helpful context about what files to upload.

#### Acceptance Criteria

1. WHEN the files component is enabled in the Stage_Editor, THE Stage_Editor SHALL display a text input field for the component description.
2. THE Stage_Editor SHALL allow the description field to be empty (optional).
3. WHEN a description value is saved in the Stage_Component_Configuration, THE Documents_Component SHALL display that description below the title heading.
4. IF the description field is empty or not present in the configuration, THEN THE Documents_Component SHALL not render any description text.
5. WHEN the description is configured, THE Stage_Editor SHALL display the configured description in the Selected Items panel (right panel) instead of the hardcoded "Upload and manage files in this stage" text.

### Requirement 5: File Management Component Required Toggle Configuration

**User Story:** As an administrator configuring a stage, I want to control whether file upload is required in the File Management component, so that I can enforce mandatory document submissions.

#### Acceptance Criteria

1. WHEN the files component is enabled in the Stage_Editor, THE Stage_Editor SHALL display a toggle switch for the Required setting.
2. WHEN the Required toggle is enabled, THE Documents_Component SHALL display a required indicator (red asterisk) next to the title.
3. WHEN the Required toggle is disabled or not present in the configuration, THE Documents_Component SHALL not display the required indicator.
4. THE Stage_Editor SHALL persist the Required value as part of the files component configuration in the `components_json` JSONB column.

### Requirement 6: Backward Compatibility

**User Story:** As a system user, I want existing stages without custom title/description configuration to continue working normally, so that no data migration is required.

#### Acceptance Criteria

1. WHEN a stage component configuration does not contain title, description, or isRequired fields, THE Documents_Component SHALL render with default values ("Documents" title, no description, not required).
2. THE system SHALL serialize new configuration fields into the existing `components_json` JSONB column without requiring database schema changes.
3. WHEN the backend receives a files component without the new configuration fields, THE backend SHALL treat missing fields as null/default values without returning errors.

### Requirement 7: Backend Model Extension

**User Story:** As a developer, I want the StageComponent model to support the new configuration fields, so that title, description, and isRequired can be persisted and transmitted via API.

#### Acceptance Criteria

1. THE StageComponent model SHALL include a `Title` property (nullable string) for the files component display title.
2. THE StageComponent model SHALL include a `Description` property (nullable string) for the files component display description.
3. THE StageComponent model SHALL include an `IsRequired` property (nullable boolean) for the files component required flag.
4. WHEN serialized to JSON, THE StageComponent SHALL use camelCase field names (`title`, `description`, `isRequired`) to match frontend conventions.
