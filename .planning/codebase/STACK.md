# Technology Stack

**Analysis Date:** 2026-05-25

## Languages

**Primary:**
- C# 12 (.NET 8) - Backend API, all server-side logic
- TypeScript 5.3 - Frontend SPA, all client-side logic

**Secondary:**
- SQL (PostgreSQL dialect) - Database migrations in `packages/flowFlex-backend/SqlSugarDB/Migrations/*.sql`
- SCSS - Frontend styles in `packages/flowFlex-common/src/styles/`
- HTML - Email templates in `packages/flowFlex-backend/Application/Templates/Email/*.html`

## Runtime

**Backend:**
- .NET 8.0 (ASP.NET Core Web API)
- Docker container: `mcr.microsoft.com/dotnet/aspnet:8.0`
- Exposed port: 8080

**Frontend:**
- Node.js >= 18.12.0
- Browser targets: > 1%, last 2 versions, not dead (ES2015 build target)

**Package Manager:**
- Frontend: pnpm 8.10.0 (enforced via `preinstall` hook)
- Lockfile: `packages/flowFlex-common/pnpm-lock.yaml` (present)
- Backend: NuGet (local packages folder at `packages/flowFlex-backend/packages/`)

## Frameworks

**Backend Core:**
- ASP.NET Core 8.0 - Web API framework (`packages/flowFlex-backend/WebApi/WebApi.csproj`)
- Clean Architecture with 6 layers: WebApi, Application, Application.Contracts, Domain, Domain.Shared, Infrastructure, SqlSugarDB

**Frontend Core:**
- Vue 3.5.12 - SPA framework (`packages/flowFlex-common/`)
- Vue Router 4.4.5 - Client-side routing
- Pinia 2.2.6 - State management with `pinia-plugin-persistedstate` 3.2.3

**Build/Dev:**
- Vite 5.4.18 - Frontend build tool (`packages/flowFlex-common/vite.config.ts`)
- Turbo 1.13.4 - Monorepo task runner
- Terser - Production minification (configured in `vite.config.ts`)

**Testing:**
- Backend: xUnit 2.6.2 + Moq 4.20.70 + FluentAssertions 6.12.0 (`packages/flowFlex-backend/Tests/FlowFlex.Tests/`)
- Frontend: Jest 29.7.0 + `@vue/test-utils` 2.4.6 (`packages/flowFlex-common/jest.config.ts`)

## Key Dependencies

**Backend Critical:**
- `SqlSugarCore` 5.1.4.159-preview23 - Primary ORM for PostgreSQL (`SqlSugarDB/SqlSugarDB.csproj`)
- `Microsoft.AspNetCore.Authentication.JwtBearer` 8.0.2 - JWT auth (`WebApi/WebApi.csproj`)
- `AutoMapper` 12.0.1 - Entity-DTO mapping (`Application/Application.csproj`)
- `FluentValidation.AspNetCore` 11.3.0 - Request validation (`Application/Application.csproj`)
- `Hangfire.AspNetCore` 1.8.12 + `Hangfire.PostgreSql` 1.20.9 - Background job scheduling (`WebApi/WebApi.csproj`)
- `Serilog.AspNetCore` 8.0.3 - Structured logging (`WebApi/WebApi.csproj`)
- `Polly` 8.2.0 + `Polly.Extensions.Http` 3.0.0 - HTTP resilience/retry (`Infrastructure/Infrastructure.csproj`)
- `RulesEngine` 5.0.3 - Business rules evaluation (`WebApi/WebApi.csproj`, `Application.Contracts/Application.Contracts.csproj`)
- `Newtonsoft.Json` 13.0.3 - JSON serialization (`Infrastructure/Infrastructure.csproj`)
- `Scrutor` 5.0.2 - Assembly scanning for DI auto-registration (`WebApi/WebApi.csproj`)
- `DotLiquid` 2.2.717 - Template engine for email rendering (`Application/Application.csproj`)
- `BCrypt.Net-Next` 4.0.3 - Password hashing (`Application/Application.csproj`)
- `NETCore.Encrypt` 2.1.1 - AES encryption (`Infrastructure/Infrastructure.csproj`)
- `EPPlus` 8.0.5 + `ClosedXML` 0.104.1 + `MiniExcel` 1.36.0 - Excel processing (`Application/Application.csproj`)
- `DocumentFormat.OpenXml` 3.2.0 - Word/Office document processing

**Internal (Item.*) Packages:**
- `Item.BlobProvider` 8.0.0 - Blob/file storage abstraction
- `Item.Internal.Framework` 8.0.3 - Internal framework base
- `Item.Internal.Nacos` 8.0.0 - Nacos service discovery/config
- `Item.Internal.StandardApi` 8.0.4 - Standard API conventions
- `Item.Common.Lib` 8.0.7 - Shared utilities
- `Item.Message.RabbitMq` 8.0.0 - RabbitMQ messaging
- `Item.Message.Kafka` 8.0.4 - Kafka messaging (`Application.Contracts/Application.Contracts.csproj`)
- `Item.Redis` 8.0.1 - Redis client wrapper
- `Item.Email.Lib` 8.0.0 - Email sending
- `Item.Internal.Auth` 8.0.0 - Internal auth/IAM
- `Item.ThirdParty` 8.0.6 - Third-party integrations
- `Item.Excel.Lib` 8.0.2 - Excel utilities

**Frontend Critical:**
- `element-plus` 2.9.1 - Primary UI component library
- `axios` 1.7.7 - HTTP client with interceptors (`src/app/apis/axios/`)
- `@vueuse/core` 10.11.1 - Vue composition utilities
- `vue-i18n` 9.14.1 - Internationalization
- `dayjs` 1.11.13 - Date manipulation
- `lodash-es` 4.17.21 - Utility functions
- `echarts` 5.5.1 + `chart.js` 4.4.6 - Data visualization
- `@vue-flow/core` 1.48.1 - Workflow canvas/diagram
- `pinia-plugin-persistedstate` 3.2.3 - Persisted Pinia stores
- `crypto-js` 4.2.0 - Client-side encryption
- `dompurify` 3.3.1 - XSS sanitization
- `markdown-it` 14.1.0 - Markdown rendering
- `@monaco-editor/loader` 1.5.0 - Code editor
- `vuedraggable` 4.1.0 - Drag-and-drop
- `gsap` 3.12.7 - Animations
- `xlsx-js-style` 1.2.0 - Excel export
- `jspdf` 3.0.1 + `html2canvas` 1.4.1 - PDF generation

**Frontend Dev:**
- `unplugin-auto-import` 0.17.8 - Auto-imports for Vue/Pinia APIs
- `unplugin-vue-components` 0.27.4 - Auto component registration
- `unplugin-icons` 22.1.0 - Icon auto-import
- `tailwindcss` 3.4.14 - Utility CSS framework
- `eslint` 8.57.1 + `prettier` 3.0.3 - Linting and formatting
- `husky` 8.0.3 + `lint-staged` 15.2.10 - Git hooks

## Configuration

**Backend Environment:**
- `appsettings.json` - Base config (`packages/flowFlex-backend/WebApi/appsettings.json`)
- `appsettings.Development.json` - Dev overrides (DB, Redis, IDM, Outlook, BlobStore)
- `appsettings.{Environment}.json` - Per-environment overrides
- Key config sections: `Database`, `Redis`, `Cache`, `Security`, `Global`, `BlobStore`, `AI`, `Email`, `FileStorage`, `IdmApis`, `IdentityHubConfig`, `ItemIamConfig`, `OutlookApis`

**Frontend Environment:**
- `.env` - Base vars (`packages/flowFlex-common/.env`)
- `.env.development`, `.env.localhost`, `.env.stage`, `.env.preview`, `.env.production` - Per-environment
- Key vars: `VITE_GLOB_API_URL`, `VITE_PROXY_URL`, `VITE_GLOB_IDM_URL`, `VITE_GLOB_DOMAIN_URL`, `VITE_GLOB_SSOURL`, `VITE_GLOB_CODE`, `VITE_USE_MOCK`, `VITE_BUILD_COMPRESS`

**Build:**
- `packages/flowFlex-common/vite.config.ts` - Vite build config
- `packages/flowFlex-common/tailwind.config.ts` - Tailwind theme (CSS variable-based)
- `packages/flowFlex-common/tsconfig.json` - TypeScript config
- `packages/flowFlex-backend/Dockerfile` - Docker multi-stage build (SDK 8.0 → aspnet 8.0 runtime)

## Platform Requirements

**Development:**
- Node.js >= 18.12.0
- pnpm >= 8.10.0
- .NET 8.0 SDK
- PostgreSQL instance
- Redis instance

**Production:**
- Docker (multi-stage build defined in `packages/flowFlex-backend/Dockerfile`)
- PostgreSQL database
- Redis cluster
- Blob storage: Aliyun OSS or AWS S3 (configurable via `Global.BlobStoreType`: Local/OSS/AWS)
- ASPNETCORE_ENVIRONMENT=Production, port 8080

---

*Stack analysis: 2026-05-25*
