using System.Text;
using Eventloop.Api.Middleware;
using Eventloop.Api.Services;
using Eventloop.Api.Validators;
using Eventloop.Core.Enums;
using Eventloop.Core.Interfaces;
using Eventloop.Infrastructure.Data;
using Eventloop.Infrastructure.Email;
using Eventloop.Infrastructure.Security;
using Eventloop.Infrastructure.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add controllers and validators
builder.Services.AddControllers();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();
builder.Services.AddHttpContextAccessor();

// Database context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connectionString);
});

// Email Sender (Console for local dev, Azure Communication Services for prod)
var emailProvider = builder.Configuration["Email:Provider"] ?? "Console";
if (emailProvider.Equals("AzureCommunicationServices", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.Configure<AzureCommunicationServicesEmailOptions>(
        builder.Configuration.GetSection(AzureCommunicationServicesEmailOptions.SectionName));
    builder.Services.AddSingleton<IEmailSender, AzureCommunicationServicesEmailSender>();
}
else
{
    builder.Services.AddSingleton<IEmailSender, ConsoleEmailSender>();
}

// Current user and domain services
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddSingleton<IRegistrationEventPublisher, AzureQueueRegistrationEventPublisher>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<IEventService, EventService>();

// JWT Configuration
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
var secretKey = builder.Configuration["Jwt:SecretKey"] ?? "Eventloop-Default-Development-Secret-Key-Must-Be-At-Least-32-Chars-Long!";
var issuer = builder.Configuration["Jwt:Issuer"] ?? "EventloopApi";
var audience = builder.Configuration["Jwt:Audience"] ?? "EventloopApp";

// Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// Role-based Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy =>
        policy.RequireRole(UserRole.Admin.ToString()));

    options.AddPolicy("RequireOrganizer", policy =>
        policy.RequireRole(UserRole.Organizer.ToString(), UserRole.Admin.ToString()));
});

// CORS for React frontend (Vite dev server default: http://localhost:5173)
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendDev", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "http://localhost:3000"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Swagger with JWT Bearer support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Eventloop API",
        Version = "v1",
        Description = "College event registration and check-in system API"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.\nExample: \"Bearer eyJhbGciOi...\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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

var app = builder.Build();

// Global Exception Handler
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Swagger UI for development and testing
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Eventloop API v1");
    });
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("FrontendDev");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health/live");

app.Run();

// Partial Program class for WebApplicationFactory integration testing
public partial class Program { }
