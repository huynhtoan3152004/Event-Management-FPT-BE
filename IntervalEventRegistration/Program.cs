using Microsoft.EntityFrameworkCore;
using IntervalEventRegistrationRepo.Data;
using IntervalEventRegistrationRepo.Interfaces;
using IntervalEventRegistrationRepo.Repository;
using IntervalEventRegistrationService.Interfaces;
using IntervalEventRegistrationService.Services;
using IntervalEventRegistrationService.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.HttpOverrides;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ==================================================================
// 0. CONFIGURATION LOADING
// ==================================================================
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// ==================================================================
// 1. CONFIGURATION BINDING
// ==================================================================
builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("Cloudinary"));
builder.Services.Configure<GoogleAuthSettings>(builder.Configuration.GetSection("GoogleAuth"));
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

// ==================================================================
// 2. DATABASE CONFIGURATION
// ==================================================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnectionStringDB");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnectionStringDB' not found.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);
    });
});

// ==================================================================
// 3. DI CONTAINER REGISTRATION
// ==================================================================
builder.Services.AddScoped<ISpeakerRepository, SpeakerRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserAuthProviderRepository, UserAuthProviderRepository>();
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IHallRepository, HallRepository>();
builder.Services.AddScoped<ISeatRepository, SeatRepository>();
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<ITicketCheckinRepository, TicketCheckinRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();

builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();
builder.Services.AddScoped<ISpeakerService, SpeakerService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IHallService, HallService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IReportService, ReportService>();


builder.Services.AddControllers();

// ==================================================================
// 4. JWT AUTHENTICATION
// ==================================================================
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSettings = jwtSection.Get<JwtSettings>();

if (jwtSettings == null || string.IsNullOrEmpty(jwtSettings.SecretKey))
{
    Console.WriteLine("❌ ERROR: JWT Settings are missing or invalid!");
    throw new InvalidOperationException("JWT Settings are missing or invalid in appsettings.json");
}

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

// ==================================================================
// 5. SWAGGER CONFIGURATION
// ==================================================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Interval Event Registration API",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Nhập JWT theo dạng: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
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

// ==================================================================
// 6. CORS CONFIGURATION (SUPPORT ALLOW ALL FLAG)
// ==================================================================

// Check if AllowAll is enabled
var allowAll = builder.Configuration.GetValue<bool>("CORS:AllowAll", false);

if (allowAll)
{
    Console.WriteLine("⚠️ CORS: ALLOW ALL MODE ENABLED (NOT SECURE FOR PRODUCTION)");
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.SetIsOriginAllowed(_ => true)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });
    });
}
else
{
    // Parse allowed origins
    var corsOriginsConfig = builder.Configuration["CORS:AllowedOrigins"];
    var allowedOrigins = new List<string>
    {
        "http://localhost:3000",
        "http://localhost:3001"
    };

    if (!string.IsNullOrEmpty(corsOriginsConfig))
    {
        var parsedOrigins = corsOriginsConfig
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(o => o.Trim())
            .Where(o => !string.IsNullOrEmpty(o))
            .ToList();
        
        allowedOrigins.AddRange(parsedOrigins);
    }

    allowedOrigins = allowedOrigins.Distinct().ToList();

    Console.WriteLine("🌐 CORS Allowed Origins:");
    foreach (var origin in allowedOrigins)
    {
        Console.WriteLine($"   ✓ {origin}");
    }

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.WithOrigins(allowedOrigins.ToArray())
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials()
                  .SetIsOriginAllowedToAllowWildcardSubdomains();
        });
    });
}

var app = builder.Build();

// ==================================================================
// 7. MIDDLEWARE PIPELINE
// ==================================================================

// Configure forwarded headers for reverse proxy
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Health check
app.MapGet("/health", () => Results.Ok(new 
{ 
    status = "healthy", 
    timestamp = DateTime.UtcNow,
    corsMode = allowAll ? "AllowAll" : "Whitelist"
}));

// Swagger
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Interval Event Registration API v1");
    options.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();

// CORS
if (allowAll)
{
    app.UseCors("AllowAll");
    app.Logger.LogWarning("⚠️ CORS: AllowAll policy enabled (NOT SECURE FOR PRODUCTION)");
}
else
{
    app.UseCors("AllowFrontend");
    app.Logger.LogInformation("🔒 CORS: AllowFrontend policy enabled");
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Log startup
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("🚀 Application started successfully!");
logger.LogInformation("📍 Environment: {Environment}", app.Environment.EnvironmentName);
logger.LogInformation("🔗 Swagger: /swagger");
logger.LogInformation("❤️ Health: /health");

app.Run();