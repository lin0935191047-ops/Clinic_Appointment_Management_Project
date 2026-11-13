---
name: react-frontend
description: Hướng dẫn xây dựng giao diện người dùng React 18+ với TypeScript, Tailwind CSS, TanStack Query, React Hook Form và Zod.
---

# React Frontend Development Skill

Kỹ năng này hướng dẫn AI Agent cách xây dựng các màn hình, components và luồng tương tác người dùng phía client trong hệ thống phòng khám.

---

## 1. Tổ chức Thư mục theo Tính năng (Feature-Sliced Directory)

Mỗi tính năng phức tạp (ví dụ `booking`, `reception`, `appointments`) cần được cô lập trong `src/features/<feature-name>`:

```text
src/features/booking/
├── api/                  # TanStack Query hooks (useAvailableSlots, useBookAppointment)
├── components/           # Các component con của tính năng
│   ├── StepSpecialty.tsx
│   ├── StepDoctor.tsx
│   ├── StepDateTime.tsx
│   └── StepPatientInfo.tsx
├── types/                # TypeScript interfaces dành riêng cho tính năng
├── utils/                # Helper logic (format slot, calculate price...)
└── BookingWizard.tsx     # Container component chính
```

---

## 2. Tiêu chuẩn Gọi API với TanStack Query

Mọi tương tác API phải được đóng gói thành Custom Hook:

```typescript
// src/features/doctors/api/useAvailableSlots.ts
import { useQuery } from '@tanstack/react-query';
import { apiClient } from '@/services/apiClient';
import { ApiResponse, SlotDto } from '@/types';

export const useAvailableSlots = (doctorId: string, date: string) => {
  return useQuery({
    queryKey: ['doctors', doctorId, 'slots', date],
    queryFn: async () => {
      const response = await apiClient.get<ApiResponse<SlotDto[]>>(
        `/api/v1/doctors/${doctorId}/available-slots`,
        { params: { date } }
      );
      return response.data.data;
    },
    enabled: Boolean(doctorId && date),
    staleTime: 1000 * 60 * 2, // Cache 2 phút
  });
};
```

---

## 3. Quản lý Biểu mẫu với React Hook Form + Zod

Mọi form nhập liệu (đặc biệt là form đặt lịch khám) bắt buộc phải có schema validation rõ ràng:

```typescript
import { z } from 'zod';

export const guestBookingSchema = z.object({
  fullName: z.string().min(2, 'Họ tên phải có ít nhất 2 ký tự'),
  phoneNumber: z.string().regex(/^(0[3|5|7|8|9])[0-9]{8}$/, 'Số điện thoại không hợp lệ'),
  email: z.string().email('Email không hợp lệ').optional().or(z.literal('')),
  dateOfBirth: z.string().min(1, 'Vui lòng chọn ngày sinh'),
  reasonForVisit: z.string().min(5, 'Vui lòng mô tả ngắn gọn triệu chứng (tối thiểu 5 ký tự)'),
});

export type GuestBookingFormValues = z.infer<typeof guestBookingSchema>;
```

---

## 4. Checklist Kiểm tra Code Frontend

- [ ] Component có hỗ trợ trạng thái `isLoading`, `isError`, và empty data không?
- [ ] Giao diện có hiển thị tốt trên màn hình Mobile (chiều rộng 375px) không?
- [ ] Các thông báo lỗi từ API có được hiển thị trực tiếp và thân thiện với người bệnh không?
- [ ] Không có type `any` tùy tiện trong mã nguồn.

