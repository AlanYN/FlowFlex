# User Invitation Feature Implementation

## Overview
This document describes the implementation of the user invitation feature for the FlowFlex onboarding portal. The feature allows administrators to invite users via email to access the onboarding portal, verify their email addresses, and complete the onboarding process.

## Architecture

### Backend Implementation

#### 1. Database Schema
- **Table**: `ff_user_invitations`
- **Purpose**: Store invitation records with tokens and status tracking
- **Key Fields**:
  - `invitation_token`: Unique token for each invitation
  - `email`: Invited user's email address
  - `onboarding_id`: Associated onboarding process
  - `status`: Invitation status (Pending, Active, Expired, Used)
  - `token_expiry`: Token expiration timestamp

#### 2. Core Components

##### DTOs (Data Transfer Objects)
- `UserInvitationRequestDto`: Request to send invitations
- `UserInvitationResponseDto`: Response with success/failure details
- `PortalUserDto`: Portal user information
- `PortalAccessVerificationRequestDto`: Email verification request
- `PortalAccessVerificationResponseDto`: Verification response with access token

##### Entities
- `UserInvitation`: Main entity for invitation records
- Includes navigation properties to `Onboarding` and `User` entities

##### Repository
- `IUserInvitationRepository`: Repository interface
- `UserInvitationRepository`: Implementation with SqlSugar ORM
- Methods for CRUD operations and token management

##### Services
- `IUserInvitationService`: Service interface
- `UserInvitationService`: Business logic implementation
- `IEmailService`: Extended to support invitation emails

##### Controllers
- `UserInvitationController`: REST API endpoints
- Endpoints for sending invitations, verification, and management

#### 3. API Endpoints

```
POST /ow/user-invitations/v1/send
- Send invitations to multiple email addresses
- Requires: onboardingId, emailAddresses[]
- Returns: success/failure counts and details

GET /ow/user-invitations/v1/portal-users/{onboardingId}
- Get all portal users for an onboarding process
- Returns: List of PortalUserDto

POST /ow/user-invitations/v1/verify-access
- Verify invitation token and email
- Requires: token, email
- Returns: access token and onboarding details

POST /ow/user-invitations/v1/resend
- Resend invitation to a user
- Requires: onboardingId, email
- Returns: success status

DELETE /ow/user-invitations/v1/remove-access/{onboardingId}
- Remove portal access for a user
- Requires: onboardingId, email query parameter
- Returns: success status
```

#### 4. Email Integration
- Extended `EmailService` with `SendOnboardingInvitationEmailAsync` method
- HTML email template with invitation link and instructions
- Configurable email settings via `EmailOptions`

#### 5. Security Features
- JWT token generation for authenticated portal access
- Secure invitation token generation using cryptographic random numbers
- Token expiration (7 days default)
- Email verification before access

### Frontend Implementation

#### 1. API Integration
- `userInvitation.ts`: API service layer
- TypeScript interfaces matching backend DTOs
- HTTP client integration with request/response handling

#### 2. Components

##### PortalAccessContent.vue
- Main component for managing portal access
- Features:
  - Send invitations to multiple emails
  - View invitation status
  - Resend invitations
  - Remove portal access
  - Real-time status updates

##### Portal Access Verification Page
- Standalone page for email verification
- Token validation and user authentication
- Redirect to onboarding after verification

#### 3. User Experience
- Intuitive email input with multi-select support
- Status indicators (Pending, Active, Expired)
- Success/error message handling
- Loading states and feedback

## Key Features

### 1. Multi-Email Invitation
- Send invitations to multiple email addresses simultaneously
- Batch processing with individual success/failure tracking
- Duplicate email handling

### 2. Token-Based Security
- Cryptographically secure invitation tokens
- Time-limited access (7 days)
- One-time use tokens

### 3. Email Verification Flow
1. Admin sends invitation via portal
2. User receives email with invitation link
3. User clicks link and verifies email address
4. System generates access token
5. User gains access to onboarding portal

### 4. Status Management
- Real-time status tracking
- Automatic status updates (Active, Expired, Used)
- Resend functionality for expired/failed invitations

### 5. User Management
- View all invited users for an onboarding process
- Remove access when needed
- Track invitation history and usage

## Configuration

### Email Settings
```json
{
  "Email": {
    "SmtpServer": "smtp.example.com",
    "SmtpPort": 587,
    "EnableSsl": true,
    "FromEmail": "noreply@flowflex.com",
    "FromName": "FlowFlex System",
    "Username": "noreply@flowflex.com",
    "Password": "your_smtp_password",
    "VerificationCodeExpiryMinutes": 10
  }
}
```

### JWT Settings
```json
{
  "Security": {
    "JwtSecretKey": "your-secret-key",
    "JwtIssuer": "FlowFlex",
    "JwtAudience": "FlowFlex.Client",
    "JwtExpiryMinutes": 1440
  }
}
```

## Database Migration

The feature includes a migration script to create the necessary database table:
- `CreateUserInvitationsTable_20250101000002.cs`
- Creates table with proper indexes and foreign key constraints
- Supports rollback functionality

## Testing

### Manual Testing Steps
1. Configure email settings in `appsettings.json`
2. Start backend service
3. Access onboarding portal management
4. Send test invitations
5. Check email delivery
6. Verify invitation links work correctly
7. Test portal access flow

### Integration Points
- Email service integration
- JWT authentication
- Database operations
- Frontend-backend communication

## Security Considerations

1. **Token Security**: Cryptographically secure random tokens
2. **Email Verification**: Required before portal access
3. **Time Limits**: Tokens expire after 7 days
4. **Access Control**: JWT-based authentication
5. **Input Validation**: Email format and required field validation

## Future Enhancements

1. **Bulk Operations**: Import email lists from CSV
2. **Template Customization**: Configurable email templates
3. **Notification System**: Real-time notifications for status changes
4. **Analytics**: Invitation and completion rate tracking
5. **Multi-language Support**: Localized email templates

## Troubleshooting

### Common Issues
1. **Email Not Sent**: Check SMTP configuration
2. **Token Expired**: Resend invitation to generate new token
3. **Access Denied**: Verify email matches invitation
4. **Database Errors**: Check migration execution

### Logs
- Email sending logs in `EmailService`
- Invitation creation logs in `UserInvitationService`
- Token verification logs in authentication flow

## Conclusion

The user invitation feature provides a complete solution for managing portal access in the FlowFlex onboarding system. It combines secure token-based authentication with a user-friendly interface for both administrators and end users. 