# Clinic Appointment Management System

Bộ khung Harness Engineering cho dự án quản lý và đặt lịch khám phòng khám.

## Công nghệ mục tiêu
- Frontend: React.js, Redux Toolkit, React Router, Tailwind CSS.
- Backend: C#, ASP.NET Core Web API.
- ORM: Entity Framework Core.
- Database: PostgreSQL, quản lý bằng TablePlus.
- API: Swagger/OpenAPI, Postman.
- IDE: Antigravity IDE; môi trường Windows 10 / PowerShell.

> Đây là bộ tài liệu và Harness; chưa có source code nghiệp vụ Frontend/Backend, chưa tạo PostgreSQL Database và chưa tạo EF Core Migration. `frontend/` và `backend/` là placeholder.

## Bắt đầu
1. Giải nén vào ví dụ `D:\Clinic-Appointment-Management`.
2. Mở thư mục gốc bằng Antigravity IDE (Open Folder), nơi có `AGENTS.md`.
3. Kiểm tra `.agents/skills/` và `.agents/rules/`.
4. Mở Agent Panel → `...` → Customizations → Skills/Rules.
5. Đọc `docs/START_HERE.md`, `docs/SPEC.md` và `docs/DATABASE_IMPLEMENTATION_READINESS.md`.

## Cấu trúc
```text
AGENTS.md
README.md
docs/                 # SRS, architecture, DB, ERD, data dictionary, API, security, tests, ADR
planning/             # feature_list, progress, backlog, implementation log
diagrams/             # PlantUML và Mermaid source
.agents/rules/        # Antigravity workspace rules
.agents/skills/       # Antigravity workspace skills
backend/              # tạo ASP.NET Core sau khi duyệt thiết kế
frontend/             # tạo React sau khi duyệt thiết kế
database/             # tài liệu và SQL constraint reference
```

## Kiểm tra nhanh (PowerShell tại thư mục gốc)
```powershell
Get-Content .\planning\feature_list.json -Raw | ConvertFrom-Json | Out-Null
Write-Host 'feature_list.json: valid JSON'
Get-ChildItem .\.agents\skills -Directory
Get-ChildItem .\.agents\rules -File
Test-Path .\docs\SPEC.md
Test-Path .\docs\ARCHITECTURE.md
Test-Path .\docs\DATABASE_DESIGN.md
Test-Path .\docs\DATA_DICTIONARY.md
Test-Path .\docs\ERD.md
```

## Database design assets
- `docs/DATABASE_DESIGN.md` v1.2: mô hình dữ liệu logic PostgreSQL.
- `docs/DATABASE_DESIGN_CHANGELOG.md`: thay đổi thiết kế DB v1.2.
- `docs/DATA_DICTIONARY.md` v1.2: từ điển bảng/cột cho 30 bảng.
- `docs/ERD.md`: sơ đồ quan hệ Mermaid theo domain; 4 PNG preview đã render.
- `docs/DATABASE_REVIEW_CHECKLIST.md`: kiểm tra trước khi phê duyệt.
- `docs/DATABASE_IMPLEMENTATION_READINESS.md`: cổng kiểm soát trước EF Core migrations.
- `diagrams/mermaid/`: ERD tách theo domain.
- `diagrams/plantuml/erd-overview.puml`: ERD overview PlantUML.
- `diagrams/ERD_0*.png`: 4 sơ đồ ERD đã render theo domain.
- `database/reference/constraints-reference.sql`: ví dụ constraint, không phải script tạo DB hoàn chỉnh.

## Nguyên tắc
- Không code trước khi Feature được chọn và dependencies sẵn sàng.
- Không đánh dấu completed nếu thiếu test evidence.
- TBD không phải quyết định đã chốt.
- Mọi thay đổi nghiệp vụ cập nhật SPEC, BUSINESS_RULES, tài liệu phụ thuộc và feature tracking.
- Không chạy SQL tham khảo hoặc tạo migration trước khi Database Design được duyệt.
