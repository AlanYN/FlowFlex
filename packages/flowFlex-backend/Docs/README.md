# FlowFlex - Open Source Workflow Management System

[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15+-blue.svg)](https://www.postgresql.org/)
[![Vue.js](https://img.shields.io/badge/Vue.js-3.0-green.svg)](https://vuejs.org/)
[![License](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

FlowFlex is an open-source workflow management system built with .NET 8 and Vue.js, providing comprehensive features for user authentication, workflow management, and task tracking.

## 🚀 Project Overview

FlowFlex aims to provide enterprises and teams with a flexible and scalable workflow management solution. The system adopts a modern technology stack, supports multiple authentication methods, and provides an intuitive user interface with powerful backend APIs.

## ✨ Key Features

### 🔐 User Authentication System
- **Multiple Login Methods**: Supports password login and email verification code login
- **User Registration**: Email verification code registration, no username required
- **JWT Authentication**: Token-based secure authentication mechanism
- **Email Verification**: Complete email verification process

### 📋 Workflow Management
- **Workflow Creation**: Visual workflow design
- **Stage Management**: Multi-stage workflow process support
- **Task Tracking**: Real-time task status monitoring
- **Checklists**: Detailed task checklists

### 🎯 Core Modules
- **Questionnaire Survey**: Custom questionnaire creation and management
- **Customer Portal**: Customer self-service interface
- **Onboarding Management**: Employee onboarding process management
- **System Fields**: Flexible field configuration system

## 🛠️ Technology Stack

### Backend Technologies
- **.NET 8.0** - Modern cross-platform framework
- **ASP.NET Core 8.0** - Web API framework
- **SqlSugar ORM** - High-performance ORM framework
- **PostgreSQL 15+** - Open-source relational database
- **JWT Bearer** - Identity authentication
- **AutoMapper** - Object mapping
- **Serilog** - Structured logging
- **BCrypt** - Password encryption

### Frontend Technologies
- **Vue.js 3.0** - Progressive JavaScript framework
- **Vue Router** - Single-page application routing
- **Axios** - HTTP client
- **Tailwind CSS** - Utility-first CSS framework
- **Font Awesome** - Icon library

### Development Tools
- **xUnit** - Unit testing framework
- **Moq** - Mocking framework
- **FluentAssertions** - Assertion library
- **Swagger** - API documentation generation

## 🚀 Quick Start

### Prerequisites

- **.NET 8.0 SDK** or higher
- **PostgreSQL 15+** database
- **Node.js 16+** (for frontend development)
- **Visual Studio 2022** or **VS Code**

### 1. Clone the Project

```bash
git clone https://github.com/your-org/FlowFlex.git
cd FlowFlex
```

### 2. Database Setup

#### 2.1 Create PostgreSQL Database
```sql
CREATE DATABASE flowflex;
```

#### 2.2 Execute Database Script
```bash
psql -d flowflex -f Docs/flowflex.sql
```

#### 2.3 Configure Connection String
Update `WebApi/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=flowflex;Username=your_username;Password=your_password;"
  }
}
```

### 3. Application Settings Configuration

Configure `WebApi/appsettings.json` with your settings:

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=flowflex;Username=your_username;Password=your_password;"
  },
  "Email": {
    "SmtpServer": "smtp.example.com",
    "SmtpPort": 587,
    "EnableSsl": true,
    "FromEmail": "noreply@flowflex.com",
    "FromName": "FlowFlex System",
    "Username": "noreply@flowflex.com",
    "Password": "your_smtp_password",
    "VerificationCodeExpiryMinutes": 10
  },
  "Jwt": {
    "SecretKey": "YourSuperSecretKeyForJWTAuthenticationChangeThisInProduction123456789",
    "Issuer": "FlowFlex",
    "Audience": "FlowFlex.Client",
    "ExpiryMinutes": 1440
  },
  "FileStorage": {
    "StorageType": "Local",
    "LocalStoragePath": "wwwroot/uploads",
    "FileUrlPrefix": "/uploads",
    "AllowedExtensions": ".jpg,.jpeg,.png,.gif,.pdf,.doc,.docx,.xls,.xlsx,.txt,.zip,.rar,.mp4,.avi,.mov",
    "MaxFileSize": 52428800,
    "EnableFileNameEncryption": true,
    "GroupByDate": true,
    "EnableAccessControl": true,
    "TempFileRetentionHours": 24
  }
}
```

### 4. Email Service Configuration

Configure SMTP settings in `WebApi/appsettings.json`:

- **SmtpServer**: Your SMTP server address
- **SmtpPort**: SMTP port (usually 587 for TLS)
- **EnableSsl**: Enable SSL/TLS encryption
- **FromEmail**: Sender email address
- **FromName**: Sender display name
- **Username**: SMTP authentication username
- **Password**: SMTP authentication password
- **VerificationCodeExpiryMinutes**: Email verification code expiry time

### 5. JWT Configuration

Configure JWT settings in `WebApi/appsettings.json`:

- **SecretKey**: JWT signing secret key (minimum 32 characters)
- **Issuer**: JWT issuer
- **Audience**: JWT audience
- **ExpiryMinutes**: Token expiry time in minutes

### 6. Start Backend Service

```bash
cd WebApi
dotnet restore
dotnet run
```

The backend service will start at `https://localhost:5019`.

### 7. Access the Application

- **API Documentation**: `https://localhost:5019/swagger`
- **Health Check**: `https://localhost:5019/health`

## 📁 Project Structure

```
FlowFlex/
├── Application/                 # Application Layer
│   ├── Services/               # Business service implementations
│   ├── Maps/                   # AutoMapper configurations
│   └── Client/                 # Third-party API clients
├── Application.Contracts/       # Application Layer Contracts
│   ├── Dtos/                   # Data Transfer Objects
│   ├── IServices/              # Service interfaces
│   └── Options/                # Configuration options
├── Domain/                     # Domain Layer
│   ├── Entities/               # Entity classes
│   ├── Manager/                # Domain managers
│   └── Repository/             # Repository interfaces
├── Domain.Shared/              # Domain Shared Layer
│   ├── Models/                 # Shared models
│   └── Enums/                  # Enum definitions
├── Infrastructure/             # Infrastructure Layer
│   ├── Services/               # Infrastructure services
│   └── Extensions/             # Extension methods
├── SqlSugarDB/                 # Data Access Layer
│   └── Implements/             # Repository implementations
├── WebApi/                     # Web API Layer
│   ├── Controllers/            # Controllers
│   ├── Middleware/             # Middleware
│   └── Program.cs              # Startup configuration
├── Docs/                       # Project Documentation
│   ├── README.md               # Project description
│   └── flowflex.sql            # Database schema script
└── tests/                      # Test Projects
    ├── Unit.Tests/             # Unit tests
    └── Integration.Tests/      # Integration tests
```

## 📚 Database Schema

FlowFlex uses PostgreSQL with the following main tables (all prefixed with `ff_`):

### Core Tables
- **ff_users**: User management and authentication
- **ff_workflow**: Workflow definitions
- **ff_stage**: Workflow stages
- **ff_checklist**: Task checklists
- **ff_checklist_task**: Individual checklist tasks
- **ff_questionnaire**: Survey questionnaires
- **ff_onboarding**: Onboarding process management

### Support Tables
- **ff_operation_change_log**: Operation audit logs
- **ff_internal_notes**: Internal notes and comments
- **ff_static_field_values**: Dynamic field values
- **ff_workflow_version**: Workflow versioning
- **ff_stage_version**: Stage versioning

## 📚 API Documentation

### Authentication APIs
- `POST /api/auth/login` - User login
- `POST /api/auth/login-with-code` - Login with verification code
- `POST /api/auth/register` - User registration
- `POST /api/auth/send-verification-code` - Send verification code
- `POST /api/auth/verify-email` - Verify email
- `GET /api/auth/check-email` - Check if email exists
- `GET /api/auth/user-info` - Get current user info
- `POST /api/auth/logout` - User logout

### Workflow APIs
- `GET /api/workflow/workflows` - Get workflow list
- `GET /api/workflow/workflows/{id}` - Get workflow details
- `POST /api/workflow/workflows` - Create workflow
- `PUT /api/workflow/workflows/{id}` - Update workflow
- `DELETE /api/workflow/workflows/{id}` - Delete workflow

### Stage APIs
- `GET /api/workflow/stages` - Get stage list
- `GET /api/workflow/stages/{id}` - Get stage details
- `POST /api/workflow/stages` - Create stage
- `PUT /api/workflow/stages/{id}` - Update stage
- `DELETE /api/workflow/stages/{id}` - Delete stage

### Checklist APIs
- `GET /api/workflow/checklists` - Get checklist list
- `GET /api/workflow/checklists/{id}` - Get checklist details
- `POST /api/workflow/checklists` - Create checklist
- `PUT /api/workflow/checklists/{id}` - Update checklist
- `DELETE /api/workflow/checklists/{id}` - Delete checklist

### Questionnaire APIs
- `GET /api/workflow/questionnaires` - Get questionnaire list
- `GET /api/workflow/questionnaires/{id}` - Get questionnaire details
- `POST /api/workflow/questionnaires` - Create questionnaire
- `PUT /api/workflow/questionnaires/{id}` - Update questionnaire
- `DELETE /api/workflow/questionnaires/{id}` - Delete questionnaire

### Onboarding APIs
- `GET /api/workflow/onboardings` - Get onboarding list
- `GET /api/workflow/onboardings/{id}` - Get onboarding details
- `POST /api/workflow/onboardings` - Create onboarding
- `PUT /api/workflow/onboardings/{id}` - Update onboarding
- `DELETE /api/workflow/onboardings/{id}` - Delete onboarding

## 🧪 Testing

### Unit Tests
```bash
cd tests/Unit.Tests
dotnet test
```

### Integration Tests
```bash
cd tests/Integration.Tests
dotnet test
```

### Test Coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## 🚀 Deployment

### Docker Deployment
```bash
# Build Docker image
docker build -t flowflex .

# Run container
docker run -p 5019:5019 flowflex
```

### Manual Deployment
1. Publish the application:
```bash
dotnet publish -c Release -o ./publish
```

2. Deploy to server and configure IIS or Nginx

## 🔧 Configuration

### Environment Variables
You can override appsettings.json values using environment variables:

```bash
# Database connection
export ConnectionStrings__Default="Host=localhost;Port=5432;Database=flowflex;Username=user;Password=pass;"

# JWT settings
export Jwt__SecretKey="YourSecretKey"
export Jwt__Issuer="FlowFlex"

# Email settings
export Email__SmtpServer="smtp.example.com"
export Email__Username="your-email@example.com"
export Email__Password="your-password"
```

### Production Considerations
- Use strong JWT secret keys (minimum 32 characters)
- Enable HTTPS in production
- Configure proper CORS settings
- Set up proper logging levels
- Use connection pooling for database connections
- Configure proper file storage (consider cloud storage for production)

## 🤝 Contributing

We welcome contributions! Please follow these steps:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Thanks to all contributors who participated in this project
- Special thanks to the open-source community for providing excellent tools and frameworks
- Inspired by modern workflow management solutions

## 📞 Support

- **Documentation**: [GitHub Wiki](https://github.com/your-org/FlowFlex/wiki)
- **Issues**: [GitHub Issues](https://github.com/your-org/FlowFlex/issues)
- **Discussions**: [GitHub Discussions](https://github.com/your-org/FlowFlex/discussions)
- **Email**: support@flowflex.com

## 🗺️ Roadmap

- [ ] Multi-language support
- [ ] Advanced workflow designer
- [ ] Mobile application
- [ ] API rate limiting
- [ ] Advanced reporting
- [ ] Third-party integrations
- [ ] Cloud deployment support
- [ ] Advanced security features
- [ ] Real-time notifications
- [ ] Workflow templates marketplace

## 📊 Performance

FlowFlex is designed for high performance:
- Efficient database queries with proper indexing
- Caching strategies for frequently accessed data
- Asynchronous processing for long-running operations
- Optimized API responses with pagination
- Connection pooling for database connections

## 🔒 Security

Security is a top priority:
- JWT-based authentication
- Password hashing with BCrypt
- SQL injection prevention with parameterized queries
- CORS configuration
- Input validation and sanitization
- Audit logging for all operations

---

**Made with ❤️ by the FlowFlex Team** 