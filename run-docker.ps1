# =============================================
# Script: Build và chạy Docker local
# Usage: .\run-docker.ps1
# =============================================

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "🚀 BUILD & RUN DOCKER - LOCAL TEST" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Bước 1: Dọn dẹp
Write-Host "1️⃣  Cleaning up old containers..." -ForegroundColor Yellow
docker stop swp392fa-api 2>$null
docker rm swp392fa-api 2>$null

# Bước 2: Build image
Write-Host "`n2️⃣  Building Docker image..." -ForegroundColor Yellow
docker build -t swp392fa-api:latest .

if ($LASTEXITCODE -ne 0) {
    Write-Host "`n❌ Build failed! Check errors above." -ForegroundColor Red
    exit 1
}

Write-Host "✅ Build successful!" -ForegroundColor Green

# Bước 3: Chạy container
Write-Host "`n3️⃣  Starting container with Production config..." -ForegroundColor Yellow
docker run -d `
  --name swp392fa-api `
  -p 8080:8080 `
  --env-file .env.production `
  --restart unless-stopped `
  swp392fa-api:latest

if ($LASTEXITCODE -ne 0) {
    Write-Host "`n❌ Container start failed!" -ForegroundColor Red
    exit 1
}

# Bước 4: Chờ container khởi động
Write-Host "`n4️⃣  Waiting for container to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

# Bước 5: Kiểm tra status
Write-Host "`n5️⃣  Container status:" -ForegroundColor Yellow
docker ps | Select-String swp392fa

# Bước 6: Xem logs
Write-Host "`n6️⃣  Recent logs:" -ForegroundColor Yellow
docker logs --tail 30 swp392fa-api

# Bước 7: Test health endpoint
Write-Host "`n7️⃣  Testing health endpoint..." -ForegroundColor Yellow
Start-Sleep -Seconds 3

try {
    $response = Invoke-WebRequest -Uri http://localhost:8080/health -UseBasicParsing -TimeoutSec 10
    Write-Host "✅ Health check: SUCCESS" -ForegroundColor Green
    Write-Host $response.Content -ForegroundColor Gray
} catch {
    Write-Host "❌ Health check: FAILED" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}

# Bước 8: Test Swagger
Write-Host "`n8️⃣  Testing Swagger endpoint..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri http://localhost:8080/swagger/v1/swagger.json -UseBasicParsing -TimeoutSec 10
    Write-Host "✅ Swagger JSON: SUCCESS" -ForegroundColor Green
} catch {
    Write-Host "❌ Swagger: FAILED" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}

# Kết quả
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "✅ SETUP COMPLETE!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "📍 Health Check: http://localhost:8080/health" -ForegroundColor White
Write-Host "📍 Swagger UI:   http://localhost:8080/swagger" -ForegroundColor White
Write-Host "📍 API Base:     http://localhost:8080/api" -ForegroundColor White
Write-Host "`n💡 Commands:" -ForegroundColor Yellow
Write-Host "   - View logs:    docker logs -f swp392fa-api" -ForegroundColor Gray
Write-Host "   - Stop:         docker stop swp392fa-api" -ForegroundColor Gray
Write-Host "   - Restart:      docker restart swp392fa-api" -ForegroundColor Gray
Write-Host "   - Remove:       docker rm -f swp392fa-api`n" -ForegroundColor Gray
