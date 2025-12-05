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

// Add services to the container.

// Đọc section "GoogleAuth" từ appsettings.json và bind vào GoogleAuthSettings
builder.Services.Configure<GoogleAuthSettings>(
    builder.Configuration.GetSection("GoogleAuth"));

// Đọc section "Jwt" từ appsettings.json và bind vào JwtSettings
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("Jwt"));

// ===== Database Configuration =====
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnectionStringDB"),
        npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorCodesToAdd: null);
        });
});

// ===== Repository Registration =====
builder.Services.AddScoped<ISpeakerRepository, SpeakerRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserAuthProviderRepository, UserAuthProviderRepository>();

// ===== Service Registration =====
builder.Services.AddScoped<ISpeakerService, SpeakerService>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddControllers();

// Lấy thông tin JwtSettings từ configuration để dùng cho TokenValidationParameters
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSettings = jwtSection.Get<JwtSettings>();

// Đăng ký Authentication với scheme mặc định là JwtBearer
builder.Services
    .AddAuthentication(options =>
    {
        // Scheme mặc định khi authenticate + challenge đều là JwtBearer
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        // Thiết lập rule validate JWT
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // Bật check Issuer (Issuer trong token phải trùng với JwtSettings.Issuer)
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,

            // Bật check Audience (aud trong token phải trùng với JwtSettings.Audience)
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,

            // Bật check khóa ký (SecretKey)
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.SecretKey)
            ),

            // Bật check thời hạn token (exp)
            ValidateLifetime = true,

            // Cho phép lệch giờ nhỏ để tránh lỗi do lệch time server (2 phút)
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Thông tin cơ bản của API
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Interval Event Registration API",
        Version = "v1"
    });

    // 1) Định nghĩa scheme "Bearer" cho JWT
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Nhập JWT theo dạng: Bearer {token}",
        Name = "Authorization",              // Tên header
        In = ParameterLocation.Header,       // Nằm ở Header
        Type = SecuritySchemeType.Http,      // Kiểu HTTP Authentication
        Scheme = "Bearer",                   // Schema = Bearer
        BearerFormat = "JWT"                 // Format = JWT
    });

    // 2) Áp dụng scheme Bearer cho toàn bộ API
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            // Tham chiếu tới scheme "Bearer" phía trên
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>() // Không cần scope cụ thể
        }
    });
});

// ===== CORS Configuration =====
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
