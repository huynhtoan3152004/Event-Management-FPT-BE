# ============================================
# Stage 1: Build
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution và project files
COPY ["IntervalEventRegistration/IntervalEventRegistration.csproj", "IntervalEventRegistration/"]
COPY ["IntervalEventRegistrationService/IntervalEventRegistrationService.csproj", "IntervalEventRegistrationService/"]
COPY ["IntervalEventRegistrationRepo/IntervalEventRegistrationRepo.csproj", "IntervalEventRegistrationRepo/"]

# Restore dependencies
RUN dotnet restore "IntervalEventRegistration/IntervalEventRegistration.csproj"

# Copy toàn bộ source code (trừ .env, *.local.json đã bị .dockerignore)
COPY . .

# Build project
WORKDIR "/src/IntervalEventRegistration"
RUN dotnet build "IntervalEventRegistration.csproj" -c Release -o /app/build

# ============================================
# Stage 2: Publish
# ============================================
FROM build AS publish
RUN dotnet publish "IntervalEventRegistration.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# ============================================
# Stage 3: Runtime (Minimal image)
# ============================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app

# Install curl cho healthcheck
RUN apk add --no-cache curl

# Copy published files
COPY --from=publish /app/publish .

# Create non-root user
RUN addgroup -g 1000 appuser && \
    adduser -u 1000 -G appuser -s /bin/sh -D appuser && \
    chown -R appuser:appuser /app

USER appuser

# Expose port
EXPOSE 8080

# Set environment variables for ASP.NET Core
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check - tăng thời gian chờ để app khởi động
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "IntervalEventRegistration.dll"]
