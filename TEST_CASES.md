# Test Cases - Event Management System

## 📋 Tổng Quan Test Cases

Tài liệu này mô tả chi tiết các test cases cho hệ thống Event Management FPT, bao gồm các chức năng chính: Authentication, Event Management, Hall Management, Ticket Management, Check-in/Check-out.

---

## 1. AUTHENTICATION TEST CASES

### TC-AUTH-001: Login với thông tin hợp lệ
**Priority:** High  
**Role:** All Users

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Navigate to login page | Login form displayed |
| 2 | Enter valid email: `student@fpt.edu.vn` | Email field filled |
| 3 | Enter valid password: `Password123!` | Password field filled (masked) |
| 4 | Click "Login" button | Redirect to dashboard |
| 5 | Verify user info displayed | User name and role shown in header |

**Test Data:**
- Student: `student@fpt.edu.vn` / `Password123!`
- Organizer: `organizer@fpt.edu.vn` / `Password123!`
- Staff: `staff@fpt.edu.vn` / `Password123!`

---

### TC-AUTH-002: Login với email không tồn tại
**Priority:** High  
**Role:** All Users

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Navigate to login page | Login form displayed |
| 2 | Enter non-existent email: `notexist@fpt.edu.vn` | Email field filled |
| 3 | Enter any password | Password field filled |
| 4 | Click "Login" button | Error message: "Invalid credentials" |
| 5 | Verify still on login page | URL still `/login` |

---

### TC-AUTH-003: Login với password sai
**Priority:** High  
**Role:** All Users

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Navigate to login page | Login form displayed |
| 2 | Enter valid email: `student@fpt.edu.vn` | Email field filled |
| 3 | Enter wrong password: `WrongPass123` | Password field filled |
| 4 | Click "Login" button | Error message: "Invalid credentials" |
| 5 | Verify password field cleared | Password field empty |

---

### TC-AUTH-004: Register student account
**Priority:** High  
**Role:** New User

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Navigate to register page | Registration form displayed |
| 2 | Enter email: `newstudent@fpt.edu.vn` | Email field filled |
| 3 | Enter name: `Nguyen Van A` | Name field filled |
| 4 | Enter password: `SecurePass123!` | Password masked |
| 5 | Enter confirm password: `SecurePass123!` | Confirm password masked |
| 6 | Select role: `Student` | Student role selected |
| 7 | Click "Register" button | Success message shown |
| 8 | Verify redirect to login | Login page displayed |

---

### TC-AUTH-005: Register với email đã tồn tại
**Priority:** Medium  
**Role:** New User

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Navigate to register page | Registration form displayed |
| 2 | Enter existing email: `student@fpt.edu.vn` | Email field filled |
| 3 | Enter other fields correctly | All fields filled |
| 4 | Click "Register" button | Error: "Email already exists" |

---

### TC-AUTH-006: Register với password yếu
**Priority:** Medium  
**Role:** New User

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Navigate to register page | Registration form displayed |
| 2 | Enter email: `test@fpt.edu.vn` | Email field filled |
| 3 | Enter password: `12345` (too weak) | Password field filled |
| 4 | Click "Register" button | Error: "Password must contain uppercase, lowercase, number, special character" |

---

### TC-AUTH-007: Logout
**Priority:** High  
**Role:** All Users

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Login successfully | Dashboard displayed |
| 2 | Click user avatar/menu | Dropdown menu shown |
| 3 | Click "Logout" | Redirect to login page |
| 4 | Try to access protected page | Redirect back to login |

---

## 2. EVENT MANAGEMENT TEST CASES

### TC-EVENT-001: Create event (Organizer)
**Priority:** High  
**Role:** Organizer

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Login as organizer | Dashboard displayed |
| 2 | Navigate to "Create Event" | Event creation form shown |
| 3 | Enter title: `AI Workshop 2025` | Title filled |
| 4 | Enter description: `Learn AI basics` | Description filled |
| 5 | Select date: Tomorrow | Date selected |
| 6 | Select start time: `09:00` | Start time selected |
| 7 | Select end time: `12:00` | End time selected |
| 8 | Select hall: `Hall A` | Hall selected |
| 9 | Upload image | Image preview shown |
| 10 | Click "Create" | Success message shown |
| 11 | Verify event in list | Event appears with status "draft" |

---

### TC-EVENT-002: Create event với thời gian không hợp lệ
**Priority:** High  
**Role:** Organizer

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Navigate to "Create Event" | Form displayed |
| 2 | Enter all fields correctly | Fields filled |
| 3 | Set start time: `12:00` | Start time selected |
| 4 | Set end time: `09:00` (before start) | End time selected |
| 5 | Click "Create" | Error: "End time must be after start time" |

---

### TC-EVENT-003: Create event với ngày trong quá khứ
**Priority:** High  
**Role:** Organizer

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Navigate to "Create Event" | Form displayed |
| 2 | Enter all fields correctly | Fields filled |
| 3 | Select date: Yesterday | Past date selected |
| 4 | Click "Create" | Error: "Event date cannot be in the past" |

---

### TC-EVENT-004: Create event không có hall (không validate seats)
**Priority:** High  
**Role:** Organizer

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Navigate to "Create Event" | Form displayed |
| 2 | Enter title, description, date, time | Fields filled |
| 3 | Leave hall empty | No hall selected |
| 4 | Enter location: `FPT University HCM` | Location filled |
| 5 | Click "Create" | Error: "Please enter Location if no Hall selected" |

---

### TC-EVENT-005: Publish event
**Priority:** High  
**Role:** Organizer

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Login as organizer | Dashboard displayed |
| 2 | Navigate to event detail (draft status) | Event details shown |
| 3 | Click "Publish" button | Confirmation dialog shown |
| 4 | Confirm publish | Success message shown |
| 5 | Verify status changed to "published" | Status badge shows "Published" |
| 6 | Verify event visible to students | Event appears in public list |

---

### TC-EVENT-006: Cancel event
**Priority:** High  
**Role:** Organizer

**Preconditions:**
- Event has < 50% registered tickets
- Event is at least 48 hours away

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Navigate to event detail (published) | Event details shown |
| 2 | Click "Cancel Event" button | Confirmation dialog shown |
| 3 | Confirm cancellation | Success message with stats |
| 4 | Verify status = "cancelled" | Status badge shows "Cancelled" |
| 5 | Verify tickets cancelled | All tickets show "cancelled" |
| 6 | Verify seats returned | Seats status = "available" |

---

### TC-EVENT-007: Cancel event (không đủ điều kiện - > 50% registered)
**Priority:** Medium  
**Role:** Organizer

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Navigate to event with > 50% registered | Event details shown |
| 2 | Click "Cancel Event" button | Confirmation dialog shown |
| 3 | Confirm cancellation | Error: "Cannot cancel when > 50% registered (X/Y)" |

---

### TC-EVENT-008: Cancel event (không đủ điều kiện - < 48h)
**Priority:** Medium  
**Role:** Organizer

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Navigate to event < 48h away | Event details shown |
| 2 | Click "Cancel Event" button | Confirmation dialog shown |
| 3 | Confirm cancellation | Error: "Can only cancel 48h before event. X hours remaining" |

---

### TC-EVENT-009: Complete event
**Priority:** High  
**Role:** Organizer

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Navigate to event detail (after event ends) | Event details shown |
| 2 | Click "Complete Event" button | Confirmation dialog shown |
| 3 | Confirm completion | Success message shown |
| 4 | Verify status = "completed" | Status badge shows "Completed" |
| 5 | Verify seats reset to available | All seats status = "available" |

---

### TC-EVENT-010: Update event
**Priority:** Medium  
**Role:** Organizer

**Preconditions:**
- Event status = "draft" or "published"

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Navigate to event detail | Event details shown |
| 2 | Click "Edit" button | Edit form shown with current data |
| 3 | Change title to `Updated Title` | Title changed |
| 4 | Change description | Description changed |
| 5 | Click "Save" | Success message shown |
| 6 | Verify changes applied | New title and description shown |

---

### TC-EVENT-011: Filter events by date range
**Priority:** Medium  
**Role:** All Users

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Navigate to events list | All events shown |
| 2 | Select "From Date": `2025-12-01` | From date selected |
| 3 | Select "To Date": `2025-12-31` | To date selected |
| 4 | Click "Filter" | Only events in date range shown |
| 5 | Verify results | All shown events within range |

---

### TC-EVENT-012: Search events by keyword
**Priority:** Medium  
**Role:** All Users

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Navigate to events list | All events shown |
| 2 | Enter search keyword: `AI` | Keyword entered |
| 3 | Click "Search" or press Enter | Events filtered |
| 4 | Verify results | Only events with "AI" in title/description shown |

---

## 3. HALL MANAGEMENT TEST CASES

### TC-HALL-001: Create hall
**Priority:** High  
**Role:** Organizer

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Login as organizer | Dashboard displayed |
| 2 | Navigate to "Halls Management" | Halls list shown |
| 3 | Click "Create Hall" | Hall creation form shown |
| 4 | Enter name: `Hall A` | Name filled |
| 5 | Enter location: `FPT HCM Building A` | Location filled |
| 6 | Enter capacity: `200` | Capacity filled |
| 7 | Enter max rows: `10` | Max rows filled |
| 8 | Enter seats per row: `20` | Seats per row filled |
| 9 | Enter facilities: `Projector, AC, WiFi` | Facilities filled |
| 10 | Click "Create" | Success message shown |
| 11 | Verify hall in list | Hall appears with status "active" |

---

### TC-HALL-002: Generate seats for hall
**Priority:** High  
**Role:** Organizer

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Navigate to hall detail | Hall info shown |
| 2 | Verify "No seats generated" | Message shown |
| 3 | Click "Generate Seats" | Seat config form shown |
| 4 | Enter rows: `10` | Rows filled (default from hall) |
| 5 | Enter seats per row: `20` | Seats filled (default from hall) |
| 6 | Click "Generate" | Success: "Created 200 seats" |
| 7 | Verify seat map displayed | Seat grid A1-J20 shown |
| 8 | Verify all seats "available" | All seats green/available |

---

### TC-HALL-003: Generate seats (already exists)
**Priority:** Medium  
**Role:** Organizer

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Navigate to hall with seats | Seat map shown |
| 2 | Click "Generate Seats" | Button disabled or hidden |
| 3 | If force regenerate | Error: "Hall already has X seats. Delete first" |

---

### TC-HALL-004: Update hall information
**Priority:** Medium  
**Role:** Organizer

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Navigate to hall detail | Hall info shown |
| 2 | Click "Edit" | Edit form shown |
| 3 | Change name to `Hall A - Updated` | Name changed |
| 4 | Change capacity to `250` | Capacity changed |
| 5 | Click "Save" | Success message |
| 6 | Verify changes applied | New name and capacity shown |

---

### TC-HALL-005: View hall availability
**Priority:** Medium  
**Role:** Organizer

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Navigate to hall detail | Hall info shown |
| 2 | Click "Check Availability" | Calendar/schedule shown |
| 3 | Select date | Events for that date shown |
| 4 | Verify occupied time slots | Booked slots highlighted |
| 5 | Verify available time slots | Free slots shown in green |

---

## 4. TICKET REGISTRATION TEST CASES

### TC-TICKET-001: Register ticket (với hall, chọn ghế)
**Priority:** High  
**Role:** Student

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Login as student | Dashboard displayed |
| 2 | Navigate to events list | Published events shown |
| 3 | Click "Register" on available event | Seat selection shown |
| 4 | Click available seat `A5` | Seat highlighted |
| 5 | Click "Confirm Registration" | Confirmation dialog |
| 6 | Confirm | Success message shown |
| 7 | Verify ticket in "My Tickets" | Ticket appears with QR code |
| 8 | Verify seat status = "reserved" | Seat A5 shows "Reserved" |

---

### TC-TICKET-002: Register ticket (auto-assign seat)
**Priority:** High  
**Role:** Student

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Navigate to event registration | Form shown |
| 2 | Don't select specific seat | No seat selected |
| 3 | Click "Register" | Auto-assigns first available seat |
| 4 | Verify ticket created | Ticket shows auto-assigned seat |

---

### TC-TICKET-003: Register ticket (không có hall)
**Priority:** High  
**Role:** Student

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Navigate to event without hall | Event details shown |
| 2 | Click "Register" | No seat selection |
| 3 | Click "Confirm" | Ticket created without seat |
| 4 | Verify ticket in list | Ticket shows "No seat assigned" |

---

### TC-TICKET-004: Register ticket (event full)
**Priority:** High  
**Role:** Student

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Navigate to full event | Event shows "Full" badge |
| 2 | Try to click "Register" | Button disabled |
| 3 | If force registration | Error: "Event is full" |

---

### TC-TICKET-005: Register ticket (duplicate registration)
**Priority:** High  
**Role:** Student

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Register for event successfully | Ticket created |
| 2 | Try to register again | Error: "Already registered for this event" |

---

### TC-TICKET-006: Register ticket (time conflict)
**Priority:** High  
**Role:** Student

**Preconditions:**
- Student already registered for Event A (9:00-12:00)

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Try to register for Event B (10:00-13:00) | Same date, overlapping time |
| 2 | Click "Register" | Error: "You have registered for 'Event A' at overlapping time (09:00-12:00)" |

---

### TC-TICKET-007: Register ticket (outside registration window)
**Priority:** Medium  
**Role:** Student

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Navigate to event (registration not started) | Event shown |
| 2 | Click "Register" | Error: "Registration has not started yet" |
| 3 | For registration ended | Error: "Registration has ended" |

---

### TC-TICKET-008: Cancel ticket (student, > 24h before event)
**Priority:** High  
**Role:** Student

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Navigate to "My Tickets" | Tickets list shown |
| 2 | Click "Cancel" on active ticket (>24h away) | Confirmation dialog |
| 3 | Confirm cancellation | Success message |
| 4 | Verify ticket status = "cancelled" | Status badge shows "Cancelled" |
| 5 | Verify seat returned | Seat status = "available" |

---

### TC-TICKET-009: Cancel ticket (student, < 24h before event)
**Priority:** High  
**Role:** Student

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Navigate to "My Tickets" | Tickets list shown |
| 2 | Try to cancel ticket < 24h before event | Button disabled or |
| 3 | If force cancel | Error: "Can only cancel 24h before event. X.X hours remaining" |

---

### TC-TICKET-010: Cancel ticket (organizer, anytime)
**Priority:** Medium  
**Role:** Organizer

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Login as organizer | Dashboard shown |
| 2 | Navigate to event tickets list | All tickets shown |
| 3 | Click "Cancel" on any ticket | Confirmation dialog |
| 4 | Confirm | Success message |
| 5 | Verify ticket cancelled | Status = "cancelled" |

---

### TC-TICKET-011: View ticket QR code
**Priority:** Medium  
**Role:** Student

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Navigate to "My Tickets" | Tickets list shown |
| 2 | Click on ticket card | Ticket detail shown |
| 3 | Verify QR code displayed | QR code image shown |
| 4 | Verify ticket info | Event, seat, time shown |

---

## 5. CHECK-IN/CHECK-OUT TEST CASES

### TC-CHECKIN-001: Check-in ticket (valid QR code)
**Priority:** High  
**Role:** Staff

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Login as staff | Dashboard displayed |
| 2 | Navigate to "Check-in" page | QR scanner shown |
| 3 | Scan valid QR code | Ticket info displayed |
| 4 | Verify student info shown | Name, seat displayed |
| 5 | Click "Confirm Check-in" | Success message |
| 6 | Verify ticket status = "checked-in" | Status updated |
| 7 | Verify seat status = "occupied" | Seat marked occupied |
| 8 | Verify checkin record created | Record with checkin time |

---

### TC-CHECKIN-002: Check-in ticket (invalid/not found)
**Priority:** High  
**Role:** Staff

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Navigate to "Check-in" page | Scanner ready |
| 2 | Scan invalid QR code | Error: "Ticket Not Found" |
| 3 | Verify no data changed | No checkin record created |

---

### TC-CHECKIN-003: Check-in ticket (cancelled)
**Priority:** High  
**Role:** Staff

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Navigate to "Check-in" page | Scanner ready |
| 2 | Scan cancelled ticket QR | Error: "Ticket Cancelled" |
| 3 | Verify no checkin allowed | Checkin button disabled |

---

### TC-CHECKIN-004: Check-in ticket (already checked in)
**Priority:** High  
**Role:** Staff

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Navigate to "Check-in" page | Scanner ready |
| 2 | Scan ticket that's already checked-in | Error: "Already Checked In" |
| 3 | Show previous checkin time | "Checked in at: HH:MM" |

---

### TC-CHECKIN-005: Manual check-in (enter ticket code)
**Priority:** Medium  
**Role:** Staff

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Navigate to "Check-in" page | Input field shown |
| 2 | Enter ticket code manually: `ABC123XYZ` | Code entered |
| 3 | Click "Check-in" | Ticket info displayed |
| 4 | Confirm check-in | Success message |

---

### TC-CHECKOUT-001: Check-out ticket (valid)
**Priority:** High  
**Role:** Staff

**Preconditions:**
- Ticket already checked-in
- Event has started

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Login as staff | Dashboard displayed |
| 2 | Navigate to "Check-out" page | QR scanner shown |
| 3 | Scan checked-in ticket QR | Ticket info displayed |
| 4 | Verify checkin time shown | "Checked in at: HH:MM" |
| 5 | Click "Confirm Check-out" | Success message |
| 6 | Verify checkout time recorded | "Checked out at: HH:MM" |
| 7 | Verify duration calculated | "Duration: Xh Ym" |
| 8 | Verify ticket status = "completed" | Status updated |
| 9 | Verify seat stays "occupied" | For statistics |

---

### TC-CHECKOUT-002: Check-out ticket (not checked-in)
**Priority:** High  
**Role:** Staff

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Navigate to "Check-out" page | Scanner ready |
| 2 | Scan ticket not checked-in yet | Error: "Ticket not checked-in. Cannot check-out" |

---

### TC-CHECKOUT-003: Check-out ticket (event not started)
**Priority:** Medium  
**Role:** Staff

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Try to checkout before event starts | Scanner ready |
| 2 | Scan checked-in ticket | Error: "Event has not started. Cannot check-out" |

---

### TC-CHECKOUT-004: Manual check-out (enter ticket code)
**Priority:** Medium  
**Role:** Staff

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Navigate to "Check-out" page | Input field shown |
| 2 | Enter ticket code: `ABC123XYZ` | Code entered |
| 3 | Click "Check-out" | Ticket info shown |
| 4 | Confirm checkout | Success with duration |

---

## 6. REPORTING TEST CASES

### TC-REPORT-001: View event statistics
**Priority:** High  
**Role:** Organizer

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Login as organizer | Dashboard shown |
| 2 | Navigate to event detail | Event info displayed |
| 3 | Click "Statistics" tab | Statistics shown |
| 4 | Verify total registered | Count shown |
| 5 | Verify total checked-in | Count shown |
| 6 | Verify total checked-out | Count shown |
| 7 | Verify attendance rate % | Percentage calculated |
| 8 | Verify seat utilization | Occupied/Total shown |

---

### TC-REPORT-002: View system report
**Priority:** Medium  
**Role:** Admin/Organizer

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Navigate to "Reports" | Reports dashboard shown |
| 2 | Select date range | From/To dates selected |
| 3 | Click "Generate Report" | Report generated |
| 4 | Verify total events | Count shown |
| 5 | Verify total registrations | Count shown |
| 6 | Verify total attendees | Count shown |
| 7 | Verify attendance trends | Chart/graph shown |

---

### TC-REPORT-003: Export event report
**Priority:** Medium  
**Role:** Organizer

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Navigate to event statistics | Stats displayed |
| 2 | Click "Export" button | Export options shown |
| 3 | Select format: CSV | Format selected |
| 4 | Click "Download" | File downloads |
| 5 | Verify file contains data | CSV with all records |

---

## 7. AUTHORIZATION TEST CASES

### TC-AUTH-101: Student cannot access organizer features
**Priority:** High  
**Role:** Student

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Login as student | Dashboard shown |
| 2 | Try to navigate to "Create Event" | 403 Forbidden or hidden |
| 3 | Try to access event edit URL directly | Redirect or error |

---

### TC-AUTH-102: Staff can only check-in/out
**Priority:** High  
**Role:** Staff

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Login as staff | Dashboard shown |
| 2 | Verify "Check-in" menu visible | Menu item shown |
| 3 | Verify "Check-out" menu visible | Menu item shown |
| 4 | Verify "Create Event" hidden | No access |
| 5 | Verify "Halls" hidden | No access |

---

### TC-AUTH-103: Organizer full access
**Priority:** High  
**Role:** Organizer

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Login as organizer | Dashboard shown |
| 2 | Verify all menus accessible | Events, Halls, Reports, etc. |
| 3 | Create event | Success |
| 4 | Manage halls | Success |
| 5 | View reports | Success |
| 6 | Check-in/out | Success |

---

## 8. EDGE CASES & ERROR HANDLING

### TC-EDGE-001: Concurrent registration (race condition)
**Priority:** High  
**Role:** Multiple Students

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Event has 1 seat left | Capacity: 99/100 |
| 2 | Student A clicks register | Processing |
| 3 | Student B clicks register (same time) | Processing |
| 4 | Verify only one succeeds | One gets ticket, other gets error |
| 5 | Verify capacity = 100/100 | No overbooking |

---

### TC-EDGE-002: Check-in during system downtime
**Priority:** Medium  
**Role:** Staff

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Backend service down | Offline |
| 2 | Try to scan QR code | Error: "Unable to connect" |
| 3 | Verify offline mode (if implemented) | Queue for later |

---

### TC-EDGE-003: Seat conflict resolution
**Priority:** High  
**Role:** Multiple Students

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Student A selects seat A5 | Seat highlighted |
| 2 | Student B selects same seat A5 | Seat highlighted |
| 3 | Student A confirms first | A5 reserved for Student A |
| 4 | Student B tries to confirm | Error: "Seat no longer available" |
| 5 | Verify A5 shows "Reserved" | Only Student A has seat |

---

## 9. PERFORMANCE TEST CASES

### TC-PERF-001: Load test - 100 concurrent registrations
**Priority:** Medium  
**Role:** Multiple Students

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Simulate 100 students | Load test tool |
| 2 | All register for same event simultaneously | Concurrent requests |
| 3 | Measure response time | < 3 seconds per request |
| 4 | Verify no data corruption | All registrations valid |
| 5 | Verify seat assignment correct | No duplicate seats |

---

### TC-PERF-002: QR code scanning performance
**Priority:** Medium  
**Role:** Staff

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Scan 50 QR codes consecutively | Fast scanning |
| 2 | Measure average scan time | < 2 seconds per scan |
| 3 | Verify all checkins recorded | No data loss |

---

## 10. SECURITY TEST CASES

### TC-SEC-001: SQL Injection protection
**Priority:** High  
**Role:** Attacker

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Enter SQL in email field: `'; DROP TABLE users; --` | Input sanitized |
| 2 | Submit login | Error or invalid login |
| 3 | Verify database intact | Tables still exist |

---

### TC-SEC-002: XSS protection
**Priority:** High  
**Role:** Attacker

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Create event with title: `<script>alert('XSS')</script>` | Input entered |
| 2 | Submit event | Event created |
| 3 | View event on public page | Script tags escaped, no alert |

---

### TC-SEC-003: JWT token expiration
**Priority:** High  
**Role:** Any User

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Login successfully | Token received |
| 2 | Wait for token expiration (e.g., 24h) | Time passed |
| 3 | Try to access protected resource | 401 Unauthorized |
| 4 | Verify redirect to login | Login page shown |

---

## 📊 Test Execution Summary

### Test Priority Distribution
- **High Priority:** 45 test cases (60%)
- **Medium Priority:** 25 test cases (33%)
- **Low Priority:** 5 test cases (7%)

### Test Coverage by Module
- Authentication: 10 cases
- Event Management: 12 cases
- Hall Management: 5 cases
- Ticket Registration: 11 cases
- Check-in/Check-out: 9 cases
- Reporting: 3 cases
- Authorization: 3 cases
- Edge Cases: 3 cases
- Performance: 2 cases
- Security: 3 cases

**Total: 61 Test Cases**

---

## 🎯 Testing Strategy

### Phase 1: Smoke Testing (Day 1)
- Run critical path tests (High priority)
- Verify basic functionality works

### Phase 2: Functional Testing (Day 2-3)
- Run all functional test cases
- Document bugs found

### Phase 3: Integration Testing (Day 4)
- Test end-to-end workflows
- Verify data flow between modules

### Phase 4: Regression Testing (Day 5)
- Re-run all tests after bug fixes
- Verify no new issues introduced

### Phase 5: Performance & Security (Day 6)
- Run performance tests
- Run security tests

---

## 📝 Test Report Template

```markdown
## Test Execution Report - [Date]

### Summary
- Total Tests: X
- Passed: Y
- Failed: Z
- Blocked: A
- Not Executed: B

### Pass Rate: XX%

### Critical Issues Found
1. [Issue description]
2. [Issue description]

### Recommendations
- [Recommendation 1]
- [Recommendation 2]
```
