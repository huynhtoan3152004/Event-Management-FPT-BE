# 🚀 HƯỚNG DẪN DEPLOY - LOCAL → COOLIFY

## 📋 Checklist trước khi deploy

### ✅ 1. Test trên local với Docker

```powershell
# Chạy script tự động
.\run-docker.ps1

# Hoặc chạy thủ công
docker build -t swp392fa-api:latest .
docker run -d --name swp392fa-api -p 8080:8080 --env-file .env.production swp392fa-api:latest

# Kiểm tra
docker logs -f swp392fa-api
```

**Test các endpoints:**
- ✅ http://localhost:8080/health
- ✅ http://localhost:8080/swagger
- ✅ http://localhost:8080/api/speakers?PageNumber=1&PageSize=10

---

### ✅ 2. Commit và push code lên Git

```bash
# Kiểm tra file nào sẽ được commit
git status

# Đảm bảo KHÔNG commit các file sau:
# ❌ .env.production
# ❌ appsettings.Development.local.json
# ❌ coolify-env.txt

# Add files
git add .

# Commit
git commit -m "Add Docker support with secure config"

# Push
git push origin develop
```

---

### ✅ 3. Deploy lên Coolify

#### **Bước 1: Tạo Application**
1. Login vào Coolify Dashboard
2. Click **New Resource** → **Application**
3. Chọn **Git Repository**
4. Connect repository: `Event-Management-FPT-BE`
5. Branch: `develop`

#### **Bước 2: Configure Build**
- **Build Pack**: Dockerfile
- **Dockerfile Location**: `/Dockerfile`
- **Port**: `8080`

#### **Bước 3: Environment Variables**
Copy nội dung từ file `coolify-env.txt` và paste vào **Environment Variables** tab:

```
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
ConnectionStrings__DefaultConnectionStringDB=Host=dpg-d4mljmhr0fns73aa6kqg-a.singapore-postgres.render.com;Port=5432;Database=culturallnvh_ok1m;Username=culturallnvh_ok1m_user;Password=nh3rK4Z8Pp7BnEhtJQBv7xRyRpj4QxHA;Ssl Mode=Require;Trust Server Certificate=true
Jwt__Issuer=IntervalEventRegistration
Jwt__Audience=IntervalEventRegistration
Jwt__SecretKey=0ccf224b0557a0e2d345fcbceb29953e91e79a6463b126a4e9eaea8ac9e3347e
Jwt__AccessTokenMinutes=60
GoogleAuth__ClientId=495796699315-j37cuombmu3qjovfgtsm0bm69asvqdv8.apps.googleusercontent.com
GoogleAuth__ValidIssuer=https://accounts.google.com
GoogleAuth__Audience=495796699315-j37cuombmu3qjovfgtsm0bm69asvqdv8.apps.googleusercontent.com
Cloudinary__CloudName=dvsfvo9kb
Cloudinary__ApiKey=975939874752318
Cloudinary__ApiSecret=ln-FzCgfBA78_4fbp-umgs22ZKw
```

#### **Bước 4: Deploy**
1. Click **Deploy**
2. Xem logs build
3. Chờ deployment complete

#### **Bước 5: Test Production**
- Health: `https://your-domain/health`
- Swagger: `https://your-domain/swagger`
- API: `https://your-domain/api/speakers`

---

## 🔒 BẢO MẬT - QUAN TRỌNG!

**⚠️ CÁC SECRETS ĐÃ BỊ LỘ, BẠN PHẢI ĐỔI NGAY:**

### 1. Database Password (Render)
- Vào Render Dashboard → PostgreSQL
- Click **Reset Password**
- Update connection string mới

### 2. JWT SecretKey
```bash
# Generate key mới (64 bytes)
openssl rand -hex 64

# Hoặc dùng PowerShell
-join ((1..128) | ForEach-Object { '{0:X}' -f (Get-Random -Maximum 16) })
```

### 3. Google OAuth Client ID
- Vào [Google Cloud Console](https://console.cloud.google.com/apis/credentials)
- Tạo OAuth 2.0 Client ID mới
- Update ClientId và Audience

### 4. Cloudinary API Secret
- Vào [Cloudinary Dashboard](https://console.cloudinary.com/settings)
- Rotate API Secret
- Update ApiSecret mới

**Sau khi đổi tất cả secrets:**
1. Update vào `.env.production` (local)
2. Update vào `appsettings.Development.local.json` (local)
3. Update vào Coolify Environment Variables
4. KHÔNG commit các file có secrets thật

---

## 📁 Cấu trúc File Config

```
IntervalEventRegistration/
├── appsettings.json                      ✅ COMMIT (không có secret)
├── appsettings.Development.json          ✅ COMMIT (dev config)
├── appsettings.Development.local.json    ❌ KHÔNG COMMIT (secrets thật)
├── .env.production                       ❌ KHÔNG COMMIT (secrets thật)
├── Dockerfile                            ✅ COMMIT
├── .dockerignore                         ✅ COMMIT
├── run-docker.ps1                        ✅ COMMIT
└── coolify-env.txt                       ❌ KHÔNG COMMIT (có secrets)
```

---

## 🧪 Testing Workflow

### Local Development (với secrets thật):
```powershell
# File: appsettings.Development.local.json
# Kết nối Render DB + secrets thật
# KHÔNG commit file này

dotnet run --project IntervalEventRegistration
# Hoặc
.\run-docker.ps1
```

### Production Simulation (với Docker):
```powershell
# File: .env.production
# Test Docker image như trên Coolify
# KHÔNG commit file này

.\run-docker.ps1
```

### Production (Coolify):
```
# Secrets từ Environment Variables UI
# Không cần file .env
```

---

## 🐛 Troubleshooting

### Lỗi: Container crash
```powershell
docker logs swp392fa-api
```

### Lỗi: Không kết nối DB
```powershell
# Test connection string
docker exec swp392fa-api printenv | Select-String ConnectionStrings
```

### Lỗi: Swagger không hiển thị
- Kiểm tra logs có dòng: "Application started"
- Test: http://localhost:8080/swagger/v1/swagger.json

### Lỗi: Build failed trên Coolify
- Xem build logs trong Coolify UI
- Kiểm tra Dockerfile path đúng chưa
- Kiểm tra ENV variables đã set đầy đủ chưa

---

## 📞 Support

Nếu gặp vấn đề:
1. Xem logs: `docker logs swp392fa-api`
2. Test health: `curl http://localhost:8080/health`
3. Kiểm tra ENV: `docker exec swp392fa-api printenv`

---

## 🎯 Summary

- ✅ Local: Dùng `appsettings.Development.local.json`
- ✅ Docker: Dùng `.env.production`
- ✅ Coolify: Dùng Environment Variables UI
- ❌ KHÔNG commit file có secrets thật
- ⚠️ ĐỔI TẤT CẢ SECRETS trước khi deploy production!
