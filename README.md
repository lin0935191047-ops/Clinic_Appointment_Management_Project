# Clinic Appointment Management System

> **Hệ thống Quản lý Đặt lịch Khám bệnh** — Nền tảng đặt lịch và quản lý phòng khám hiện đại, an toàn và tối ưu hiệu suất, xây dựng trên nền tảng **ASP.NET Core Web API**, **React**, **Entity Framework Core** và **PostgreSQL**.

---

## 📌 Tổng quan dự án (Project Overview)

Dự án cung cấp giải pháp toàn diện cho việc đăng ký khám bệnh, quản lý lịch làm việc của bác sĩ, quản trị hồ sơ bệnh nhân và quy trình tiếp đón tại phòng khám:
- **Bệnh nhân & Khách vãng lai (Guest Booking):** Tìm kiếm bác sĩ, chuyên khoa, chọn khung giờ linh hoạt, đặt lịch khám nhanh chóng.
- **Bác sĩ (Doctor Portal):** Quản lý ca làm việc, xem danh sách bệnh nhân theo ngày, cập nhật ghi chú y tế.
- **Nhân viên tiếp đón (Receptionist/Staff):** Check-in bệnh nhân, tiếp nhận ca khám đột xuất, điều phối lịch hẹn và quản lý trạng thái phòng khám.
- **Quản trị viên (Administrator):** Quản lý chuyên khoa, tài khoản người dùng, báo cáo thống kê lượt khám và doanh thu.

---

## 🛠 Công nghệ sử dụng (Tech Stack)

| Tầng | Công nghệ | Chi tiết |
| :--- | :--- | :--- |
| **Backend** | ASP.NET Core 8.0+ Web API | C#, Clean Architecture, MediatR/CQRS, FluentValidation |
| **ORM & Database** | EF Core + PostgreSQL | Code-First Migrations, Npgsql, Connection Pooling |
| **Frontend** | React 18+ / TypeScript | Vite / Next.js, Tailwind CSS, TanStack Query, React Hook Form |
| **Authentication & Security** | JWT & Refresh Tokens | ASP.NET Core Identity / Custom RBAC, Rate Limiting, CORS |
| **Testing** | xUnit, Moq, Vitest / Playwright | Integration Testing với Testcontainers PostgreSQL |
| **DevOps & Tooling** | Docker, Docker Compose, GitHub Actions | Containerized dev/prod deployment |

---

## 📂 Cấu trúc thư mục (Repository Structure)

```text
Clinic-Appointment-Management/
│
├── AGENTS.md                          # Hướng dẫn và quy tắc làm việc cho AI Agents
├── README.md                          # Tài liệu tổng quan dự án
│
├── docs/                              # Tài liệu kỹ thuật chi tiết
│   ├── SPEC.md                        # Đặc tả yêu cầu phần mềm (SRS)
│   ├── ARCHITECTURE.md                # Kiến trúc hệ thống & thiết kế tầng
│   ├── DATABASE_DESIGN.md             # Thiết kế cơ sở dữ liệu PostgreSQL
│   ├── API_SPECIFICATION.md           # Đặc tả RESTful API
│   ├── BUSINESS_RULES.md              # Quy tắc nghiệp vụ đặt lịch & phòng khám
│   ├── SECURITY.md                    # Chính sách bảo mật & phân quyền
│   ├── TESTING_STRATEGY.md            # Chiến lược kiểm thử tự động
│   ├── DEVELOPMENT_PLAN.md            # Kế hoạch phát triển theo từng giai đoạn
│   └── adr/                           # Quyết định kiến trúc (Architecture Decision Records)
│       ├── 0001-frontend-backend-separation.md
│       ├── 0002-database-postgresql.md
│       └── 0003-guest-booking.md
│
├── planning/                          # Kế hoạch & theo dõi tiến độ
│   ├── feature_list.json              # Danh sách tính năng và phân loại ưu tiên
│   ├── progress.md                    # Trạng thái tiến độ thực tế
│   ├── backlog.md                     # Backlog công việc & user stories
│   └── implementation_log.md          # Nhật ký triển khai thay đổi kỹ thuật
│
├── .agents/                           # Quy chuẩn và kỹ năng cho AI Assistants
│   ├── rules/                         # Quy chuẩn lập trình (Coding Rules)
│   │   ├── project-rules.md
│   │   ├── backend-rules.md
│   │   ├── frontend-rules.md
│   │   └── database-rules.md
│   └── skills/                        # Hướng dẫn kỹ năng chuyên biệt (Agent Skills)
│       ├── project-planning/SKILL.md
│       ├── dotnet-backend/SKILL.md
│       ├── react-frontend/SKILL.md
│       ├── efcore-postgresql/SKILL.md
│       ├── clinic-business-logic/SKILL.md
│       ├── testing-qa/SKILL.md
│       └── documentation-sync/SKILL.md
│
├── backend/                           # ASP.NET Core Web API (Mã nguồn backend)
├── frontend/                          # React.js Application (Mã nguồn frontend)
└── .gitignore                         # Danh sách file loại trừ khỏi Git
```

---

## 🚀 Hướng dẫn bắt đầu (Getting Started)

### Điều kiện tiên quyết (Prerequisites)
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+ & npm / pnpm](https://nodejs.org/)
- [PostgreSQL 15+](https://www.postgresql.org/) hoặc Docker Desktop

### 1. Khởi chạy Database
```bash
# Sử dụng Docker Compose (sẽ cấu hình trong giai đoạn triển khai backend)
docker run --name clinic-postgres -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=ClinicAppointmentDb -p 5432:5432 -d postgres:16-alpine
```

### 2. Cài đặt và chạy Backend (Sắp ra mắt)
```bash
cd backend
dotnet restore
dotnet ef database update
dotnet run
```

### 3. Cài đặt và chạy Frontend (Sắp ra mắt)
```bash
cd frontend
npm install
npm run dev
```

---

## 📖 Hướng dẫn khai thác tài liệu

- Xem đặc tả yêu cầu chi tiết tại [docs/SPEC.md](docs/SPEC.md).
- Xem thiết kế CSDL và quan hệ thực thể tại [docs/DATABASE_DESIGN.md](docs/DATABASE_DESIGN.md).
- Xem quy tắc nghiệp vụ quan trọng (đặt lịch, chống trùng lịch) tại [docs/BUSINESS_RULES.md](docs/BUSINESS_RULES.md).
- Xem tiến độ triển khai tại [planning/progress.md](planning/progress.md).

