# Requirements Document

## Introduction

Message Center 是一个统一的消息管理中心，用于管理系统内的各类消息通信。该功能整合了三种消息类型：Internal Message（内部消息）、Customer Email（客户邮件，通过 Outlook 集成）和 Portal Message（Portal 消息）。用户可以在一个统一的界面中查看、发送、管理所有类型的消息，并支持消息与 Onboarding/Case 的关联、标签分类、星标、归档等功能。

## Glossary

- **Message_Center_System**: Message Center 后端 API 系统，负责处理所有消息相关的业务逻辑
- **Message**: 消息实体，包含 Internal Message、Customer Email、Portal Message 三种类型
- **Internal_Message**: 系统内部用户之间的消息通信
- **Customer_Email**: 通过 Microsoft Graph API 发送给客户的外部邮件
- **Portal_Message**: 发送到客户 Portal 的消息通知
- **Message_Folder**: 消息文件夹，包括 Inbox、Sent、Starred、Archive、Trash
- **Message_Label**: 消息标签，用于分类消息（Internal、External、Important、Portal）
- **Related_Entity**: 消息关联的业务实体（Onboarding/Case）
- **Attachment**: 消息附件
- **Recipient**: 消息接收者

## Requirements

### Requirement 1

**User Story:** As a user, I want to view all my messages in a unified inbox, so that I can manage all communications in one place.

#### Acceptance Criteria

1. WHEN a user requests the message list THEN the Message_Center_System SHALL return paginated messages sorted by received date in descending order
2. WHEN a user filters messages by folder (Inbox, Sent, Starred, Archive, Trash) THEN the Message_Center_System SHALL return only messages belonging to that folder
3. WHEN a user filters messages by label (Internal, External, Important, Portal) THEN the Message_Center_System SHALL return only messages with the specified label
4. WHEN a user searches messages by keyword THEN the Message_Center_System SHALL return messages where subject or body contains the search term
5. WHEN displaying message list items THEN the Message_Center_System SHALL include sender name, subject, preview text, date, labels, related entity code, read status, and starred status

### Requirement 2

**User Story:** As a user, I want to view the full details of a message, so that I can read the complete content and take actions.

#### Acceptance Criteria

1. WHEN a user requests a message by ID THEN the Message_Center_System SHALL return the complete message including subject, body, sender, recipients, attachments, and related entity information
2. WHEN a user views an unread message THEN the Message_Center_System SHALL mark the message as read automatically
3. WHEN a message has attachments THEN the Message_Center_System SHALL return attachment metadata including name, size, and content type

### Requirement 3

**User Story:** As a user, I want to compose and send internal messages to team members, so that I can communicate with colleagues about onboarding cases.

#### Acceptance Criteria

1. WHEN a user sends an internal message with valid recipient, subject, and body THEN the Message_Center_System SHALL create the message and deliver it to the recipient's inbox
2. WHEN a user attempts to send a message with empty subject or body THEN the Message_Center_System SHALL reject the request and return a validation error
3. WHEN a user sends an internal message THEN the Message_Center_System SHALL store a copy in the sender's Sent folder
4. WHEN a user sends an internal message with a related entity THEN the Message_Center_System SHALL associate the message with the specified Onboarding or Case
5. WHEN a user sends an internal message with attachments THEN the Message_Center_System SHALL store and associate the attachments with the message

### Requirement 4

**User Story:** As a user, I want to send emails to customers through Outlook integration, so that I can communicate with external parties directly from the system.

#### Acceptance Criteria

1. WHEN a user sends a customer email with valid recipient email address THEN the Message_Center_System SHALL send the email via Microsoft Graph API and store a record in the system
2. WHEN a user sends a customer email THEN the Message_Center_System SHALL use the user's bound Outlook account for sending
3. IF a user attempts to send a customer email without a bound Outlook account THEN the Message_Center_System SHALL reject the request and return an error indicating email binding is required
4. WHEN a customer email is sent successfully THEN the Message_Center_System SHALL store the message with External label in the Sent folder
5. WHEN a user sends a customer email with attachments THEN the Message_Center_System SHALL include the attachments in the outgoing email

### Requirement 5

**User Story:** As a user, I want to send messages to customer portals, so that customers can receive notifications in their portal dashboard.

#### Acceptance Criteria

1. WHEN a user sends a portal message with valid customer portal selection THEN the Message_Center_System SHALL create the message and make it available in the customer's portal
2. WHEN a user sends a portal message THEN the Message_Center_System SHALL store the message with Portal label in the Sent folder
3. WHEN a portal message is sent THEN the Message_Center_System SHALL associate it with the related Onboarding entity
4. WHEN a user sends a portal message with attachments THEN the Message_Center_System SHALL make the attachments downloadable from the customer portal

### Requirement 6

**User Story:** As a user, I want to manage my messages with actions like star, archive, and delete, so that I can organize my communications effectively.

#### Acceptance Criteria

1. WHEN a user stars a message THEN the Message_Center_System SHALL mark the message as starred and include it in the Starred folder view
2. WHEN a user unstars a message THEN the Message_Center_System SHALL remove the starred status from the message
3. WHEN a user archives a message THEN the Message_Center_System SHALL move the message to the Archive folder
4. WHEN a user deletes a message THEN the Message_Center_System SHALL move the message to the Trash folder
5. WHEN a user permanently deletes a message from Trash THEN the Message_Center_System SHALL remove the message record from the system
6. WHEN a user marks a message as read or unread THEN the Message_Center_System SHALL update the read status accordingly

### Requirement 7

**User Story:** As a user, I want to reply to and forward messages, so that I can continue conversations and share information with others.

#### Acceptance Criteria

1. WHEN a user replies to a message THEN the Message_Center_System SHALL create a new message with the original sender as recipient and preserve the conversation thread
2. WHEN a user forwards a message THEN the Message_Center_System SHALL create a new message with the original content and allow specifying new recipients
3. WHEN replying or forwarding THEN the Message_Center_System SHALL preserve the related entity association from the original message
4. WHEN replying to a message THEN the Message_Center_System SHALL prefix the subject with "RE:" if not already present
5. WHEN forwarding a message THEN the Message_Center_System SHALL prefix the subject with "FW:" if not already present

### Requirement 8

**User Story:** As a user, I want to see message counts for each folder, so that I can quickly understand my message status.

#### Acceptance Criteria

1. WHEN a user requests folder statistics THEN the Message_Center_System SHALL return the total count and unread count for each folder
2. WHEN a user requests inbox count THEN the Message_Center_System SHALL return the number of unread messages in the inbox
3. WHEN message status changes (read, archived, deleted) THEN the Message_Center_System SHALL update the folder counts accordingly

### Requirement 9

**User Story:** As a user, I want to download message attachments, so that I can access files shared in communications.

#### Acceptance Criteria

1. WHEN a user requests to download an attachment THEN the Message_Center_System SHALL return the file content with appropriate content type
2. WHEN downloading an attachment THEN the Message_Center_System SHALL verify the user has access to the parent message
3. IF a user requests an attachment that does not exist THEN the Message_Center_System SHALL return a not found error

### Requirement 10

**User Story:** As a system administrator, I want messages to be isolated by tenant, so that data security is maintained across organizations.

#### Acceptance Criteria

1. WHEN any message operation is performed THEN the Message_Center_System SHALL filter data by the current user's tenant ID
2. WHEN creating a message THEN the Message_Center_System SHALL automatically set the tenant ID from the user context
3. IF a user attempts to access a message from a different tenant THEN the Message_Center_System SHALL deny access and return an authorization error

### Requirement 11

**User Story:** As a user, I want to save message drafts, so that I can compose messages over time and send them when ready.

#### Acceptance Criteria

1. WHEN a user saves a draft THEN the Message_Center_System SHALL store the message with draft status and place it in the Drafts folder
2. WHEN a user updates a draft THEN the Message_Center_System SHALL update the draft content while preserving the draft status
3. WHEN a user sends a draft THEN the Message_Center_System SHALL send the message and move it from Drafts to Sent folder
4. WHEN a user deletes a draft THEN the Message_Center_System SHALL remove the draft from the Drafts folder
5. WHEN displaying the Drafts folder THEN the Message_Center_System SHALL show all unsent draft messages

### Requirement 12

**User Story:** As a user, I want to view and manage deleted Outlook emails, so that I can recover accidentally deleted emails or permanently remove them.

#### Acceptance Criteria

1. WHEN a user requests deleted Outlook emails THEN the Message_Center_System SHALL retrieve emails from the Outlook DeletedItems folder via Microsoft Graph API
2. WHEN a user restores a deleted Outlook email THEN the Message_Center_System SHALL move the email from DeletedItems to Inbox in Outlook
3. WHEN a user permanently deletes an Outlook email THEN the Message_Center_System SHALL remove the email from Outlook permanently via Microsoft Graph API
4. WHEN syncing Outlook emails THEN the Message_Center_System SHALL update local message records to reflect the current state in Outlook

### Requirement 13

**User Story:** As a user, I want to manage Outlook email drafts, so that I can compose emails over time and send them when ready through Outlook.

#### Acceptance Criteria

1. WHEN a user creates an Outlook draft THEN the Message_Center_System SHALL create the draft in Outlook's Drafts folder via Microsoft Graph API and store a local record
2. WHEN a user updates an Outlook draft THEN the Message_Center_System SHALL update the draft content in Outlook via Microsoft Graph API
3. WHEN a user sends an Outlook draft THEN the Message_Center_System SHALL send the draft via Microsoft Graph API and update the local record to reflect sent status
4. WHEN a user deletes an Outlook draft THEN the Message_Center_System SHALL remove the draft from Outlook via Microsoft Graph API and delete the local record
5. WHEN a user requests Outlook drafts THEN the Message_Center_System SHALL retrieve drafts from Outlook's Drafts folder via Microsoft Graph API
