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
// --- Services ---
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>(); // Mới thêm từ file của bạn
builder.Services.AddScoped<ISpeakerService, SpeakerService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEventService, EventService>();
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
// 6. CORS CONFIGURATION
// ==================================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// ==================================================================
// 7. MIDDLEWARE PIPELINE
// ==================================================================

// Health check endpoint (cho Docker HEALTHCHECK)
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// ✅ ENABLE SWAGGER CHO TẤT CẢ ENVIRONMENTS (để test trên Coolify)
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Interval Event Registration API v1");
    options.RoutePrefix = "swagger"; // URL: /swagger
});

app.UseHttpsRedirection();

app.UseCors("AllowAll"); // Đặt trước Auth

app.UseAuthentication(); // Đăng nhập
app.UseAuthorization();  // Phân quyền

app.MapControllers();

// Log startup info
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("🚀 Application started successfully!");
logger.LogInformation("📍 Environment: {Environment}", app.Environment.EnvironmentName);
logger.LogInformation("🔗 Swagger UI: /swagger");
logger.LogInformation("❤️ Health Check: /health");

app.Run();