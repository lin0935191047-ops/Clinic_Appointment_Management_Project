import '@testing-library/jest-dom';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { MemoryRouter } from 'react-router-dom';
import { BookingPage } from '../pages/BookingPage';
import { catalogApi, appointmentApi, mockSpecialties, mockDoctors, mockServices } from '../api/client';

vi.mock('../api/client', async () => {
  const actual = await vi.importActual<any>('../api/client');
  return {
    ...actual,
    catalogApi: {
      getSpecialties: vi.fn(),
      getDoctors: vi.fn(),
      getServices: vi.fn(),
      getAvailableSlots: vi.fn(),
    },
    appointmentApi: {
      bookGuest: vi.fn(),
    },
  };
});

describe('Frontend Component & Journey: Guest Booking (FEAT-052)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(catalogApi.getSpecialties).mockResolvedValue(mockSpecialties);
    vi.mocked(catalogApi.getDoctors).mockResolvedValue(mockDoctors);
    vi.mocked(catalogApi.getServices).mockResolvedValue(mockServices);
    vi.mocked(catalogApi.getAvailableSlots).mockResolvedValue([
      { startTime: '09:00', endTime: '09:30', isAvailable: true },
      { startTime: '09:30', endTime: '10:00', isAvailable: false },
      { startTime: '10:00', endTime: '10:30', isAvailable: true },
    ]);
  });

  it('renders booking step 1 and validates service selection', async () => {
    render(
      <MemoryRouter>
        <BookingPage />
      </MemoryRouter>
    );

    // Kiểm tra tiêu đề trang
    expect(screen.getByText(/Đặt Lịch Khám Trực Tuyến/i)).toBeInTheDocument();
    expect(screen.getByText(/Guest Booking/i)).toBeInTheDocument();

    // Đợi catalog tải xong
    await waitFor(() => {
      expect(catalogApi.getSpecialties).toHaveBeenCalled();
    });

    // Chọn Chuyên khoa trước
    const specialtyCard = await screen.findByText('Nội Tổng Quát');
    fireEvent.click(specialtyCard);

    // Chọn Dịch vụ
    const serviceCard = await screen.findByText('Khám Nội Tổng Quát');
    fireEvent.click(serviceCard);

    // Chuyển sang Bước 2
    const nextBtn = screen.getByRole('button', { name: /Tiếp tục chọn Bác sĩ & Giờ khám/i });
    fireEvent.click(nextBtn);

    // Phải sang được Bước 2 (Bác sĩ & Khung Giờ)
    await waitFor(() => {
      expect(screen.getByText(/Chọn Bác Sĩ Chuyên Khoa/i)).toBeInTheDocument();
    });
  });

  it('validates required fields on step 3 (Patient Info) according to SRS rules', async () => {
    render(
      <MemoryRouter>
        <BookingPage />
      </MemoryRouter>
    );

    // Chuyển qua bước 1
    await waitFor(() => expect(catalogApi.getSpecialties).toHaveBeenCalled());
    const specialtyCard = await screen.findByText('Nội Tổng Quát');
    fireEvent.click(specialtyCard);
    const serviceCard = await screen.findByText('Khám Nội Tổng Quát');
    fireEvent.click(serviceCard);
    fireEvent.click(screen.getByRole('button', { name: /Tiếp tục chọn Bác sĩ & Giờ khám/i }));

    // Tại bước 2, nhấn tiếp tục sang bước 3
    await waitFor(() => expect(screen.getByText(/Chọn Bác Sĩ Chuyên Khoa/i)).toBeInTheDocument());
    const toStep3Btn = screen.getByRole('button', { name: /Tiếp tục điền thông tin/i });
    fireEvent.click(toStep3Btn);

    // Tại bước 3: Form submit khi chưa điền Họ tên
    await waitFor(() => expect(screen.getByText(/Thông Tin Người Khám Bệnh/i)).toBeInTheDocument());
    const submitBtn = screen.getByRole('button', { name: /Xác Nhận Đặt Lịch Khám/i });
    const form = submitBtn.closest('form')!;
    fireEvent.submit(form);

    // Bắt buộc hiển thị lỗi Họ tên là bắt buộc
    expect(screen.getByText(/Họ và tên bệnh nhân là bắt buộc/i)).toBeInTheDocument();

    // Điền Họ tên nhưng chưa điền ngày sinh
    const nameInput = screen.getByPlaceholderText(/Ví dụ: NGUYỄN VĂN AN/i);
    fireEvent.change(nameInput, { target: { value: 'TRẦN VĂN KHÁCH' } });
    fireEvent.submit(form);

    // Bắt buộc hiển thị lỗi ngày tháng năm sinh
    expect(
      screen.getByText(/Vui lòng nhập Ngày tháng năm sinh đầy đủ, hoặc chuyển sang chế độ Chỉ biết năm sinh/i)
    ).toBeInTheDocument();

    // Điền ngày sinh nhưng số điện thoại không hợp lệ (regex)
    const dobInput = document.querySelector('input[type="date"]');
    if (dobInput) {
      fireEvent.change(dobInput, { target: { value: '1990-05-15' } });
    }
    const phoneInput = screen.getByPlaceholderText(/Ví dụ: 0912345678/i);
    fireEvent.change(phoneInput, { target: { value: '123' } }); // Sai regex sđt

    fireEvent.submit(form);
    expect(screen.getByText(/Số điện thoại liên hệ không hợp lệ/i)).toBeInTheDocument();
  });

  it('completes guest booking happy path and shows confirmation screen', async () => {
    vi.mocked(appointmentApi.bookGuest).mockResolvedValue({
      appointmentId: 'apt-1234-uuid',
      bookingCode: 'APT-TEST-2026',
      patientFullName: 'TRẦN VĂN KHÁCH',
      serviceName: 'Khám Nội Tổng Quát',
      doctorName: 'BS. CKII Nguyễn Văn An',
      specialtyName: 'Nội Tổng Quát',
      appointmentDate: '2026-11-15',
      startTime: '09:00',
      endTime: '09:30',
      fee: 250000,
      status: 'PendingConfirmation',
    });

    render(
      <MemoryRouter>
        <BookingPage />
      </MemoryRouter>
    );

    // Chuyển qua bước 1
    await waitFor(() => expect(catalogApi.getSpecialties).toHaveBeenCalled());
    const specialtyCard = await screen.findByText('Nội Tổng Quát');
    fireEvent.click(specialtyCard);
    const serviceCard = await screen.findByText('Khám Nội Tổng Quát');
    fireEvent.click(serviceCard);
    fireEvent.click(screen.getByRole('button', { name: /Tiếp tục chọn Bác sĩ & Giờ khám/i }));

    // Chuyển qua bước 2
    await waitFor(() => expect(screen.getByText(/Chọn Bác Sĩ Chuyên Khoa/i)).toBeInTheDocument());
    fireEvent.click(screen.getByRole('button', { name: /Tiếp tục điền thông tin/i }));

    // Bước 3: Điền đầy đủ thông tin bệnh nhân
    await waitFor(() => expect(screen.getByText(/Thông Tin Người Khám Bệnh/i)).toBeInTheDocument());
    fireEvent.change(screen.getByPlaceholderText(/Ví dụ: NGUYỄN VĂN AN/i), {
      target: { value: 'TRẦN VĂN KHÁCH' },
    });
    fireEvent.change(screen.getByPlaceholderText(/Ví dụ: 0912345678/i), {
      target: { value: '0912345678' },
    });
    
    // Chọn ngày sinh
    const dobInput = document.querySelector('input[type="date"]');
    if (dobInput) {
      fireEvent.change(dobInput, { target: { value: '1990-05-15' } });
    }

    // Submit form
    const submitBtn = screen.getByRole('button', { name: /Xác Nhận Đặt Lịch Khám/i });
    const form = submitBtn.closest('form')!;
    fireEvent.submit(form);

    // Assert: Gọi API đặt lịch và hiển thị màn hình thành công
    await waitFor(() => {
      expect(appointmentApi.bookGuest).toHaveBeenCalled();
      expect(screen.getByText(/Đặt Lịch Khám Thành Công!/i)).toBeInTheDocument();
      expect(screen.getByText(/APT-TEST-2026/i)).toBeInTheDocument();
    });
  });
});
