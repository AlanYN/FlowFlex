# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Development Commands

### Frontend (Vue.js)
```bash
# Development
pnpm dev                    # Start development server
pnpm serve                  # Start development server (alias)
pnpm serve:local           # Start with localhost config
pnpm serve:staging         # Start with staging config
pnpm serve:preview         # Start with preview config

# Build
pnpm build:production      # Build for production
pnpm build:preview         # Build for preview environment
pnpm build:development     # Build for development
pnpm build:stage           # Build for staging

# Code Quality
pnpm lint                  # Run all linters in parallel
pnpm lint:eslint          # Run ESLint with auto-fix
pnpm lint:prettier        # Format code with Prettier
pnpm lint:stylelint       # Lint and fix styles
pnpm type:check           # TypeScript type checking

# Testing
pnpm test                  # Run tests (if configured)
```

### Backend (.NET)
```bash
# Development
dotnet restore             # Restore NuGet packages
dotnet build              # Build the solution
dotnet run --project WebApi  # Run the WebApi project

# Testing
dotnet test               # Run all tests
dotnet test tests/Unit.Tests  # Run unit tests
dotnet test tests/Integration.Tests  # Run integration tests

# Database
# Run migrations from SqlSugarDB project
dotnet run --project SqlSugarDB migrate  # Apply migrations
```

## Architecture Overview

### Solution Structure
FlowFlex is a monorepo containing both frontend and backend applications:

- **Frontend**: Vue.js 3 SPA located in `packages/flowFlex-common/`
- **Backend**: .NET 8 Web API located in `packages/flowFlex-backend/`

### Backend Architecture
The backend follows Clean Architecture principles with clear layer separation:

1. **WebApi Layer** (`WebApi/`)
   - Entry point and API controllers
   - Middleware for authentication, exception handling, and app isolation
   - JWT-based authentication with multi-tenancy support

2. **Application Layer** (`Application/` & `Application.Contracts/`)
   - Business logic and service implementations
   - DTOs and service interfaces
   - AutoMapper profiles for entity-DTO mapping
   - Notification handlers and workflow helpers

3. **Domain Layer** (`Domain/` & `Domain.Shared/`)
   - Core business entities and domain logic
   - Repository interfaces
   - Shared enums, constants, and domain events
   - Value objects and domain-specific exceptions

4. **Infrastructure Layer** (`Infrastructure/`)
   - Cross-cutting concerns and technical implementations
   - Database configuration and extensions
   - Third-party service integrations

5. **Data Access Layer** (`SqlSugarDB/`)
   - SqlSugar ORM implementation
   - Repository implementations
   - Database migrations (timestamp-based)
   - PostgreSQL as primary database

### Frontend Architecture
Vue.js 3 application with TypeScript:

- **State Management**: Pinia with persisted state
- **Routing**: Vue Router for SPA navigation
- **UI Framework**: Element Plus + Tailwind CSS
- **API Communication**: Axios with interceptors
- **Build Tool**: Vite with auto-imports and component registration

### Key Technical Decisions

1. **Multi-Tenancy**: App-level isolation using `AppCode` field throughout entities
2. **Authentication**: JWT Bearer tokens with email-based authentication
3. **Database**: PostgreSQL with JSONB columns for flexible data storage
4. **ORM**: SqlSugar for database operations with custom filters
5. **API Documentation**: Swagger/OpenAPI for API documentation
6. **Validation**: FluentValidation for request validation
7. **Logging**: Serilog with structured logging

### Workflow System Components

The core workflow system consists of:
- **Workflows**: Main workflow definitions with stages
- **Stages**: Individual steps in a workflow with components
- **Components**: Questionnaires, Checklists, Actions linked to stages
- **Stage Progress**: Tracks user progress through workflow stages
- **Events**: Event sourcing for workflow state changes

### Database Conventions
- All tables prefixed with `ff_` (FlowFlex)
- Snowflake IDs for primary keys
- JSONB columns for flexible/dynamic data
- Soft deletes using `IsDeleted` flag
- Multi-tenancy via `AppCode` field
- Audit fields: `CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy`

### API Patterns
- RESTful API design
- Consistent response format with `ApiResponse<T>`
- Pagination support with `PagedResult<T>`
- Global exception handling
- Request/response validation