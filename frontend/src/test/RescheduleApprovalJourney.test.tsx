import '@testing-library/jest-dom';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { MemoryRouter } from 'react-router-dom';
import { ReceptionistDashboardPage } from '../pages/ReceptionistDashboardPage';
import { authApi, receptionApi, RescheduleRequestItem, ReceptionAppointmentItem } from '../api/client';

vi.mock('../api/client', async () => {
  const actual = await vi.importActual<any>('../api/client');
  return {
    ...actual,
    authApi: {
      getStoredUser: vi.fn(),
      login: vi.fn(),
      logout: vi.fn(),
    },
    receptionApi: {
      getPendingReschedules: vi.fn(),
      getQueue: vi.fn(),
      approveReschedule: vi.fn(),
      rejectReschedule: vi.fn(),
      confirmAppointment: vi.fn(),
      checkInAppointment: vi.fn(),
    },
  };
});

describe('Frontend Component & Journey: Reschedule Approval by Receptionist (FEAT-052)', () => {
  const mockReceptionUser = {
    id: 'user-rec-01',
    email: 'reception@clinic.vn',
    roles: ['RECEPTIONIST'],
    fullName: 'Lễ Tân Thùy Linh',
  };

  const mockPendingReschedules: RescheduleRequestItem[] = [
    {
      requestId: 'req-001',
      appointmentId: 'apt-001',
      bookingCode: 'APT-RESCHED-01',
      patientFullName: 'Lê Văn Bận',
      patientPhoneNumber: '098***1234',
      serviceName: 'Khám Tim Mạch',
      currentDoctorFullName: 'BS. CKII Nguyễn Văn An',
      currentStartAtUtc: '2026-11-14T02:00:00Z',
      currentEndAtUtc: '2026-11-14T02:30:00Z',
      requestedDoctorFullName: 'BS. CKII Nguyễn Văn An',
      proposedStartAtUtc: '2026-11-16T03:00:00Z',
      proposedEndAtUtc: '2026-11-16T03:30:00Z',
      reason: 'Có cuộc họp đột xuất sáng thứ 7',
    },
  ];

  const mockAppointments: ReceptionAppointmentItem[] = [
    {
      id: 'apt-002',
      bookingCode: 'APT-NEW-02',
      queueNumber: 1,
      doctorId: 'doc-01',
      doctorName: 'BS. CKII Nguyễn Văn An',
      patientId: 'pat-01',
      patientName: 'Nguyễn Thị Hoa',
      patientCode: 'PAT-002',
      maskedPhoneNumber: '098***1234',
      serviceName: 'Khám Tim Mạch',
      servicePrice: 250000,
      startAt: '2026-11-14T02:00:00Z',
      endAt: '2026-11-14T02:30:00Z',
      status: 'PendingConfirmation',
      isGuest: true,
    },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(authApi.getStoredUser).mockReturnValue(mockReceptionUser);
    vi.mocked(receptionApi.getPendingReschedules).mockResolvedValue(mockPendingReschedules);
    vi.mocked(receptionApi.getQueue).mockResolvedValue(mockAppointments);
    vi.mocked(receptionApi.approveReschedule).mockResolvedValue({
      id: 'req-001',
      status: 'Approved',
      message: 'Đã phê duyệt đổi lịch thành công.',
    });
    vi.mocked(receptionApi.rejectReschedule).mockResolvedValue({
      id: 'req-001',
      status: 'Rejected',
      message: 'Đã từ chối yêu cầu đổi lịch.',
    });
    vi.mocked(receptionApi.confirmAppointment).mockResolvedValue({
      id: 'apt-002',
      status: 'Confirmed',
    });
  });

  it('renders receptionist dashboard and lists pending reschedule requests', async () => {
    render(
      <MemoryRouter>
        <ReceptionistDashboardPage />
      </MemoryRouter>
    );

    // Kiểm tra hiển thị tiêu đề dashboard
    expect(screen.getByText(/Bàn Tiếp Nhận Lễ Tân/i)).toBeInTheDocument();
    expect(screen.getByText(/Quản Lý Ca Khám & Đổi Lịch/i)).toBeInTheDocument();

    // Kiểm tra hiển thị yêu cầu đổi lịch đang chờ duyệt
    await waitFor(() => {
      expect(screen.getByText(/APT-RESCHED-01/i)).toBeInTheDocument();
      expect(screen.getByText(/Lê Văn Bận/i)).toBeInTheDocument();
      expect(screen.getByText(/Có cuộc họp đột xuất sáng thứ 7/i)).toBeInTheDocument();
    });
  });

  it('approves a reschedule request and triggers refresh', async () => {
    render(
      <MemoryRouter>
        <ReceptionistDashboardPage />
      </MemoryRouter>
    );

    await waitFor(() => expect(screen.getByText(/APT-RESCHED-01/i)).toBeInTheDocument());

    // Nhấn nút Duyệt Đổi Lịch
    const approveBtn = screen.getByRole('button', { name: /Duyệt Đổi Lịch/i });
    fireEvent.click(approveBtn);

    // Xác nhận gọi API approve với ghi chú
    await waitFor(() => {
      expect(receptionApi.approveReschedule).toHaveBeenCalledWith('req-001', expect.any(String));
      expect(screen.getByText(/Phê duyệt đổi lịch thành công/i)).toBeInTheDocument();
    });
  });

  it('opens reject modal and requires reject reason before submission', async () => {
    render(
      <MemoryRouter>
        <ReceptionistDashboardPage />
      </MemoryRouter>
    );

    await waitFor(() => expect(screen.getByText(/APT-RESCHED-01/i)).toBeInTheDocument());

    // Nhấn nút Từ chối đổi lịch ngoài danh sách
    const rejectBtn = screen.getByRole('button', { name: /Từ Chối/i });
    fireEvent.click(rejectBtn);

    // Modal từ chối mở ra
    expect(screen.getByText(/Từ Chối Yêu Cầu Đổi Lịch/i)).toBeInTheDocument();

    // Nhấn Xác nhận từ chối mà chưa nhập lý do -> Bị chặn
    const confirmRejectBtn = screen.getByRole('button', { name: /Xác Nhận Từ Chối/i });
    const rejectForm = confirmRejectBtn.closest('form')!;
    fireEvent.submit(rejectForm);

    expect(screen.getByText(/Vui lòng nhập lý do từ chối yêu cầu đổi lịch/i)).toBeInTheDocument();
    expect(receptionApi.rejectReschedule).not.toHaveBeenCalled();

    // Nhập lý do từ chối hợp lệ
    const reasonInput = screen.getByPlaceholderText(/Ví dụ: Bác sĩ bận/i);
    fireEvent.change(reasonInput, { target: { value: 'Bác sĩ không có ca trực vào giờ này.' } });
    fireEvent.submit(rejectForm);

    // Xác nhận gọi API reject với lý do
    await waitFor(() => {
      expect(receptionApi.rejectReschedule).toHaveBeenCalledWith(
        'req-001',
        'Bác sĩ không có ca trực vào giờ này.'
      );
    });
  });

  it('allows receptionist to confirm a pending appointment', async () => {
    render(
      <MemoryRouter>
        <ReceptionistDashboardPage />
      </MemoryRouter>
    );

    await waitFor(() => expect(screen.getByText(/APT-NEW-02/i)).toBeInTheDocument());

    // Nhấn Xác nhận ca khám
    const confirmBtn = screen.getByRole('button', { name: /Xác Nhận Lịch/i });
    fireEvent.click(confirmBtn);

    await waitFor(() => {
      expect(receptionApi.confirmAppointment).toHaveBeenCalledWith('apt-002', expect.any(String));
      expect(screen.getByText(/Đã xác nhận lịch hẹn thành công/i)).toBeInTheDocument();
    });
  });
});
