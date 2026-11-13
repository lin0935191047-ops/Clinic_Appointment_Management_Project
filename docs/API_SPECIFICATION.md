# API SPECIFICATION — REST API v1

Status: Proposed contract. Base path `/api/v1`; JSON over HTTPS in deployment.

## Conventions
- JSON camelCase; UUID IDs; UTC ISO-8601 timestamps.
- ProblemDetails-style errors; paginated list `{ items, page, pageSize, totalItems, totalPages }`.
- Backend owns status, timestamps, refund rates and money calculations.
- Client không được quyết định PatientId, UserId, Role, Status, refund rate/amount.
- Public Guest Booking không cần JWT.

## Public catalog
| Method | Path | Purpose | Auth |
|---|---|---|---|
| GET | `/public/specialties` | Danh sách chuyên khoa | Public |
| GET | `/public/doctors` | Tìm bác sĩ | Public |
| GET | `/public/doctors/{doctorId}` | Chi tiết bác sĩ public | Public |
| GET | `/public/services` | Danh sách dịch vụ | Public |
| GET | `/public/availability` | Khung giờ còn trống | Public + rate limit |

Availability query: `doctorId`, `serviceId`, `fromDate`, `toDate`. Không trả tên bệnh nhân/lịch riêng của người khác.

## Guest booking
### POST `/public/appointments`
No JWT required. Request example:
```json
{
  "patient": {
    "fullName": "Nguyen Van A",
    "dateOfBirth": "1998-04-12",
    "birthYear": null,
    "phoneNumber": "+84901234567",
    "email": null,
    "identifiers": []
  },
  "doctorId": "00000000-0000-0000-0000-000000000001",
  "serviceId": "00000000-0000-0000-0000-000000000002",
  "startAt": "2026-11-20T01:00:00Z",
  "reason": "General consultation",
  "idempotencyKey": "client-generated-random-key"
}
```
Validation: FullName; DOB hoặc BirthYear; phone; doctor/service active; valid schedule; future slot; no overlap. `201 Created` trả bookingCode, status, time, bác sĩ/dịch vụ public và next steps; không trả PatientId nội bộ.

## OTP flow
| Method | Path | Purpose |
|---|---|---|
| POST | `/public/appointment-access/otp/request` | Request OTP cho LOOKUP/RESCHEDULE/CANCEL |
| POST | `/public/appointment-access/otp/verify` | Verify OTP, cấp scoped grant |

Request gồm `bookingCode`, `phoneNumber`, `purpose`. Response không tiết lộ booking/phone có tồn tại; generic acknowledgement. Verify body: `challengeId`, `otp`. Grant ngắn hạn, gắn booking/purpose và không phải User JWT. SMS provider TBD.

## Lookup
`POST /public/appointments/lookup` — cần LOOKUP grant; trả appointment summary tối thiểu, không trả Consultation/medical notes/raw identifiers.

## Reschedule
| Method | Path | Purpose | Auth |
|---|---|---|---|
| POST | `/public/appointments/{bookingCode}/reschedule-requests` | Guest đề xuất slot mới | RESCHEDULE grant |
| GET | `/reception/reschedule-requests` | Queue duyệt | Receptionist |
| POST | `/reception/reschedule-requests/{requestId}/approve` | Duyệt, recheck slot | Receptionist |
| POST | `/reception/reschedule-requests/{requestId}/reject` | Từ chối + reason | Receptionist |

Guest gửi `proposedStartAt`, optional `patientNote`. Old appointment remains. Slot mới không được giữ trước approval. Khi duyệt, re-check và update trong transaction; nếu conflict, giữ lịch cũ và trả 409/đóng request theo policy.

## Cancellation
`POST /public/appointments/{bookingCode}/cancel` — cần CANCEL grant. Body `{ "reason": "..." }`. Không nhận cancellation timestamp/rate/amount từ Client. Server kiểm tra chưa đến giờ bắt đầu, lấy server time, tính `>=60 => 100%`, `<60 and >0 => 50%`, ghi cancellation/refund + chuyển Cancelled trong transaction. Response phải phân biệt expected refund và processing status; không nói tiền đã về khi Pending.

## Authenticated/staff endpoints
| Method | Path | Role |
|---|---|---|
| POST | `/auth/register` | Public Patient optional |
| POST | `/auth/login` | Public |
| POST | `/auth/refresh` | Refresh token |
| POST | `/auth/logout` | Authenticated |
| GET/PATCH | `/patients/me` | Patient |
| GET | `/doctor/appointments` | Doctor |
| POST | `/doctor/appointments/{id}/start` | Assigned Doctor |
| POST | `/doctor/appointments/{id}/complete` | Assigned Doctor |
| POST | `/doctor/consultations` | Assigned Doctor |
| GET | `/reception/appointments` | Receptionist |
| POST | `/reception/appointments/{id}/confirm` | Receptionist |
| POST | `/reception/appointments/{id}/check-in` | Receptionist |
| GET/POST/PUT | `/admin/doctors` | Admin |
| GET/POST/PUT | `/admin/specialties` | Admin |
| GET/POST/PUT | `/admin/services` | Admin |
| GET/POST/PUT | `/admin/schedules` | Admin |
| GET | `/admin/reports/appointments` | Admin |

## HTTP status guidance
200 success; 201 created; 400/422 validation; 401 unauthenticated; 403 forbidden; 404 not found/concealed; 409 conflict/stale slot; 429 rate limit; 500 generic unexpected error.

## Contract acceptance
Mỗi endpoint có DTO + validation; OpenAPI mô tả auth/role; public API không lộ dữ liệu nhạy cảm; booking/reschedule/cancel có transaction + idempotency/concurrency tests; OTP grant scoped/short-lived/one-time.
