# START HERE — Antigravity Harness

## 1. Open workspace
Mở đúng thư mục gốc repository (nơi có `AGENTS.md`) trong Antigravity IDE. Không chỉ mở riêng frontend hoặc backend.

## 2. Discovery locations
- Workspace Skill: `.agents/skills/<skill-name>/SKILL.md`.
- Workspace Rules: `.agents/rules/*.md`.
- Root `AGENTS.md`: instructions luôn active, không cần frontmatter.
- Kiểm tra qua Agent Panel → `...` → Customizations → Skills/Rules.
- Nếu Skill vừa tạo chưa xuất hiện, refresh/reopen workspace hoặc mở conversation mới.

## 3. Slash commands
`/project-planning`, `/dotnet-backend`, `/react-frontend`, `/efcore-postgresql`, `/clinic-business-logic`, `/testing-qa`, `/documentation-sync`.

## 4. Prompt audit đầu tiên
```text
Đọc AGENTS.md, docs/START_HERE.md, docs/SPEC.md và planning/feature_list.json.
Chỉ audit Harness hiện có; không lập trình, không cài package, không tạo database.
Báo cáo thiếu sót và đề xuất sửa theo ưu tiên.
```

## 5. Document order
SPEC → Use Cases/Activity → Business Rules → Architecture → Database Design → API Specification → Testing Strategy → Feature tracking.

## 6. Feature lifecycle
`not_started` → `in_progress` → `needs_review` → `completed`; dùng `blocked` khi bị chặn bởi dependency/TBD. Không completed nếu thiếu test evidence.

## 7. Change control
Thay đổi nghiệp vụ phải cập nhật SPEC/BUSINESS_RULES, xác định tác động DB/API/Architecture/Tests, update Feature và tạo ADR nếu cần. Không code khi docs mâu thuẫn.

## Database design review (before EF Core migrations)

1. Read `DATABASE_DESIGN.md` v1.2.
2. Review `DATA_DICTIONARY.md`, `ERD.md` and `DATABASE_DESIGN_CHANGELOG.md`.
3. Open `DATABASE_REVIEW_CHECKLIST.md` and resolve required decisions.
4. Review `docs/adr/0006-postgresql-overlap-constraint.md`, `0008-patient-identity-storage.md`, and `0009-reschedule-slot-transaction.md`.
5. Do not create PostgreSQL schema or EF Core migrations until the owner approves the physical design.
