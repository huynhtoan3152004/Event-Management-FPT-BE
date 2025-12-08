# 🧪 HALL API - JSON TEST CASES

## 🔐 Authentication Required
Tất cả POST/PUT/DELETE endpoints cần JWT token của Organizer

**Header:**
```
Authorization: Bearer YOUR_ORGANIZER_JWT_TOKEN
```

---

## 1️⃣ POST `/api/halls` - Tạo Hội Trường

### ✅ Valid - Basic Hall
```json
{
  "name": "Hội trường A - Tòa FPT",
  "location": "Tầng 5, Tòa nhà FPT Complex, Hà Nội",
  "capacity": 200,
  "facilities": "{\"projector\":true,\"microphone\":true,\"wifi\":true,\"airConditioner\":true,\"whiteboard\":true}"
}
```

### ✅ Valid - Large Hall
```json
{
  "name": "Grand Hall - Convention Center",
  "location": "Ground Floor, Main Building, FPT University",
  "capacity": 1000,
  "facilities": "{\"projector\":true,\"microphone\":true,\"wifi\":true,\"airConditioner\":true,\"whiteboard\":false,\"otherFacilities\":\"Stage, Sound System, LED Screen, VIP Lounge\"}"
}
```

### ✅ Valid - Small Meeting Room
```json
{
  "name": "Meeting Room 301",
  "location": "Tầng 3, Tòa A",
  "capacity": 30
}
```

### ❌ Invalid - Missing Required Fields
```json
{
  "location": "Tầng 5"
}
```
**Expected: 400 Bad Request** - "Tên hội trường là bắt buộc", "Sức chứa là bắt buộc"

### ❌ Invalid - Capacity Too Low
```json
{
  "name": "Invalid Hall",
  "capacity": 0
}
```
**Expected: 400 Bad Request** - "Sức chứa phải từ 1-10000"

---

## 2️⃣ GET `/api/halls` - Lấy Danh Sách

### Query Parameters:
```
GET /api/halls?pageNumber=1&pageSize=10&search=FPT&status=active&minCapacity=50&maxCapacity=500
```

### Examples:

**Lấy tất cả:**
```
GET /api/halls
```

**Tìm theo tên:**
```
GET /api/halls?search=Grand Hall
```

**Filter theo capacity:**
```
GET /api/halls?minCapacity=100&maxCapacity=300
```

**Filter theo status:**
```
GET /api/halls?status=active
GET /api/halls?status=maintenance
GET /api/halls?status=unavailable
```

**Phân trang:**
```
GET /api/halls?pageNumber=2&pageSize=5
```

---

## 3️⃣ GET `/api/halls/{id}` - Chi Tiết Hội Trường

```
GET /api/halls/hall-123-abc
```

**Expected Response:**
```json
{
  "success": true,
  "message": "Lấy thông tin hội trường thành công",
  "data": {
    "hallId": "hall-123-abc",
    "name": "Hội trường A",
    "location": "Tầng 5",
    "capacity": 200,
    "status": "active",
    "totalSeats": 180,
    "facilities": "{...}",
    "facilitiesParsed": {
      "projector": true,
      "microphone": true
    },
    "seats": [],
    "activeEventsCount": 2,
    "createdAt": "2024-12-08T10:00:00Z"
  }
}
```

---

## 4️⃣ PUT `/api/halls/{id}` - Cập Nhật

### ✅ Valid - Update All Fields
```json
{
  "name": "Hội trường A - Renovated",
  "location": "Tầng 5, Tòa nhà FPT Complex (Đã nâng cấp)",
  "capacity": 250,
  "facilities": "{\"projector\":true,\"microphone\":true,\"wifi\":true,\"airConditioner\":true,\"whiteboard\":true,\"otherFacilities\":\"Smart Board, Video Conference System\"}",
  "status": "active"
}
```

### ✅ Valid - Change Status to Maintenance
```json
{
  "name": "Hội trường B",
  "location": "Tầng 3",
  "capacity": 100,
  "status": "maintenance"
}
```

### ❌ Invalid - Reduce Capacity Below Seats
```json
{
  "name": "Hall A",
  "location": "Floor 5",
  "capacity": 50,
  "status": "active"
}
```
**Expected: 400 Bad Request** - "Không thể giảm sức chứa xuống dưới 180 (số ghế đã tạo)"

### ❌ Invalid - Wrong Status
```json
{
  "name": "Hall A",
  "location": "Floor 5",
  "capacity": 200,
  "status": "invalid_status"
}
```
**Expected: 400 Bad Request** - "Status không hợp lệ"

---

## 5️⃣ DELETE `/api/halls/{id}` - Xóa Hội Trường

```
DELETE /api/halls/hall-123-abc
```

**Success Response:**
```json
{
  "success": true,
  "message": "Xóa hội trường thành công",
  "data": true
}
```

**Error - Hall Has Active Events:**
```json
{
  "success": false,
  "message": "Không thể xóa hội trường vì có 3 sự kiện đang sử dụng"
}
```

---

## 6️⃣ POST `/api/halls/{id}/seats/generate` - Tạo Ghế Tự Động

### ✅ Valid - 10 hàng x 20 ghế (Regular)
```json
{
  "rows": 10,
  "seatsPerRow": 20,
  "prefix": "",
  "seatType": "regular"
}
```
**Result:** A1, A2, ..., A20, B1, B2, ..., J20 (200 ghế)

### ✅ Valid - VIP Section (3 hàng x 10 ghế)
```json
{
  "rows": 3,
  "seatsPerRow": 10,
  "prefix": "VIP-",
  "seatType": "vip"
}
```
**Result:** VIP-A1, VIP-A2, ..., VIP-C10 (30 ghế)

### ✅ Valid - Wheelchair Section
```json
{
  "rows": 2,
  "seatsPerRow": 5,
  "prefix": "WC-",
  "seatType": "wheelchair"
}
```
**Result:** WC-A1, WC-A2, ..., WC-B5 (10 ghế)

### ❌ Invalid - Too Many Seats
```json
{
  "rows": 20,
  "seatsPerRow": 50,
  "prefix": "",
  "seatType": "regular"
}
```
**Expected: 400 Bad Request** - "Tổng số ghế (1000) vượt quá sức chứa (200)"

### ❌ Invalid - Invalid Rows
```json
{
  "rows": 30,
  "seatsPerRow": 10,
  "prefix": "",
  "seatType": "regular"
}
```
**Expected: 400 Bad Request** - "Số hàng phải từ 1-26 (A-Z)"

---

## 7️⃣ GET `/api/halls/{id}/seats` - Lấy Danh Sách Ghế

### Query All Seats:
```
GET /api/halls/hall-123/seats
```

### Filter by Type:
```
GET /api/halls/hall-123/seats?seatType=vip
GET /api/halls/hall-123/seats?seatType=regular
GET /api/halls/hall-123/seats?seatType=wheelchair
```

### Filter by Active Status:
```
GET /api/halls/hall-123/seats?isActive=true
GET /api/halls/hall-123/seats?isActive=false
```

**Expected Response:**
```json
{
  "success": true,
  "message": "Lấy danh sách 200 ghế thành công",
  "data": [
    {
      "seatId": "seat-001",
      "seatCode": "A1",
      "seatRow": "A",
      "seatNumber": 1,
      "seatType": "regular",
      "isActive": true
    },
    {
      "seatId": "seat-002",
      "seatCode": "A2",
      "seatRow": "A",
      "seatNumber": 2,
      "seatType": "regular",
      "isActive": true
    }
  ]
}
```

---

## 8️⃣ POST `/api/halls/{id}/availability` - Check Trống

### ✅ Check Available Time Slot
```json
{
  "date": "2025-03-15",
  "startTime": "09:00:00",
  "endTime": "12:00:00"
}
```

**Success Response (Available):**
```json
{
  "success": true,
  "message": null,
  "data": {
    "hallId": "hall-123",
    "hallName": "Hội trường A",
    "isAvailable": true,
    "conflictingEvents": [],
    "message": "Hội trường còn trống"
  }
}
```

**Success Response (Not Available):**
```json
{
  "success": true,
  "message": null,
  "data": {
    "hallId": "hall-123",
    "hallName": "Hội trường A",
    "isAvailable": false,
    "conflictingEvents": [
      {
        "eventId": "evt-001",
        "title": "Workshop AI 2024",
        "date": "2025-03-15",
        "startTime": "10:00:00",
        "endTime": "11:30:00"
      }
    ],
    "message": "Hội trường đã được đặt bởi 1 sự kiện"
  }
}
```

### ❌ Invalid - End Time Before Start Time
```json
{
  "date": "2025-03-15",
  "startTime": "14:00:00",
  "endTime": "09:00:00"
}
```
**Expected: 400 Bad Request** - "Thời gian kết thúc phải sau thời gian bắt đầu"

---

## 📊 COMPLETE TEST WORKFLOW

### Step 1: Create Hall
```bash
POST /api/halls
Authorization: Bearer <organizer_token>

Body:
{
  "name": "Test Hall",
  "location": "Floor 5",
  "capacity": 100
}

Response: 201 Created
{
  "data": {
    "hallId": "hall-new-123"
  }
}
```

### Step 2: Generate Seats
```bash
POST /api/halls/hall-new-123/seats/generate
Authorization: Bearer <organizer_token>

Body:
{
  "rows": 5,
  "seatsPerRow": 20,
  "prefix": "",
  "seatType": "regular"
}

Response: 200 OK (100 seats created)
```

### Step 3: Get Hall Details
```bash
GET /api/halls/hall-new-123

Response: 200 OK
{
  "data": {
    "hallId": "hall-new-123",
    "totalSeats": 100,
    "seats": [...]
  }
}
```

### Step 4: Check Availability
```bash
POST /api/halls/hall-new-123/availability

Body:
{
  "date": "2025-04-01",
  "startTime": "09:00:00",
  "endTime": "17:00:00"
}

Response: 200 OK
{
  "data": {
    "isAvailable": true
  }
}
```

### Step 5: Update Hall
```bash
PUT /api/halls/hall-new-123
Authorization: Bearer <organizer_token>

Body:
{
  "name": "Test Hall - Updated",
  "location": "Floor 5 - Room 501",
  "capacity": 150,
  "status": "active"
}

Response: 200 OK
```

### Step 6: Delete Hall
```bash
DELETE /api/halls/hall-new-123
Authorization: Bearer <organizer_token>

Response: 200 OK
{
  "success": true,
  "message": "Xóa hội trường thành công"
}
```

---

## 🎯 PERMISSION TEST CASES

### ❌ Student Try to Create Hall (403 Forbidden)
```bash
POST /api/halls
Authorization: Bearer <student_token>

Body: {...}

Response: 403 Forbidden
```

### ❌ No Token (401 Unauthorized)
```bash
POST /api/halls
# No Authorization header

Body: {...}

Response: 401 Unauthorized
```

### ✅ Organizer Can Do Everything
```bash
POST/PUT/DELETE /api/halls
Authorization: Bearer <organizer_token>

Response: Success
```

---

**Created**: December 8, 2025  
**Version**: 1.0  
**Author**: GitHub Copilot
