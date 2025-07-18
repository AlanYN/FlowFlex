using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.IIS;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OfficeOpenXml;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts.Options;
using FlowFlex.Application.Services.OW;
using FlowFlex.WebApi.Extensions;
using FlowFlex.WebApi.Middlewares;
using FlowFlex.SqlSugarDB.Extensions;
using FlowFlex.Infrastructure.Extensions;
using FlowFlex.Domain.Shared.JsonConverters;
using System;
using System.Linq;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Set EPPlus license (EPPlus 8.x new setup method)
ExcelPackage.License.SetNonCommercialPersonal("FlowFlex System");

// Get all assemblies - using a more reliable approach
var assemblies = new List<Assembly>();
try
{
    // Load assemblies by name to ensure they're properly loaded
    assemblies.Add(typeof(FlowFlex.Application.Maps.UserMapProfile).Assembly); // Application assembly
    assemblies.Add(typeof(FlowFlex.Domain.Entities.OW.User).Assembly); // Domain assembly
    assemblies.Add(typeof(FlowFlex.Application.Contracts.Dtos.OW.User.UserDto).Assembly); // Application.Contracts assembly

    // Also try loading from files as fallback
    var dllFiles = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "FlowFlex.*.dll");
    foreach (var dllFile in dllFiles)
    {
        try
        {
            var assembly = Assembly.LoadFrom(dllFile);
            if (!assemblies.Contains(assembly))
            {
                assemblies.Add(assembly);
            }
        }
        catch (Exception ex)
        {
            // Log but don't fail - some DLLs might not be loadable
            // Assembly loading failed - continue with other assemblies
        }
    }
}
catch (Exception ex)
{
    // Fallback to current approach if type loading fails
    // Assembly loading by type failed - using file loading fallback
    assemblies = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "FlowFlex.*.dll")
        .Select(Assembly.LoadFrom).ToList();
}

// Configure file upload limits
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 52428800; // 50MB
});

builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 52428800; // 50MB
});

builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = 52428800; // 50MB
    options.MultipartHeadersLengthLimit = int.MaxValue;
});

// Add services to the container.
builder.Services.AddControllers(options =>
{
    // Use central route prefix
    options.UseCentralRoutePrefix(new RouteAttribute("api/"));
})
.AddNewtonsoftJson(options =>
{
    // Configure Newtonsoft.Json settings
    options.SerializerSettings.DateFormatHandling = Newtonsoft.Json.DateFormatHandling.IsoDateFormat;
    options.SerializerSettings.DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc;
    options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;

    // Add global converter for long to string
    options.SerializerSettings.Converters.Add(new LongToStringConverter());
    options.SerializerSettings.Converters.Add(new NullableLongToStringConverter());
})
// Disable automatic model validation, allow model validation errors to pass through, handle manually in controllers
.ConfigureApiBehaviorOptions(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

// Register services (auto injection)
builder.Services.AddService(builder.Configuration);

// Register AutoMapper with explicit profile configuration
builder.Services.AddAutoMapper(config =>
{
    // Explicitly add all mapping profiles
    config.AddProfile<FlowFlex.Application.Maps.UserMapProfile>();
    config.AddProfile<FlowFlex.Application.Maps.WorkflowMapProfile>();
    config.AddProfile<FlowFlex.Application.Maps.OnboardingMapProfile>();
    config.AddProfile<FlowFlex.Application.Maps.ChecklistMapProfile>();
    config.AddProfile<FlowFlex.Application.Maps.QuestionnaireMapProfile>();
    config.AddProfile<FlowFlex.Application.Maps.StageMapProfile>();
    config.AddProfile<FlowFlex.Application.Maps.WorkflowVersionMapProfile>();
    config.AddProfile<FlowFlex.Application.Maps.InternalNoteMapProfile>();
    config.AddProfile<FlowFlex.Application.Maps.StaticFieldValueMapProfile>();
    config.AddProfile<FlowFlex.Application.Maps.OnboardingFileMapProfile>();
    config.AddProfile<FlowFlex.Application.Maps.QuestionnaireAnswerMapProfile>();
    config.AddProfile<FlowFlex.Application.Maps.OperationChangeLogMapProfile>();
    config.AddProfile<FlowFlex.Application.Maps.ChecklistTaskCompletionMapProfile>();
    config.AddProfile<FlowFlex.Application.Maps.ChecklistTaskMapProfile>();
    config.AddProfile<FlowFlex.Application.Maps.StageCompletionLogMapProfile>();
    config.AddProfile<FlowFlex.Application.Maps.StageVersionMapProfile>();
    config.AddProfile<FlowFlex.Application.Maps.QuestionnaireSectionMapProfile>();
}, assemblies);

// Configure options
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
// Manually configure JwtOptions from Security section
builder.Services.Configure<JwtOptions>(options =>
{
    options.SecretKey = builder.Configuration["Security:JwtSecretKey"];
    options.Issuer = builder.Configuration["Security:JwtIssuer"];
    options.Audience = builder.Configuration["Security:JwtAudience"];
    if (int.TryParse(builder.Configuration["Security:JwtExpiryMinutes"], out var expiryMinutes))
    {
        options.ExpiryMinutes = expiryMinutes;
    }
});
builder.Services.Configure<FileStorageOptions>(builder.Configuration.GetSection("FileStorage"));

// Register JWT authentication
var jwtSecretKey = builder.Configuration["Security:JwtSecretKey"];
var jwtIssuer = builder.Configuration["Security:JwtIssuer"];
var jwtAudience = builder.Configuration["Security:JwtAudience"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

// Register HTTP context accessor
builder.Services.AddHttpContextAccessor();

// Register CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader()
               .WithExposedHeaders("Content-Disposition");
    });
});

// Register API version control
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("x-api-version")
    );
});

// Register API version resource manager
builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// Register Swagger
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "FlowFlex API", Version = "v1" });
    options.DocInclusionPredicate((version, desc) =>
    {
        return true;
    });

    // Add custom Schema ID generator to resolve type conflicts with same name but different namespaces
    options.CustomSchemaIds(type =>
    {
        // Handle generic types
        if (type.IsGenericType)
        {
            var genericTypeName = type.Name;
            if (genericTypeName.IndexOf('`') > 0)
            {
                genericTypeName = genericTypeName.Substring(0, genericTypeName.IndexOf('`'));
            }

            // Recursively handle generic parameters, including nested generic types
            string GetGenericArgName(Type argType)
            {
                if (argType.IsGenericType)
                {
                    var argGenericName = argType.Name;
                    if (argGenericName.IndexOf('`') > 0)
                    {
                        argGenericName = argGenericName.Substring(0, argGenericName.IndexOf('`'));
                    }

                    var nestedArgs = string.Join("_", argType.GetGenericArguments().Select(GetGenericArgName));
                    return $"{argGenericName}Of{nestedArgs}";
                }

                // For non-generic types, use the full name (including namespace) to avoid conflicts
                return argType.FullName?.Replace(".", "_") ?? argType.Name;
            }

            var genericArgs = string.Join("_", type.GetGenericArguments().Select(GetGenericArgName));
            return $"{genericTypeName}Of{genericArgs}";
        }

        // For specific conflicting types, add namespace prefix
        if (type.Name == "QuestionDto")
        {
            if (type.Namespace.Contains("Stage"))
                return "Stage_QuestionDto";
            if (type.Namespace.Contains("Questionnaire"))
                return "Questionnaire_QuestionDto";
        }

        // Default to using the full name (including namespace) to avoid conflicts
        return type.FullName?.Replace(".", "_") ?? type.Name;
    });

    // Add JWT authentication configuration
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Register infrastructure services
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddGlobalExceptionHandling();

// Note: Most services are auto-registered via IScopedService/ISingletonService/ITransientService interfaces
// Only register services that are not auto-registered or need special configuration

var app = builder.Build();

// Configure HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "OW Open API v1");
    });
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Configure static file services
app.UseStaticFiles();

// Ensure upload directory exists
var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
    // Upload directory created successfully
}

// Configure file upload access
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads",
    OnPrepareResponse = ctx =>
    {
        // Add security headers
        ctx.Context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        ctx.Context.Response.Headers["X-Frame-Options"] = "DENY";

        // Set cache policy
        var headers = ctx.Context.Response.GetTypedHeaders();
        headers.CacheControl = new Microsoft.Net.Http.Headers.CacheControlHeaderValue
        {
            Public = true,
            MaxAge = TimeSpan.FromDays(30)
        };
    }
});

app.UseRouting();
app.UseMiddleware<FlowFlex.WebApi.Middlewares.AppIsolationMiddleware>();
app.UseMiddleware<FlowFlex.WebApi.Middlewares.TenantMiddleware>();
app.UseMiddleware<FlowFlex.Infrastructure.Exceptions.GlobalExceptionHandlingMiddleware>();
app.UseMiddleware<FileAccessMiddleware>();
app.UseCors("AllowAll");

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseMiddleware<FlowFlex.WebApi.Middlewares.TokenValidationMiddleware>();
app.UseAuthorization();

app.MapControllers();

// Initialize database
try
{
    Console.WriteLine("[Program] Starting database initialization...");
    app.Services.InitializeDatabase();
    Console.WriteLine("[Program] Database initialization completed successfully");
}
catch (Exception ex)
{
    // Database initialization failed - log the error
    Console.WriteLine($"[Program] Database initialization failed: {ex.Message}");
    app.Logger.LogError(ex, "Database initialization failed");

    // In development environment, can choose to continue running, production environment should terminate
    if (!app.Environment.IsDevelopment())
    {
        Console.WriteLine("[Program] Production environment - terminating due to database initialization failure");
        throw;
    }
    else
    {
        Console.WriteLine("[Program] Development environment - continuing despite database initialization failure");
    }
}

// Execute database migrations (this is handled by InitializeDatabase, so we can remove this duplicate call)
// The DatabaseMigrationService is a different system that handles .sql files
// We're using the MigrationManager system instead

app.Run();
