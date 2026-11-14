import '@testing-library/jest-dom';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { MemoryRouter } from 'react-router-dom';
import { LookupPage } from '../pages/LookupPage';
import { appointmentApi } from '../api/client';
import { AppointmentDetail, RefundPreview } from '../types';

vi.mock('../api/client', async () => {
  const actual = await vi.importActual<any>('../api/client');
  return {
    ...actual,
    appointmentApi: {
      requestOtp: vi.fn(),
      verifyOtpAndGetDetails: vi.fn(),
      calculateRefundPreview: vi.fn(),
      cancelAppointment: vi.fn(),
    },
  };
});

describe('Frontend Component & Journey: Refund Boundary Display (FEAT-052)', () => {
  const mockAppointment: AppointmentDetail = {
    id: 'apt-001-uuid',
    bookingCode: 'APT-REFUND-01',
    patientFullName: 'Phạm Thị Lan',
    contactPhoneMasked: '091***456',
    doctorName: 'BS. CKII Nguyễn Văn An',
    specialtyName: 'Tim Mạch',
    serviceName: 'Khám Tim Mạch Chuyên Sâu',
    appointmentDate: '2026-11-15',
    startTime: '10:00',
    endTime: '10:30',
    status: 'Confirmed',
    paidAmount: 300000,
    createdAt: '2026-11-14T00:00:00Z',
  };

  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(appointmentApi.requestOtp).mockResolvedValue({
      challengeId: 'chl-001',
      message: 'Mã OTP đã được gửi.',
    });
    vi.mocked(appointmentApi.verifyOtpAndGetDetails).mockResolvedValue(mockAppointment);
  });

  it('displays 100% refund rate when cancelled >= 60 minutes before appointment (BR-010)', async () => {
    // 100% refund preview mock (còn 120 phút trước giờ hẹn)
    const mockPreview100: RefundPreview = {
      paidAmount: 300000,
      minutesUntilAppointment: 120,
      refundPercentage: 100,
      refundAmount: 300000,
      cancellationFee: 0,
      policyNote: 'Hủy trước 60 phút: Hoàn 100% phí dịch vụ đã thanh toán (phí hủy 0%).',
    };
    vi.mocked(appointmentApi.calculateRefundPreview).mockReturnValue(mockPreview100);

    render(
      <MemoryRouter>
        <LookupPage />
      </MemoryRouter>
    );

    // Bước 1: Yêu cầu OTP
    fireEvent.change(screen.getByPlaceholderText(/Ví dụ: BK123456/i), {
      target: { value: 'APT-REFUND-01' },
    });
    fireEvent.change(screen.getByPlaceholderText(/Ví dụ: 0912345678/i), {
      target: { value: '0912345678' },
    });
    fireEvent.click(screen.getByRole('button', { name: /Gửi Mã Xác Thực OTP/i }));

    // Bước 2: Nhập OTP
    await waitFor(() => expect(screen.getByPlaceholderText(/Nhập 6 số OTP/i)).toBeInTheDocument());
    fireEvent.change(screen.getByPlaceholderText(/Nhập 6 số OTP/i), {
      target: { value: '123456' },
    });
    fireEvent.click(screen.getByRole('button', { name: /Xác Nhận & Xem Lịch Hẹn/i }));

    // Bước 3: Đã tải thông tin cuộc hẹn
    await waitFor(() => {
      expect(screen.getByText(/APT-REFUND-01/i)).toBeInTheDocument();
      expect(screen.getByText(/Phạm Thị Lan/i)).toBeInTheDocument();
    });

    // Nhấn nút Hủy lịch khám & Hoàn phí
    const cancelBtn = screen.getByRole('button', { name: /Hủy Lịch Khám & Hoàn Phí/i });
    fireEvent.click(cancelBtn);

    // Assert: Modal hiển thị tính toán ranh giới 100%
    await waitFor(() => {
      expect(appointmentApi.calculateRefundPreview).toHaveBeenCalled();
      expect(screen.getByText(/Chi Tiết Tính Hoàn Phí Theo Quy Định/i)).toBeInTheDocument();
      expect(screen.getByText(/120 phút/i)).toBeInTheDocument();
      // Kiểm tra có hiển thị tỷ lệ 100% và ghi chú chính sách
      expect(screen.getAllByText(/100%/i).length).toBeGreaterThanOrEqual(1);
      expect(screen.getByText(/Hủy trước 60 phút: Hoàn 100%/i)).toBeInTheDocument();
    });
  });

  it('displays 50% refund rate when cancelled < 60 minutes before appointment (BR-010)', async () => {
    // 50% refund preview mock (còn 45 phút trước giờ hẹn)
    const mockPreview50: RefundPreview = {
      paidAmount: 300000,
      minutesUntilAppointment: 45,
      refundPercentage: 50,
      refundAmount: 150000,
      cancellationFee: 150000,
      policyNote: 'Hủy dưới 60 phút trước giờ hẹn: Hoàn 50% phí dịch vụ (phí hủy 50%).',
    };
    vi.mocked(appointmentApi.calculateRefundPreview).mockReturnValue(mockPreview50);

    render(
      <MemoryRouter>
        <LookupPage />
      </MemoryRouter>
    );

    // Tra cứu
    fireEvent.change(screen.getByPlaceholderText(/Ví dụ: BK123456/i), {
      target: { value: 'APT-REFUND-01' },
    });
    fireEvent.change(screen.getByPlaceholderText(/Ví dụ: 0912345678/i), {
      target: { value: '0912345678' },
    });
    fireEvent.click(screen.getByRole('button', { name: /Gửi Mã Xác Thực OTP/i }));

    await waitFor(() => expect(screen.getByPlaceholderText(/Nhập 6 số OTP/i)).toBeInTheDocument());
    fireEvent.change(screen.getByPlaceholderText(/Nhập 6 số OTP/i), {
      target: { value: '123456' },
    });
    fireEvent.click(screen.getByRole('button', { name: /Xác Nhận & Xem Lịch Hẹn/i }));

    await waitFor(() => expect(screen.getByText(/APT-REFUND-01/i)).toBeInTheDocument());

    // Mở modal hủy
    fireEvent.click(screen.getByRole('button', { name: /Hủy Lịch Khám & Hoàn Phí/i }));

    // Assert: Modal hiển thị tính toán ranh giới 50%
    await waitFor(() => {
      expect(screen.getByText(/45 phút/i)).toBeInTheDocument();
      expect(screen.getAllByText(/50%/i).length).toBeGreaterThanOrEqual(1);
      expect(screen.getByText(/Hủy dưới 60 phút trước giờ hẹn: Hoàn 50%/i)).toBeInTheDocument();
    });
  });
});
