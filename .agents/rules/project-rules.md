# Project Rules & General Standards

> Quy chuẩn làm việc chung áp dụng cho toàn bộ thành viên và AI Agents tham gia dự án **Clinic-Appointment-Management**.

---

## 1. Quy chuẩn Git & Commit (Conventional Commits)

Mọi commit bắt buộc phải tuân thủ chuẩn [Conventional Commits](https://www.conventionalcommits.org/):

```text
<type>(<scope>): <mô tả ngắn gọn bằng tiếng Anh hoặc tiếng Việt>

[Nội dung chi tiết nếu có]
```

### Các loại commit type:
- `feat`: Tính năng mới (ví dụ: `feat(booking): implement guest appointment creation API`)
- `fix`: Sửa lỗi (ví dụ: `fix(slots): resolve timezone offset issue in available slots calculation`)
- `docs`: Thêm hoặc chỉnh sửa tài liệu (ví dụ: `docs(api): update appointment cancellation endpoint docs`)
- `style`: Định dạng code, whitespace, formatting (không ảnh hưởng logic)
- `refactor`: Tái cấu trúc mã nguồn mà không sửa bug hay thêm tính năng
- `test`: Bổ sung hoặc sửa đổi Unit/Integration tests
- `chore`: Cập nhật cấu hình build, package dependencies, gitignore...

### Quy ước đặt tên nhánh (Branching Strategy):
- Nhánh chính: `main` (Production-ready), `develop` (Tích hợp tính năng).
- Nhánh tính năng: `feature/<tên-tính-năng>` (ví dụ: `feature/doctor-scheduling`).
- Nhánh sửa lỗi: `bugfix/<tên-lỗi>` hoặc `hotfix/<tên-lỗi>`.

---

## 2. Quy trình Kiểm tra Mã nguồn (Pull Request & Quality Checklist)

Trước khi coi một tác vụ là hoàn thành:
1. **Build & Compile:** Toàn bộ dự án phải compile thành công không có Warning nghiêm trọng và 0 Error.
2. **Automated Tests:** Toàn bộ test liên quan phải pass 100%.
3. **No Secrets in Code:** Tuyệt đối không hardcode mật khẩu, connection strings, JWT secret keys vào source code.
4. **Documentation Sync:** Cập nhật lại các file tài liệu tương ứng (`docs/`, `planning/implementation_log.md`, `planning/progress.md`) nếu có thay đổi về API, DB hoặc cấu trúc nghiệp vụ.

---

## 3. Quản lý Môi trường & Cấu hình (Environment Management)

- Mọi tệp chứa cấu hình thực tế của môi trường cục bộ (Local / Development) phải được khai báo trong `.gitignore`.
- Cung cấp các tệp mẫu:
  - Backend: `appsettings.Example.json`
  - Frontend: `.env.example`
- Không bao giờ chia sẻ dữ liệu thật của bệnh nhân trong môi trường phát triển (sử dụng dữ liệu giả lập / mock data).

