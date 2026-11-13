# Frontend Rules (React.js / TypeScript)

> Quy chuẩn lập trình Frontend áp dụng cho React 18+, TypeScript và Tailwind CSS.

---

## 1. Chuẩn mực TypeScript & React

- **Strict Type Checking:**
  - Bật `"strict": true` trong `tsconfig.json`.
  - Tuyệt đối hạn chế sử dụng kiểu `any`. Sử dụng `unknown`, `never`, hoặc định nghĩa explicit interfaces / types.
  - Sử dụng Type Inference khi kiểu dữ liệu đã hiển nhiên (ví dụ `const [count, setCount] = useState(0)` thay vì `useState<number>(0)`).
- **Function Components:**
  - 100% sử dụng Functional Components kết hợp React Hooks. Không sử dụng Class Components.
  - Định nghĩa component: `export const AppointmentCard = ({ appointment }: AppointmentCardProps) => { ... }`.
- **Naming Conventions:**
  - `PascalCase` cho tên File Component (`AppointmentCard.tsx`), Custom Type/Interface (`DoctorSchedule`).
  - `camelCase` cho Custom Hooks (`useDoctorSchedule.ts`), helper functions, variables.
  - `kebab-case` cho thư mục chức năng (`appointment-booking`).

---

## 2. Quản lý Trạng thái (State Management)

1. **Server State (Dữ liệu từ API):**
   - Sử dụng **TanStack Query (React Query)** cho toàn bộ tác vụ fetch, cache, revalidate dữ liệu từ backend.
   - Định nghĩa query key có phân cấp (Query Key Factory):
     ```typescript
     export const doctorKeys = {
       all: ['doctors'] as const,
       lists: () => [...doctorKeys.all, 'list'] as const,
       list: (filters: DoctorFilters) => [...doctorKeys.lists(), filters] as const,
       details: () => [...doctorKeys.all, 'detail'] as const,
       detail: (id: string) => [...doctorKeys.details(), id] as const,
       availableSlots: (id: string, date: string) => [...doctorKeys.detail(id), 'slots', date] as const,
     };
     ```
2. **Client State (Trạng thái giao diện cục bộ & toàn cục):**
   - Trạng thái UI trong phạm vi một màn hình: Sử dụng `useState` hoặc `useReducer`.
   - Trạng thái UI toàn cục (Thông tin User đăng nhập, Giỏ đặt lịch nhiều bước): Sử dụng `Zustand`.
3. **Form Handling:**
   - Sử dụng `React Hook Form` kết hợp với `Zod` để xây dựng schema validation ở phía client, đồng bộ thông báo lỗi trực tiếp trên giao diện.

---

## 3. Quy chuẩn Thiết kế Giao diện (UI & Styling)

- **Tailwind CSS:**
  - Sử dụng utility classes của Tailwind CSS.
  - Tránh viết CSS inline (`style={{ ... }}`) trừ các trường hợp tính toán vị trí động không thể tránh khỏi.
  - Đảm bảo thiết kế Responsive Mobile-First (`sm:`, `md:`, `lg:`, `xl:`).
- **Trải nghiệm Người dùng (UX & Feedback):**
  - Mọi thao tác tải dữ liệu phải có Skeleton Loading hoặc Spinner rõ ràng.
  - Mọi thao tác Mutation (Đặt lịch, Hủy lịch, Check-in) phải có phản hồi trạng thái: Disabled nút bấm khi đang gửi request, hiển thị Toast thông báo thành công hoặc lỗi chi tiết.
- **Khả năng tiếp cận (Accessibility - a11y):**
  - Cung cấp `aria-label` cho các nút bấm chỉ có icon.
  - Đảm bảo form inputs có nhãn `<label htmlFor="...">` tương ứng.

