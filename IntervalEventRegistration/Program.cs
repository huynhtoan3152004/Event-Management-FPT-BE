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
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ==================================================================
// 0. CONFIGURATION LOADING (Load local config files if exist)
// ==================================================================
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// ==================================================================
// 1. CONFIGURATION BINDING (Đọc cấu hình từ appsettings.json)
// ==================================================================

// Cloudinary
builder.Services.Configure<CloudinarySettings>(
    builder.Configuration.GetSection("Cloudinary"));

// Google Auth
builder.Services.Configure<GoogleAuthSettings>(
    builder.Configuration.GetSection("GoogleAuth"));

// JWT Settings
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("Jwt"));

// ==================================================================
// 2. DATABASE CONFIGURATION (PostgreSQL)
// ==================================================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnectionStringDB");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnectionStringDB' not found.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString,
        npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorCodesToAdd: null);
        });
});

// ==================================================================
// 3. DI CONTAINER REGISTRATION (Đăng ký Service & Repo)
// ==================================================================

// --- Repositories ---
builder.Services.AddScoped<ISpeakerRepository, SpeakerRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserAuthProviderRepository, UserAuthProviderRepository>();
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IHallRepository, HallRepository>();
builder.Services.AddScoped<ISeatRepository, SeatRepository>();
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<ITicketCheckinRepository, TicketCheckinRepository>();

// --- Services ---
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();
builder.Services.AddScoped<ISpeakerService, SpeakerService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IHallService, HallService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddControllers();

// ==================================================================
// 4. JWT AUTHENTICATION CONFIGURATION
// ==================================================================
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSettings = jwtSection.Get<JwtSettings>();

// Kiểm tra null để tránh crash nếu quên cấu hình
if (jwtSettings == null || string.IsNullOrEmpty(jwtSettings.SecretKey))
{
    Console.WriteLine("❌ ERROR: JWT Settings are missing or invalid!");
    Console.WriteLine("Please check appsettings.json or Environment Variables.");
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
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.SecretKey)
            ),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

// ==================================================================
// 5. SWAGGER CONFIGURATION (Hỗ trợ Bearer Token)
// ==================================================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Interval Event Registration API",
        Version = "v1"
    });

    // Định nghĩa nút "Authorize" nhập Bearer Token
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
// 6. ✅ CORS CONFIGURATION (Đọc từ Environment Variable)
// ==================================================================

// Đọc CORS từ environment variable hoặc appsettings.json
var corsOriginsConfig = builder.Configuration["CORS:AllowedOrigins"];
var allowedOrigins = new List<string>
{
    "http://localhost:3000",  // Default local dev
    "http://localhost:3001"   // Default local dev alternate port
};

// Parse từ environment variable (format: url1,url2,url3)
if (!string.IsNullOrEmpty(corsOriginsConfig))
{
    var parsedOrigins = corsOriginsConfig
        .Split(',', StringSplitOptions.RemoveEmptyEntries)
        .Select(o => o.Trim())
        .Where(o => !string.IsNullOrEmpty(o))
        .ToList();
    
    allowedOrigins.AddRange(parsedOrigins);
}

// Remove duplicates
allowedOrigins = allowedOrigins.Distinct().ToList();

// Log allowed origins for debugging
Console.WriteLine("🌐 CORS Allowed Origins:");
foreach (var origin in allowedOrigins)
{
    Console.WriteLine($"   ✓ {origin}");
}

builder.Services.AddCors(options =>
{
    // Policy cho Production (đọc từ env)
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .SetIsOriginAllowed(origin => 
            {
                // Nếu không có origin header (same-origin request), cho phép
                if (string.IsNullOrEmpty(origin))
                    return true;
                
                // Check whitelist
                var isAllowed = allowedOrigins.Any(allowed => 
                    origin.Equals(allowed, StringComparison.OrdinalIgnoreCase) ||
                    origin.StartsWith(allowed.TrimEnd('/'), StringComparison.OrdinalIgnoreCase)
                );
                
                Console.WriteLine($"🔍 CORS Check: Origin={origin}, Allowed={isAllowed}");
                return isAllowed;
            })
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });

    // Policy cho Development (Allow all)
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true) // Allow any origin
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// ==================================================================
// 7. MIDDLEWARE PIPELINE
// ==================================================================

// Health check endpoint (cho Docker HEALTHCHECK)
app.MapGet("/health", () => Results.Ok(new { 
    status = "healthy", 
    timestamp = DateTime.UtcNow,
    allowedOrigins = allowedOrigins // Debug info
}));

// ✅ ENABLE SWAGGER CHO TẤT CẢ ENVIRONMENTS
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Interval Event Registration API v1");
    options.RoutePrefix = "swagger"; // URL: /swagger
});

// app.UseHttpsRedirection();

// ✅ DEBUG MIDDLEWARE: Log tất cả requests
app.Use(async (context, next) =>
{
    var origin = context.Request.Headers["Origin"].ToString();
    var referer = context.Request.Headers["Referer"].ToString();
    var method = context.Request.Method;
    var path = context.Request.Path;
    
    Console.WriteLine($"📨 Request: {method} {path}");
    Console.WriteLine($"   Origin: {(string.IsNullOrEmpty(origin) ? "(empty)" : origin)}");
    Console.WriteLine($"   Referer: {(string.IsNullOrEmpty(referer) ? "(empty)" : referer)}");
    
    await next();
    
    var corsHeaders = context.Response.Headers
        .Where(h => h.Key.StartsWith("Access-Control"))
        .Select(h => $"{h.Key}={h.Value}");
    
    if (corsHeaders.Any())
    {
        Console.WriteLine($"📤 CORS Headers: {string.Join(", ", corsHeaders)}");
    }
});

// ✅ LUÔN DÙNG AllowFrontend (đã đọc từ env)
app.UseCors("AllowFrontend");
app.Logger.LogInformation("🔒 CORS: AllowFrontend policy enabled");
app.Logger.LogInformation("🌐 Allowed origins: {Origins}", string.Join(", ", allowedOrigins));

app.UseAuthentication(); // Đăng nhập
app.UseAuthorization();  // Phân quyền

app.MapControllers();

// Log startup info
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("🚀 Application started successfully!");
logger.LogInformation("📍 Environment: {Environment}", app.Environment.EnvironmentName);
logger.LogInformation("🔗 Swagger UI: https://s4kc4gkkkc4ssko484sscow8.14.225.231.92.sslip.io/swagger");
logger.LogInformation("🌐 Frontend: https://eventfptticket.14.225.231.92.sslip.io");
logger.LogInformation("❤️ Health Check: /health");

app.Run();