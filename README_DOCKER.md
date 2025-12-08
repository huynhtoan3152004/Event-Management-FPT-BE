# 🚀 Interval Event Registration API - Docker Setup

## ✅ Status: READY FOR DEPLOYMENT

### 📊 Test Results (Local Docker)
- ✅ Docker Build: SUCCESS
- ✅ Container Running: SUCCESS  
- ✅ Health Check: http://localhost:8080/health - OK
- ✅ Swagger UI: http://localhost:8080/swagger - OK
- ✅ Database Connection: Render PostgreSQL - OK

---

## 🎯 Quick Start (Local)

### Option 1: Tự động (Khuyến nghị)
```powershell
.\run-docker.ps1
```

### Option 2: Thủ công
```powershell
# Build image
docker build -t swp392fa-api:latest .

# Run container
docker run -d --name swp392fa-api -p 8080:8080 --env-file .env.production swp392fa-api:latest

# View logs
docker logs -f swp392fa-api
```

### Test endpoints:
- Health: http://localhost:8080/health
- Swagger: http://localhost:8080/swagger
- API: http://localhost:8080/api/speakers?PageNumber=1&PageSize=10

---

## 📦 Deploy lên Coolify

### Bước 1: Push code
```bash
git add .
git commit -m "Add Docker support"
git push origin develop
```

### Bước 2: Coolify Configuration
1. **New Application** → Git Repository
2. **Branch**: `develop`
3. **Build Pack**: Dockerfile
4. **Port**: `8080`

### Bước 3: Environment Variables
Copy nội dung từ file `coolify-env.txt` hoặc:

```
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
ConnectionStrings__DefaultConnectionStringDB=<your-db-connection-string>
Jwt__Issuer=IntervalEventRegistration
Jwt__Audience=IntervalEventRegistration
Jwt__SecretKey=<your-jwt-secret>
Jwt__AccessTokenMinutes=60
GoogleAuth__ClientId=<your-google-client-id>
GoogleAuth__ValidIssuer=https://accounts.google.com
GoogleAuth__Audience=<your-google-client-id>
Cloudinary__CloudName=<your-cloudinary-name>
Cloudinary__ApiKey=<your-cloudinary-key>
Cloudinary__ApiSecret=<your-cloudinary-secret>
```

### Bước 4: Deploy
Click **Deploy** và chờ build hoàn tất.

---

## 🔒 Bảo mật

### ⚠️ QUAN TRỌNG: Các secrets đã bị lộ, phải đổi ngay!

**Các file KHÔNG được commit:**
- ❌ `.env.production`
- ❌ `appsettings.Development.local.json`
- ❌ `coolify-env.txt`

**Đổi secrets:**
1. Database password (Render Dashboard)
2. JWT SecretKey (`openssl rand -hex 64`)
3. Google OAuth Client ID (Google Cloud Console)
4. Cloudinary API Secret (Cloudinary Dashboard)

---

## 📁 Cấu trúc Config

```
appsettings.json                      → Base config (COMMIT)
appsettings.Development.json          → Dev config (COMMIT)
appsettings.Development.local.json    → Local secrets (KHÔNG COMMIT)
.env.production                       → Docker secrets (KHÔNG COMMIT)
Dockerfile                            → Docker build (COMMIT)
.dockerignore                         → Exclude secrets (COMMIT)
```

---

## 🛠️ Commands

### Docker
```powershell
# Build
docker build -t swp392fa-api:latest .

# Run
docker run -d --name swp392fa-api -p 8080:8080 --env-file .env.production swp392fa-api:latest

# Logs
docker logs -f swp392fa-api

# Stop
docker stop swp392fa-api

# Remove
docker rm swp392fa-api

# Restart
docker restart swp392fa-api
```

### Local Development
```powershell
# Run với Visual Studio
dotnet run --project IntervalEventRegistration

# Hoặc
cd IntervalEventRegistration
dotnet run
```

---

## 🐛 Troubleshooting

### Container crash
```powershell
docker logs swp392fa-api
```

### Port already in use
```powershell
docker stop $(docker ps -q --filter "publish=8080")
# Hoặc dùng port khác
docker run -d -p 8081:8080 --env-file .env.production swp392fa-api:latest
```

### Database connection failed
```powershell
# Kiểm tra connection string
docker exec swp392fa-api printenv | Select-String ConnectionStrings
```

---

## 📚 Tài liệu

- [DEPLOY_GUIDE.md](DEPLOY_GUIDE.md) - Hướng dẫn chi tiết deploy
- [COOLIFY_ENV_SETUP.md](COOLIFY_ENV_SETUP.md) - Setup ENV cho Coolify
- [coolify-env.txt](coolify-env.txt) - Template ENV variables

---

## 🎉 Features

- ✅ Multi-stage Docker build (optimized size)
- ✅ Non-root user (security)
- ✅ Health check endpoint
- ✅ Swagger UI (enabled in Production)
- ✅ Environment-based configuration
- ✅ Secrets management (ENV variables)
- ✅ CORS enabled
- ✅ JWT Authentication
- ✅ PostgreSQL with retry logic
- ✅ Cloudinary integration
- ✅ Google OAuth support

---

## 📊 Image Info

- **Base**: `mcr.microsoft.com/dotnet/aspnet:8.0-alpine`
- **Size**: ~220MB (optimized)
- **Port**: 8080
- **Health Check**: `/health`
- **Swagger**: `/swagger`

---

## 👨‍💻 Development

### Prerequisites
- .NET 8.0 SDK
- Docker Desktop
- PostgreSQL (local hoặc Render)

### Setup Local
1. Clone repo
2. Tạo file `appsettings.Development.local.json` với secrets thật
3. Chạy `dotnet run --project IntervalEventRegistration`

### Setup Docker
1. Tạo file `.env.production` với secrets thật
2. Chạy `.\run-docker.ps1`
3. Test http://localhost:8080/swagger

---

## 📞 Support

Nếu gặp vấn đề:
1. Kiểm tra logs: `docker logs swp392fa-api`
2. Test health: `curl http://localhost:8080/health`
3. Verify ENV: `docker exec swp392fa-api printenv`
4. Xem file [DEPLOY_GUIDE.md](DEPLOY_GUIDE.md)

---

## ✨ Next Steps

1. ✅ Test local với Docker - DONE
2. ⏳ Push code lên Git
3. ⏳ Setup Coolify với ENV variables từ `coolify-env.txt`
4. ⏳ Deploy và test production
5. ⏳ Đổi tất cả secrets

---

**Status**: ✅ Ready to deploy to Coolify!
