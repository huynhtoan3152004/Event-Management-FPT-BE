# FPTU Internal Event Registration & Ticketing System  
## Backend – Validation & Flow Overview (VN)

---

## 1. Danh sách Error Message (tiếng Việt)

### 1.1. Lỗi chung (Common)

| Mã lỗi | Thông điệp tiếng Việt |
|-------|------------------------|
| COMMON_UNEXPECTED_ERROR | Đã xảy ra lỗi không mong muốn. Vui lòng thử lại sau hoặc liên hệ bộ phận hỗ trợ. |
| COMMON_FORBIDDEN | Bạn không có quyền thực hiện thao tác này. |
| COMMON_NOT_FOUND | Dữ liệu không tồn tại hoặc đã bị xóa. |
| COMMON_INVALID_INPUT | Dữ liệu gửi lên không hợp lệ. Vui lòng kiểm tra lại. |

---

### 1.2. Đăng nhập / phân quyền (Auth)

| Mã lỗi | Thông điệp tiếng Việt |
|-------|------------------------|
| AUTH_REQUIRED | Vui lòng đăng nhập để tiếp tục. |
| AUTH_INVALID_TOKEN | Phiên đăng nhập không hợp lệ hoặc đã hết hạn. Vui lòng đăng nhập lại. |
| AUTH_ROLE_NOT_ALLOWED | Tài khoản của bạn không có quyền thực hiện thao tác này. |

---

### 1.3. Hội trường (Hall / Venue)

| Mã lỗi | Thông điệp tiếng Việt |
|-------|------------------------|
| HALL_NAME_REQUIRED | Tên hội trường là bắt buộc. |
| HALL_NAME_DUPLICATED | Tên hội trường này đã tồn tại. Vui lòng chọn tên khác. |
| HALL_CAPACITY_INVALID | Sức chứa hội trường phải là số nguyên dương. |
| HALL_MAX_ROWS_INVALID | Số hàng ghế tối đa của hội trường phải là số nguyên dương. |
| HALL_MAX_SEATS_PER_ROW_INVALID | Số ghế tối đa mỗi hàng phải là số nguyên dương. |
| HALL_IN_USE_CANNOT_DELETE | Hội trường đang được sử dụng cho một hoặc nhiều sự kiện, không thể xóa. |

---

### 1.4. Sự kiện (Event)

| Mã lỗi | Thông điệp tiếng Việt |
|-------|------------------------|
| EVENT_TITLE_REQUIRED | Tiêu đề sự kiện là bắt buộc. |
| EVENT_DESCRIPTION_REQUIRED | Mô tả sự kiện là bắt buộc. |
| EVENT_DATETIME_REQUIRED | Ngày giờ bắt đầu/kết thúc sự kiện là bắt buộc. |
| EVENT_START_IN_PAST | Thời gian bắt đầu sự kiện phải ở tương lai. |
| EVENT_END_BEFORE_START | Thời gian kết thúc phải sau thời gian bắt đầu. |
| EVENT_VENUE_REQUIRED | Vui lòng chọn hội trường cho sự kiện. |
| EVENT_CAPACITY_REQUIRED | Sức chứa tối đa của sự kiện là bắt buộc. |
| EVENT_CAPACITY_INVALID | Sức chứa sự kiện phải là số nguyên dương. |
| EVENT_CAPACITY_EXCEED_HALL | Sức chứa sự kiện không được vượt quá sức chứa hội trường (tối đa {{maxCapacity}} chỗ). |
| EVENT_REGISTRATION_DEADLINE_INVALID | Thời hạn đăng ký phải nằm trước thời gian bắt đầu sự kiện. |
| EVENT_CAPACITY_LESS_THAN_REGISTERED | Sức chứa mới không được nhỏ hơn số lượng người đã đăng ký ({{registeredCount}} người). |
| EVENT_SCHEDULE_CONFLICT_SAME_HALL | Hội trường đã được đặt cho một sự kiện khác trong khoảng thời gian này. |
| EVENT_SCHEDULE_NEED_5_HOURS_GAP | Giữa hai sự kiện trong cùng hội trường phải cách nhau ít nhất 5 giờ. |
| EVENT_NOT_FOUND | Sự kiện không tồn tại hoặc đã bị xóa. |
| EVENT_ALREADY_CANCELLED | Sự kiện đã bị hủy trước đó. |
| EVENT_HAS_REGISTRATIONS_CANNOT_DELETE | Sự kiện đã có người đăng ký, không thể xóa. Vui lòng hủy sự kiện thay vì xóa. |
| EVENT_CANCEL_SUCCESS | Sự kiện đã được hủy. Tất cả người đăng ký sẽ được thông báo. |
| EVENT_IS_CANCELLED | Sự kiện này đã bị hủy, không thể đăng ký hoặc check-in. |

---

### 1.5. Cấu hình ghế cho sự kiện (Event Seat Config)

Giả định: Hall có `maxRows`, `maxSeatsPerRow`. Organizer cấu hình cho từng event: `numberOfRows`, `seatsPerRow`.

| Mã lỗi | Thông điệp tiếng Việt |
|-------|------------------------|
| SEAT_CONFIG_ROWS_REQUIRED | Vui lòng nhập số hàng ghế cho sự kiện. |
| SEAT_CONFIG_SEATS_PER_ROW_REQUIRED | Vui lòng nhập số ghế trên mỗi hàng cho sự kiện. |
| SEAT_CONFIG_ROWS_INVALID | Số hàng ghế phải là số nguyên dương. |
| SEAT_CONFIG_SEATS_PER_ROW_INVALID | Số ghế trên mỗi hàng phải là số nguyên dương. |
| SEAT_CONFIG_ROWS_EXCEED_HALL | Số hàng ghế không được vượt quá số hàng tối đa của hội trường (tối đa {{hallMaxRows}} hàng). |
| SEAT_CONFIG_SEATS_PER_ROW_EXCEED_HALL | Số ghế mỗi hàng không được vượt quá giới hạn hội trường (tối đa {{hallMaxSeatsPerRow}} ghế/hàng). |
| SEAT_CONFIG_TOTAL_SEATS_LESS_THAN_CAPACITY | Tổng số ghế ({{totalSeats}}) không được nhỏ hơn sức chứa sự kiện ({{eventCapacity}}). |
| SEAT_CONFIG_TOTAL_SEATS_EXCEED_HALL_CAPACITY | Tổng số ghế cấu hình vượt quá sức chứa tối đa của hội trường. |
| SEAT_GENERATION_FAILED | Lỗi khi sinh danh sách ghế cho sự kiện. Vui lòng thử lại sau. |

---

### 1.6. Ghế & chọn chỗ (Seats & Seat Selection)

| Mã lỗi | Thông điệp tiếng Việt |
|-------|------------------------|
| SEAT_NOT_FOUND | Ghế không tồn tại. |
| SEAT_NOT_BELONG_TO_EVENT | Ghế này không thuộc sự kiện hiện tại. |
| SEAT_ALREADY_BOOKED | Ghế này đã được người khác đặt, vui lòng chọn ghế khác. |
| SEAT_INVALID_STATE | Trạng thái ghế không hợp lệ, vui lòng tải lại trang và thử lại. |

---

### 1.7. Đăng ký & vé (Registration / Ticket)

| Mã lỗi | Thông điệp tiếng Việt |
|-------|------------------------|
| REG_EVENT_FULL | Đăng ký thất bại: Sự kiện đã hết chỗ. |
| REG_EVENT_CLOSED | Đăng ký thất bại: Sự kiện đã đóng đăng ký. |
| REG_EVENT_CANCELLED | Đăng ký thất bại: Sự kiện đã bị hủy. |
| REG_ALREADY_REGISTERED | Bạn đã đăng ký sự kiện này rồi. |
| REG_STUDENT_TIME_CONFLICT | Bạn đã đăng ký một sự kiện khác trùng thời gian. Vui lòng hủy đăng ký cũ trước khi đăng ký sự kiện này. |
| REG_SEAT_REQUIRED | Vui lòng chọn ghế trước khi xác nhận đăng ký. |
| REG_TICKET_CREATE_FAILED | Đăng ký thất bại do lỗi hệ thống. Vui lòng thử lại sau. |
| REG_NOT_FOUND | Đăng ký/vé không tồn tại hoặc đã bị hủy. |

---

### 1.8. Hủy đăng ký (Cancellation)

| Mã lỗi | Thông điệp tiếng Việt |
|-------|------------------------|
| CANCEL_NOT_OWNER | Bạn không thể hủy đăng ký này vì không thuộc về tài khoản của bạn. |
| CANCEL_EVENT_STARTED | Bạn không thể hủy vì sự kiện đã bắt đầu. |
| CANCEL_AFTER_CUTOFF | Bạn chỉ có thể hủy đăng ký trước ít nhất {{minHours}} giờ so với giờ bắt đầu sự kiện. |
| CANCEL_ALREADY_CANCELLED | Đăng ký này đã được hủy trước đó. |
| CANCEL_SUCCESS | Hủy đăng ký thành công. Ghế của bạn đã được trả lại cho hệ thống. |

---

### 1.9. Vé QR & Check-in

| Mã lỗi | Thông điệp tiếng Việt |
|-------|------------------------|
| QR_INVALID | Mã QR vé không hợp lệ. |
| QR_TICKET_NOT_FOUND | Không tìm thấy vé tương ứng với mã QR này. |
| QR_EVENT_MISMATCH | Vé này không thuộc sự kiện hiện tại. |
| QR_EVENT_CANCELLED | Sự kiện này đã bị hủy, vé không còn giá trị. |
| QR_TICKET_ALREADY_USED | Vé này đã được sử dụng để check-in trước đó. |
| QR_TICKET_CANCELLED | Vé này đã bị hủy, không thể check-in. |
| QR_CHECKIN_CLOSED | Check-in đã kết thúc. Vui lòng liên hệ ban tổ chức. |
| QR_CHECKIN_SUCCESS | Check-in thành công. Chúc bạn tham gia sự kiện vui vẻ! |

---

### 1.10. Báo cáo (Reporting)

| Mã lỗi | Thông điệp tiếng Việt |
|-------|------------------------|
| REPORT_FORBIDDEN | Bạn không có quyền xem báo cáo của sự kiện này. |
| REPORT_NO_DATA | Không có dữ liệu phù hợp với tiêu chí tìm kiếm. |

---

## 2. Tổng quan luồng backend (cho dev)

### 2.1. Domain chính (gợi ý)

- **User**: `UserId`, `Role` (STUDENT / ORGANIZER / STAFF), `Name`, `Email`, `StudentCode`, …
- **Hall**: `HallId`, `Name`, `Location`, `MaxCapacity`, `MaxRows`, `MaxSeatsPerRow`, `Status`
- **Event**: `EventId`, `Title`, `Description`, `HallId`, `StartTime`, `EndTime`, `Capacity`, `RegistrationDeadline`, `Status` (DRAFT/PUBLISHED/CANCELLED), `OrganizerId`
- **EventSeatConfig**: `EventId`, `NumberOfRows`, `SeatsPerRow`
- **Seat (EventSeat)**: `SeatId`, `EventId`, `RowNumber`, `SeatNumber`, `Status` (AVAILABLE/BOOKED/LOCKED)
- **Registration/Ticket**: `RegistrationId`, `EventId`, `UserId`, `SeatId`, `QrCode`, `Status` (REGISTERED/CANCELLED/CHECKED_IN), `CreatedAt`, `CancelledAt`, `CheckinAt`

---

### 2.2. Luồng 1 – Organizer CRUD Event + Hall + Seat Config

#### 2.2.1. Tạo sự kiện (Create Event)

1. **Input**:  
   - Title, Description  
   - StartTime, EndTime  
   - HallId  
   - Capacity  
   - RegistrationDeadline (optional)  

2. **Validate** (trả về ErrorMessage ở trên nếu fail):  
   - Bắt buộc: `Title`, `StartTime`, `EndTime`, `HallId`, `Capacity`  
     - `EVENT_TITLE_REQUIRED`  
     - `EVENT_DATETIME_REQUIRED`  
     - `EVENT_VENUE_REQUIRED`  
     - `EVENT_CAPACITY_REQUIRED`  

   - Thời gian:  
     - `StartTime > now` → nếu không: `EVENT_START_IN_PAST`  
     - `EndTime > StartTime` → nếu không: `EVENT_END_BEFORE_START`  
     - Nếu có `RegistrationDeadline` → phải `< StartTime` → nếu không: `EVENT_REGISTRATION_DEADLINE_INVALID`  

   - Capacity:  
     - `Capacity > 0` → nếu không: `EVENT_CAPACITY_INVALID`  
     - Lấy `Hall.MaxCapacity` → nếu `Capacity > MaxCapacity`: `EVENT_CAPACITY_EXCEED_HALL`  

   - Xung đột lịch trong cùng Hall:  
     - Tìm các event cùng `HallId` có `Status` ∈ (PUBLISHED, DRAFT?) chưa bị cancel  
     - Check overlap time:  
       - Nếu trùng khoảng thời gian: `EVENT_SCHEDULE_CONFLICT_SAME_HALL`  
       - Nếu cùng ngày: khoảng cách giữa (EndTime của event A) và (StartTime của event B) < 5 giờ: `EVENT_SCHEDULE_NEED_5_HOURS_GAP`  

3. **Lưu DB**:  
   - Nếu tất cả OK → insert Event với `Status = DRAFT` hoặc `PUBLISHED` (theo nghiệp vụ)  

---

#### 2.2.2. Cấu hình ghế cho sự kiện (Event Seat Config + Generate Seats)

1. **Input**:  
   - `EventId`, `NumberOfRows`, `SeatsPerRow`  

2. **Validate**:  
   - Bắt buộc: `NumberOfRows`, `SeatsPerRow` → nếu thiếu:  
     - `SEAT_CONFIG_ROWS_REQUIRED` / `SEAT_CONFIG_SEATS_PER_ROW_REQUIRED`  
   - Phải là số nguyên dương:  
     - Nếu không: `SEAT_CONFIG_ROWS_INVALID` / `SEAT_CONFIG_SEATS_PER_ROW_INVALID`  

   - Lấy thông tin Hall của Event: `Hall.MaxRows`, `Hall.MaxSeatsPerRow`, `Hall.MaxCapacity`  

   - `NumberOfRows <= Hall.MaxRows` → nếu không: `SEAT_CONFIG_ROWS_EXCEED_HALL`  
   - `SeatsPerRow <= Hall.MaxSeatsPerRow` → nếu không: `SEAT_CONFIG_SEATS_PER_ROW_EXCEED_HALL`  

   - `TotalSeats = NumberOfRows * SeatsPerRow`  
     - `TotalSeats <= Hall.MaxCapacity` → nếu không: `SEAT_CONFIG_TOTAL_SEATS_EXCEED_HALL_CAPACITY`  
     - `TotalSeats >= Event.Capacity` (đảm bảo đủ ghế cho capacity event) → nếu không: `SEAT_CONFIG_TOTAL_SEATS_LESS_THAN_CAPACITY`  

3. **Sinh danh sách ghế (generate seats)**:  
   - Loop `row = 1..NumberOfRows`  
   - Loop `seat = 1..SeatsPerRow`  
     - Insert `EventSeat(EventId, RowNumber=row, SeatNumber=seat, Status=AVAILABLE)`  

4. **Xử lý lỗi hệ thống**:  
   - Nếu xảy ra lỗi DB trong lúc generate → rollback, trả về `SEAT_GENERATION_FAILED`  

---

#### 2.2.3. Cập nhật sự kiện (Update Event)

1. Load `Event` theo `EventId` → nếu không có: `EVENT_NOT_FOUND`  
2. Kiểm tra quyền: Organizer phải là owner hoặc Staff → nếu không: `COMMON_FORBIDDEN`  
3. Check trạng thái:  
   - Nếu event đã `CANCELLED` → không cho sửa: `EVENT_ALREADY_CANCELLED`  

4. Validate các field tương tự **Create**:  
   - Thời gian, capacity, hall conflict, etc.  

5. Validate capacity khi đã có người đăng ký:  
   - Lấy `RegisteredCount` = số registration status = REGISTERED  
   - Nếu `NewCapacity < RegisteredCount` → `EVENT_CAPACITY_LESS_THAN_REGISTERED`  

6. Lưu thay đổi nếu tất cả OK.  

---

#### 2.2.4. Xóa / Hủy sự kiện (Delete / Cancel Event)

1. Load event: nếu không có → `EVENT_NOT_FOUND`  
2. Check quyền: Organizer của event hoặc Staff → nếu không → `COMMON_FORBIDDEN`  

- **Delete cứng**:  
  - Đếm số registration với event này (status ≠ CANCELLED):  
    - Nếu > 0 → `EVENT_HAS_REGISTRATIONS_CANNOT_DELETE`  
    - Nếu = 0 → cho delete  

- **Cancel**:  
  - Nếu status đã CANCELLED → `EVENT_ALREADY_CANCELLED`  
  - Set status = CANCELLED  
  - (Business khác như gửi email có thể xử lý async)  
  - Trả về `EVENT_CANCEL_SUCCESS`  

---

### 2.3. Luồng 2 – Student duyệt & đăng ký sự kiện

#### 2.3.1. Browse event list

- Backend trả danh sách event:  
  - `Status = PUBLISHED`  
  - `StartTime > now` (hoặc show cả upcoming & closing soon)  

---

#### 2.3.2. Đăng ký (Register Event)

1. **Pre-check**:  
   - User phải đăng nhập → nếu không: `AUTH_REQUIRED`  
   - Role: phải là STUDENT → nếu không: `AUTH_ROLE_NOT_ALLOWED`  

2. **Kiểm tra event**:  
   - Event tồn tại & `Status = PUBLISHED` → nếu không: `EVENT_NOT_FOUND` hoặc `EVENT_IS_CANCELLED`  
   - `StartTime > now` → nếu không: `REG_EVENT_CLOSED`  
   - Nếu có `RegistrationDeadline` và `now > deadline` → `REG_EVENT_CLOSED`  

3. **Kiểm tra capacity**:  
   - Lấy `RegisteredCount` (số vé status = REGISTERED)  
   - Nếu `RegisteredCount >= Event.Capacity` → `REG_EVENT_FULL`  

4. **Kiểm tra sinh viên đã đăng ký event này chưa**:  
   - Nếu đã có registration (status = REGISTERED) → `REG_ALREADY_REGISTERED`  

5. **Kiểm tra trùng thời gian với các event khác sinh viên đã đăng ký**:  
   - Lấy tất cả registration của user có status = REGISTERED  
   - Join sang Event, check trùng khoảng thời gian (overlap Start/End)  
   - Nếu trùng bất kỳ event nào → `REG_STUDENT_TIME_CONFLICT`  

6. **Chọn ghế**:  
   - Client gửi `SeatId` mà user chọn  
   - Backend:  
     - Check `Seat` tồn tại và `Seat.EventId = EventId` → nếu không: `SEAT_NOT_BELONG_TO_EVENT`  
     - Check `Seat.Status = AVAILABLE` → nếu không: `SEAT_ALREADY_BOOKED`  

7. **Tạo registration + cập nhật seat** (transaction):  
   - Insert `Registration` với `Status = REGISTERED`  
   - Generate `QrCode` (string) & lưu  
   - Update `Seat.Status = BOOKED`  

8. **Nếu mọi thứ OK**: trả về thông tin vé + QR code.  
   - Nếu lỗi hệ thống: `REG_TICKET_CREATE_FAILED`  

---

### 2.4. Luồng 3 – Student hủy đăng ký

1. User đăng nhập (STUDENT) → nếu không: `AUTH_REQUIRED`  
2. Load `Registration` theo `RegistrationId` hoặc (`EventId`, `UserId`)  
   - Nếu không có → `REG_NOT_FOUND`  

3. Kiểm tra chủ sở hữu:  
   - Nếu `Registration.UserId != currentUser` → `CANCEL_NOT_OWNER`  

4. Kiểm tra trạng thái:  
   - Nếu `Registration.Status = CANCELLED` → `CANCEL_ALREADY_CANCELLED`  

5. Kiểm tra thời gian:  
   - Lấy `Event.StartTime`  
   - Nếu `now >= Event.StartTime` → `CANCEL_EVENT_STARTED`  
   - Nếu cấu hình `minHoursBeforeCancel` (ví dụ 24 giờ):  
     - Nếu `Event.StartTime - now < minHoursBeforeCancel` → `CANCEL_AFTER_CUTOFF`  

6. Hủy đăng ký (transaction):  
   - Update `Registration.Status = CANCELLED`, `CancelledAt = now`  
   - Đổi `Seat.Status` từ `BOOKED` về `AVAILABLE`  

7. Trả về `CANCEL_SUCCESS`.  

---

### 2.5. Luồng 4 – Staff check-in bằng QR

1. Staff đăng nhập (ROLE = STAFF hoặc ORGANIZER liên quan event)  
   - Nếu không → `AUTH_REQUIRED`  
   - Nếu không có quyền với event → `COMMON_FORBIDDEN`  

2. Staff mở màn check-in cho **một Event cụ thể**.  
   - Gửi lên `EventId`  

3. Khi scan QR, client gửi lên `QrCode` (string).  

4. Backend xử lý:

   1. Tìm `Registration` theo `QrCode`.  
      - Không thấy → `QR_TICKET_NOT_FOUND` hoặc `QR_INVALID`  

   2. Kiểm tra Event:  
      - Nếu `Registration.EventId != EventId đang check-in` → `QR_EVENT_MISMATCH`  
      - Load Event: nếu `Status = CANCELLED` → `QR_EVENT_CANCELLED`  
      - Nếu `now > Event.EndTime` → có thể xem là `QR_CHECKIN_CLOSED`  

   3. Kiểm tra trạng thái vé:  
      - Nếu `Status = CANCELLED` → `QR_TICKET_CANCELLED`  
      - Nếu `Status = CHECKED_IN` → `QR_TICKET_ALREADY_USED`  

5. Nếu tất cả OK:  
   - Update `Registration.Status = CHECKED_IN`, `CheckinAt = now`  
   - Trả về `QR_CHECKIN_SUCCESS`  

---

### 2.6. Luồng 5 – Reporting (chỉ hiển thị, không xuất Excel)

1. **Event Organizer – xem báo cáo event của mình**  
   - Input: `EventId`  
   - Backend:  
     - Check quyền: `Event.OrganizerId == currentUserId` hoặc Staff  
       - Nếu không: `REPORT_FORBIDDEN`  
     - Tính:  
       - `TotalRegistered` = count `Registration.Status = REGISTERED OR CHECKED_IN`  
       - `TotalAttended` = count `Registration.Status = CHECKED_IN`  
       - `AttendanceRate` = nếu `TotalRegistered > 0` thì `TotalAttended / TotalRegistered * 100`, ngược lại = 0 hoặc N/A  

   - Output JSON cho FE render (table/list).  

2. **Staff – tổng hợp**  
   - Filter: theo khoảng thời gian, organizer, hall, v.v.  
   - Nếu không có event nào → `REPORT_NO_DATA`  

3. Không yêu cầu xuất Excel:  
   - Backend chỉ cần trả JSON  
   - FE có thể render bảng hoặc biểu đồ ngay trên UI  

---

### 2.7. Ghi log lỗi validate (gợi ý)

- Mỗi khi validate thất bại, ngoài việc trả ErrorMessage cho client, backend nên log lại:  
  - `Timestamp`  
  - `UserId` (nếu có)  
  - `Endpoint` / `Action`  
  - `Payload` (đã được mask thông tin nhạy cảm)  
  - `ErrorCode` (ví dụ: `REG_EVENT_FULL`)  
  - `ErrorMessage`  

- Lợi ích:  
  - Dễ đối chiếu khi QA/test thử các case  
  - Dễ debug khi production có lỗi business  
  - Dùng để thống kê loại lỗi validate nào xuất hiện nhiều nhất  

---

> Toàn bộ nội dung trên có thể lưu nguyên file này với tên ví dụ:  
> **`backend-validation-and-flows.md`**  
> để dùng làm tài liệu chung cho backend & QA khi implement và viết test case.
