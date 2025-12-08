using Microsoft.EntityFrameworkCore;
using IntervalEventRegistrationRepo.Data;
using IntervalEventRegistrationRepo.Interfaces;
using IntervalEventRegistrationRepo.Repository;
using IntervalEventRegistrationService.Configuration;
using IntervalEventRegistrationService.Interfaces;
using IntervalEventRegistrationService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// ===== Cloudinary Configuration =====
builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("Cloudinary"));
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();

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

// ===== Service Registration =====
builder.Services.AddScoped<ISpeakerService, SpeakerService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

app.UseAuthorization();

app.MapControllers();

app.Run();
