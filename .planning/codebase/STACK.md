# Technology Stack

**Analysis Date:** 2026-06-08

## Languages

**Primary:**
- C# 12 (.NET 8.0) - Backend API, business logic, domain layer
- TypeScript 5.3 - Frontend application, type definitions

**Secondary:**
- SQL (PostgreSQL) - Database migrations, stored procedures
- SCSS - Component styles
- HTML - Vue templates

## Runtime

**Environment:**
- .NET 8.0 (ASP.NET Core) - Backend runtime
- Node.js >= 18.12 - Frontend build and dev server

**Package Manager:**
- NuGet (via `dotnet restore`) - Backend packages
  - NuGet sources: local `./packages`, Item Nexus (`https://nexus-public.item.pub/repository/nuget-hosted/index.json`), nuget.org
  - Config: `packages/flowFlex-backend/nuget.config`
  - Local package cache: `packages/flowFlex-backend/packages/`
- pnpm 8.10.0 - Frontend packages
  - Lockfile: present (`pnpm-lock.yaml`)
  - Enforced via `preinstall` script (`npx only-allow pnpm`)

## Frameworks

**Core:**
- ASP.NET Core 8.0 - Web API framework (`packages/flowFlex-backend/WebApi/WebApi.csproj`)
- Vue 3.5.12 - Frontend SPA framework (`packages/flowFlex-common/package.json`)
- Element Plus 2.9.1 - UI component library
- Pinia 2.2.6 - State management

**Testing:**
- xUnit 2.6.2 - Backend unit testing (`packages/flowFlex-backend/Tests/FlowFlex.Tests/FlowFlex.Tests.csproj`)
- Moq 4.20.70 - Backend mocking
- FluentAssertions 6.12.0 - Backend assertion library
- Jest 29.7.0 - Frontend testing
- @vue/test-utils 2.4.6 - Vue component testing

**Build/Dev:**
- Vite 5.4.18 - Frontend build tool and dev server (`packages/flowFlex-common/vite.config.ts`)
- Terser - Production minification (via Vite)
- Turbo 1.13.4 - Monorepo task runner (frontend internal packages)
- Docker (multi-stage build) - Backend containerization (`packages/flowFlex-backend/Dockerfile`)

## Key Dependencies

**Critical (Backend):**
- SqlSugarCore 5.1.4.159-preview23 - ORM (Domain, SqlSugarDB, Infrastructure layers)
- AutoMapper 12.0.1 - Object mapping (Application layer)
- FluentValidation.AspNetCore 11.3.0 - Request DTO validation
- Hangfire.AspNetCore 1.8.12 + Hangfire.PostgreSql 1.20.9 - Background job processing
- MediatR 12.3.0 - In-process messaging / domain events (`Domain.Shared`)
- Serilog.AspNetCore 8.0.3 - Structured logging (with PostgreSQL + File sinks)
- Polly 8.2.0 - HTTP resilience and retry policies
- RulesEngine 5.0.3 - Stage condition evaluation
- Newtonsoft.Json 13.0.3 - JSON serialization (API layer)
- System.IdentityModel.Tokens.Jwt 7.1.2 - JWT token handling

**Critical (Frontend):**
- Axios 1.7.7 - HTTP client
- Vue Router 4.4.5 - Client-side routing
- vue-i18n 9.14.1 - Internationalization
- @vue-flow/core 1.48.1 - Workflow diagram visualization
- DOMPurify 3.3.1 - XSS sanitization
- ECharts 5.5.1 - Charting
- dayjs 1.11.13 - Date manipulation
- crypto-js 4.2.0 - Client-side encryption

**Infrastructure (Backend):**
- Item.Redis 8.0.1 - Redis caching integration
- Item.Internal.Auth (local package) - JWT + IDM + ItemIAM authentication
- Item.BlobProvider 8.0.0 - File storage abstraction (Aliyun OSS / AWS S3)
- Item.Message.RabbitMq 8.0.0 - RabbitMQ messaging
- Item.Internal.Nacos 8.0.0 - Nacos service discovery
- Item.Internal.Framework 8.0.3 - Internal framework utilities
- Item.Internal.StandardApi 8.0.4 - Standard API patterns
- Item.Common.Lib 8.0.7 - Shared utilities
- Item.Email.Lib 8.0.0 - Email sending utilities
- Item.Internal.ChangeLog 8.0.23 - Audit/change log tracking
- Aliyun.OSS.SDK.NetCore 2.14.1 - Aliyun OSS direct SDK
- AWSSDK.S3 3.7.406 - AWS S3 SDK
- Microsoft.Extensions.Caching.StackExchangeRedis 8.0.0 - Redis distributed cache

**Excel/Document Processing (Backend):**
- EPPlus 8.0.5 - Excel generation (non-commercial license)
- ClosedXML 0.104.1 - Excel manipulation
- MiniExcel 1.36.0 - Lightweight Excel reading
- DocumentFormat.OpenXml 3.2.0 - Word/OpenXML processing
- ExcelDataReader 3.7.0 - Excel import

**Frontend Dev Tools:**
- ESLint 8.57.1 + Prettier 3.0.3 - Code formatting/linting
- Husky 8.0.3 + lint-staged 15.2.10 - Git hooks
- unplugin-auto-import 0.17.8 - Auto-import Vue APIs
- unplugin-vue-components 0.27.4 - Auto-import Vue components
- Tailwind CSS 3.4.14 - Utility-first CSS
- PostCSS 8.4.47 + Autoprefixer 10.4.20 - CSS processing

## Configuration

**Environment (Backend):**
- `packages/flowFlex-backend/WebApi/appsettings.json` - Base configuration
- `packages/flowFlex-backend/WebApi/appsettings.Development.json` - Dev overrides
- Config sections: `Database`, `Redis`, `Cache`, `Security`, `Global`, `BlobStore`, `AI`, `Email`, `FileStorage`, `IdmApis`, `IdentityHubConfig`, `ItemIamConfig`, `OutlookApis`, `IdeApis`, `Logging`

**Environment (Frontend):**
- `packages/flowFlex-common/.env` - Base env vars
- `packages/flowFlex-common/.env.development` - Dev environment
- `packages/flowFlex-common/.env.stage` - Staging environment
- `packages/flowFlex-common/.env.preview` - Preview environment
- `packages/flowFlex-common/.env.production` - Production environment
- `packages/flowFlex-common/.env.localhost` - Local dev environment
- Key vars: `VITE_GLOB_API_URL`, `VITE_PROXY_URL`, `VITE_GLOB_IDM_URL`, `VITE_GLOB_CODE`, `VITE_GLOB_DOMAIN_URL`

**Build:**
- `packages/flowFlex-backend/FlowFlex.sln` - .NET solution file
- `packages/flowFlex-backend/Directory.Build.props` - MSBuild properties (local package paths)
- `packages/flowFlex-common/vite.config.ts` - Vite build configuration
- `packages/flowFlex-common/tsconfig.json` - TypeScript configuration (extends `@uni/ts-config/vue-app.json`)

## Platform Requirements

**Development:**
- .NET 8.0 SDK
- Node.js >= 18.12
- pnpm >= 8.10.0
- PostgreSQL 15+
- Redis

**Production:**
- Docker container (Linux, .NET 8.0 ASP.NET runtime)
- Container port: 8080
- PostgreSQL 15 (Alpine image in docker-compose)
- Volumes: `/app/wwwroot/uploads` (file storage), `/app/logs` (application logs)

**Docker:**
- Backend: `packages/flowFlex-backend/Dockerfile` - Multi-stage build (SDK 8.0 -> ASP.NET 8.0 runtime)
- Frontend: `packages/flowFlex-common/Dockerfile`
- Compose: `packages/flowFlex-backend/docker-compose.yml` - PostgreSQL + API service
- Dev compose: `packages/flowFlex-backend/docker-compose.dev.yml`

---

*Stack analysis: 2026-06-08*
