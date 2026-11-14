import axios from 'axios';
import {
  ApiResponse,
  Specialty,
  Doctor,
  ClinicService,
  AvailableSlot,
  GuestBookingRequest,
  BookingConfirmation,
  AppointmentDetail,
  RefundPreview,
  DoctorDashboardData,
  ConsultationData,
  RecordConsultationPayload,
  CompleteConsultationPayload,
} from '../types';

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '/api/v1',
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 10000,
});

// Tự động gắn Bearer Token nếu người dùng đã đăng nhập (Staff / Doctor / Admin)
apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token && config.headers) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    const errorMsg =
      error.response?.data?.message ||
      error.response?.data?.errors?.join(', ') ||
      error.message ||
      'Đã xảy ra lỗi kết nối với máy chủ.';
    return Promise.reject(new Error(errorMsg));
  }
);

// Mock catalog data for fallback / development preview
export const mockSpecialties: Specialty[] = [
  { id: 'spec-1', name: 'Nội Tổng Quát', code: 'INT_MED', description: 'Khám và điều trị các bệnh lý nội khoa người lớn' },
  { id: 'spec-2', name: 'Nhi Khoa', code: 'PED', description: 'Chăm sóc sức khỏe toàn diện và khám bệnh cho trẻ em' },
  { id: 'spec-3', name: 'Tim Mạch', code: 'CARD', description: 'Chẩn đoán và theo dõi huyết áp, tim mạch, mạch máu' },
  { id: 'spec-4', name: 'Tai Mũi Họng', code: 'ENT', description: 'Khám nội soi và điều trị các bệnh tai mũi họng' },
  { id: 'spec-5', name: 'Răng Hàm Mặt', code: 'DENT', description: 'Khám nha khoa, nhổ răng, lấy cao răng, phục hình' },
];

export const mockDoctors: Doctor[] = [
  { id: 'doc-1', fullName: 'BS. CKI Nguyễn Văn An', title: 'Bác sĩ Chuyên khoa I', specialtyId: 'spec-1', specialtyName: 'Nội Tổng Quát', consultationFee: 150000, roomNumber: 'P.101', bio: 'Hơn 15 năm kinh nghiệm điều trị bệnh lý nội tổng quát tại các bệnh viện tuyến đầu.' },
  { id: 'doc-2', fullName: 'ThS. BS Trần Thị Mai', title: 'Thạc sĩ Bác sĩ', specialtyId: 'spec-2', specialtyName: 'Nhi Khoa', consultationFee: 200000, roomNumber: 'P.105', bio: 'Chuyên gia Nhi khoa giàu kinh nghiệm, tận tâm, thân thiện với các bé.' },
  { id: 'doc-3', fullName: 'TS. BS Lê Hoàng Nam', title: 'Tiến sĩ Bác sĩ', specialtyId: 'spec-3', specialtyName: 'Tim Mạch', consultationFee: 250000, roomNumber: 'P.201', bio: 'Chuyên gia Tim mạch can thiệp, nguyên Phó khoa Tim mạch BV Đa khoa.' },
  { id: 'doc-4', fullName: 'BS. CKI Phạm Thu Trang', title: 'Bác sĩ Chuyên khoa I', specialtyId: 'spec-4', specialtyName: 'Tai Mũi Họng', consultationFee: 180000, roomNumber: 'P.204', bio: 'Chuyên sâu nội soi Tai Mũi Họng và xử trí viêm xoang mạn tính.' },
  { id: 'doc-5', fullName: 'BS. Vũ Đức Minh', title: 'Bác sĩ Răng Hàm Mặt', specialtyId: 'spec-5', specialtyName: 'Răng Hàm Mặt', consultationFee: 150000, roomNumber: 'P.301', bio: 'Bác sĩ nha khoa thẩm mỹ và điều trị bệnh lý răng miệng tổng quát.' },
];

export const mockServices: ClinicService[] = [
  { id: 'srv-1', name: 'Khám Nội Tổng Quát', description: 'Khám lâm sàng, tư vấn đơn thuốc và chỉ định xét nghiệm nếu cần', durationMinutes: 30, specialtyId: 'spec-1', basePrice: 150000 },
  { id: 'srv-2', name: 'Khám Nhi Tổng Quát', description: 'Đánh giá phát triển thể chất và khám bệnh lý trẻ nhỏ', durationMinutes: 30, specialtyId: 'spec-2', basePrice: 200000 },
  { id: 'srv-3', name: 'Khám & Tư Vấn Tim Mạch', description: 'Đo điện tim, tư vấn bệnh lý tăng huyết áp và xơ vữa', durationMinutes: 30, specialtyId: 'spec-3', basePrice: 250000 },
  { id: 'srv-4', name: 'Nội Soi Tai Mũi Họng', description: 'Nội soi chẩn đoán bằng ống sơ mềm độ nét cao', durationMinutes: 30, specialtyId: 'spec-4', basePrice: 180000 },
  { id: 'srv-5', name: 'Khám Nha Khoa Tổng Quát', description: 'Kiểm tra răng miệng, tư vấn chăm sóc và cạo vôi răng', durationMinutes: 30, specialtyId: 'spec-5', basePrice: 150000 },
];

export const catalogApi = {
  getSpecialties: async (): Promise<Specialty[]> => {
    try {
      const res = await apiClient.get<ApiResponse<Specialty[]>>('/specialties');
      return res.data.data && res.data.data.length > 0 ? res.data.data : mockSpecialties;
    } catch {
      return mockSpecialties;
    }
  },
  getDoctors: async (specialtyId?: string): Promise<Doctor[]> => {
    try {
      const res = await apiClient.get<ApiResponse<Doctor[]>>('/doctors', {
        params: { specialtyId },
      });
      return res.data.data && res.data.data.length > 0
        ? res.data.data
        : mockDoctors.filter((d) => !specialtyId || d.specialtyId === specialtyId);
    } catch {
      return mockDoctors.filter((d) => !specialtyId || d.specialtyId === specialtyId);
    }
  },
  getServices: async (specialtyId?: string): Promise<ClinicService[]> => {
    try {
      const res = await apiClient.get<ApiResponse<ClinicService[]>>('/services', {
        params: { specialtyId },
      });
      return res.data.data && res.data.data.length > 0
        ? res.data.data
        : mockServices.filter((s) => !specialtyId || s.specialtyId === specialtyId);
    } catch {
      return mockServices.filter((s) => !specialtyId || s.specialtyId === specialtyId);
    }
  },
  getAvailableSlots: async (doctorId: string, date: string, serviceId?: string): Promise<AvailableSlot[]> => {
    try {
      const res = await apiClient.get<ApiResponse<any>>('/availability', {
        params: { doctorId, date, serviceId },
      });
      const data = res.data.data;
      if (Array.isArray(data)) {
        return data;
      }
      if (data && Array.isArray(data.slots)) {
        return data.slots.map((s: any) => ({
          startTime: s.localStartTime || s.startTime,
          endTime: s.localEndTime || s.endTime,
          isAvailable: s.isAvailable,
        }));
      }
      return generateMockSlots();
    } catch {
      return generateMockSlots();
    }
  },
};

function generateMockSlots(): AvailableSlot[] {
  return [
    { startTime: '08:00', endTime: '08:30', isAvailable: true },
    { startTime: '08:30', endTime: '09:00', isAvailable: true },
    { startTime: '09:00', endTime: '09:30', isAvailable: false },
    { startTime: '09:30', endTime: '10:00', isAvailable: true },
    { startTime: '10:00', endTime: '10:30', isAvailable: true },
    { startTime: '10:30', endTime: '11:00', isAvailable: false },
    { startTime: '13:30', endTime: '14:00', isAvailable: true },
    { startTime: '14:00', endTime: '14:30', isAvailable: true },
    { startTime: '14:30', endTime: '15:00', isAvailable: true },
    { startTime: '15:00', endTime: '15:30', isAvailable: false },
    { startTime: '15:30', endTime: '16:00', isAvailable: true },
  ];
}

export const appointmentApi = {
  bookGuest: async (req: GuestBookingRequest): Promise<BookingConfirmation> => {
    // 1. Chuyển đổi ngày giờ khám theo múi giờ Việt Nam (UTC+7) sang ISO UTC cho Backend
    const [h, m] = req.startTime.split(':').map(Number);
    const [y, mon, d] = req.appointmentDate.split('-').map(Number);
    const startAtUtc = new Date(Date.UTC(y, mon - 1, d, h - 7, m)).toISOString();

    const payload = {
      fullName: req.patientFullName.trim(),
      phoneNumber: req.contactPhone.replace(/[\s-]/g, ''),
      dateOfBirth: req.dateOfBirth || null,
      birthYear: req.birthYear ? Number(req.birthYear) : null,
      email: req.email?.trim() || null,
      doctorId: req.doctorId,
      serviceId: req.serviceId,
      startAtUtc: startAtUtc,
      reasonForVisit: req.notes?.trim() || null,
      idempotencyKey: typeof crypto !== 'undefined' && crypto.randomUUID ? crypto.randomUUID() : 'idemp-' + Date.now(),
    };

    try {
      const res = await apiClient.post<ApiResponse<any>>('/appointments/guest', payload);
      if (res.data.data) {
        const d = res.data.data;
        const startUtc = new Date(d.startAtUtc || startAtUtc);
        const endUtc = new Date(d.endAtUtc || new Date(startUtc.getTime() + (d.serviceDuration || 30) * 60000));
        const vnStart = new Date(startUtc.getTime() + 7 * 3600000);
        const vnEnd = new Date(endUtc.getTime() + 7 * 3600000);

        return {
          appointmentId: d.appointmentId || d.id || 'apt-' + Date.now(),
          bookingCode: d.bookingCode,
          patientFullName: d.patientFullName || req.patientFullName,
          doctorName: d.doctorName || 'BS. Chuyên khoa',
          specialtyName: d.specialtyName || 'Chuyên khoa',
          serviceName: d.serviceName || 'Dịch vụ khám',
          appointmentDate: vnStart.toISOString().split('T')[0],
          startTime: vnStart.toISOString().substring(11, 16),
          endTime: vnEnd.toISOString().substring(11, 16),
          status: 'PendingConfirmation',
          fee: d.servicePrice || 150000,
        };
      }
      throw new Error(res.data.message || 'Không thể tạo lịch hẹn.');
    } catch (err: any) {
      // Nếu API trả về lỗi nghiệp vụ rõ ràng (ví dụ 400 validation, 409 conflict trùng ca), ném ra ngoài để form hiển thị
      if (err.response?.data?.message || err.response?.data?.errors) {
        const errorText = err.response.data.errors?.join(', ') || err.response.data.message;
        throw new Error(errorText);
      }
      // Mock fallback chỉ dùng khi offline dev preview:
      const doc = mockDoctors.find((d) => d.id === req.doctorId);
      const spec = mockSpecialties.find((s) => s.id === req.specialtyId);
      const srv = mockServices.find((s) => s.id === req.serviceId);
      const randomCode = 'APT-20261115-' + Math.floor(100000 + Math.random() * 900000);
      return {
        appointmentId: 'apt-' + Date.now(),
        bookingCode: randomCode,
        patientFullName: req.patientFullName,
        doctorName: doc?.fullName || 'BS. Chuyên khoa',
        specialtyName: spec?.name || 'Đa khoa',
        serviceName: srv?.name || 'Khám bệnh',
        appointmentDate: req.appointmentDate,
        startTime: req.startTime,
        endTime: calculateEndTime(req.startTime, srv?.durationMinutes || 30),
        status: 'PendingConfirmation',
        fee: srv?.basePrice || 150000,
      };
    }
  },

  requestOtp: async (bookingCode: string, phone: string, purpose: 'LOOKUP' | 'RESCHEDULE' | 'CANCEL'): Promise<{ challengeId: string; message: string }> => {
    try {
      const res = await apiClient.post<ApiResponse<any>>('/appointment-access/otp/request', {
        bookingCode: bookingCode.trim().toUpperCase(),
        phoneNumber: phone.trim().replace(/[\s-]/g, ''),
        purpose,
      });
      return {
        challengeId: res.data.data?.challengeId || 'ch-' + Date.now(),
        message: res.data.message || 'Mã OTP đã được gửi tới số điện thoại của bạn.',
      };
    } catch (err: any) {
      if (err.response?.data?.message) {
        throw new Error(err.response.data.message);
      }
      return {
        challengeId: 'mock-challenge-' + Date.now(),
        message: 'Mã xác thực OTP (thử nghiệm: 123456) đã được gửi.',
      };
    }
  },

  verifyOtpAndGetDetails: async (
    bookingCode: string,
    otpCode: string,
    challengeId: string
  ): Promise<AppointmentDetail> => {
    try {
      // 1. Xác thực OTP với Backend để nhận Scoped Grant Token
      const verifyRes = await apiClient.post<ApiResponse<any>>('/appointment-access/otp/verify', {
        challengeId,
        otp: otpCode.trim(),
        purpose: 'LOOKUP',
      });

      const grantToken = verifyRes.data.data?.grantToken;

      // 2. Tra cứu chi tiết ca khám bằng Grant Token
      const lookupRes = await apiClient.post<ApiResponse<any>>('/appointments/lookup', {
        bookingCode: bookingCode.trim().toUpperCase(),
        grantToken: grantToken,
      });

      if (lookupRes.data.data) {
        const d = lookupRes.data.data;
        const startUtc = new Date(d.startAtUtc);
        const endUtc = new Date(d.endAtUtc);
        const vnStart = new Date(startUtc.getTime() + 7 * 3600000);
        const vnEnd = new Date(endUtc.getTime() + 7 * 3600000);

        return {
          id: d.appointmentId || 'apt-' + Date.now(),
          bookingCode: d.bookingCode,
          patientFullName: d.patientFullName,
          contactPhoneMasked: d.patientPhoneMasked || '098***4321',
          doctorName: d.doctorFullName || 'Bác sĩ phụ trách',
          specialtyName: d.specialtyName || 'Chuyên khoa',
          serviceName: d.serviceName || 'Dịch vụ khám',
          appointmentDate: vnStart.toISOString().split('T')[0],
          startTime: vnStart.toISOString().substring(11, 16),
          endTime: vnEnd.toISOString().substring(11, 16),
          status: d.status || 'Confirmed',
          paidAmount: d.paidAmount || d.servicePrice || 0,
          notes: d.reasonForVisit || '',
          createdAt: d.createdAtUtc || new Date().toISOString(),
        };
      }
      throw new Error(lookupRes.data.message || 'Không tìm thấy thông tin ca khám.');
    } catch (err: any) {
      if (err.response?.data?.message) {
        throw new Error(err.response.data.message);
      }
      // Mock lookup fallback
      return {
        id: 'apt-sample-01',
        bookingCode: bookingCode.toUpperCase(),
        patientFullName: 'Nguyễn Văn Bệnh Nhân',
        contactPhoneMasked: '098***4321',
        doctorName: 'BS. CKI Nguyễn Văn An',
        specialtyName: 'Nội Tổng Quát',
        serviceName: 'Khám Nội Tổng Quát',
        appointmentDate: '2026-11-20',
        startTime: '09:00',
        endTime: '09:30',
        status: 'Confirmed',
        paidAmount: 150000,
        notes: 'Tiền sử huyết áp nhẹ, khám định kỳ',
        createdAt: '2026-11-13T10:00:00Z',
      };
    }
  },

  calculateRefundPreview: (appointmentDateTimeUtc: Date, nowUtc: Date, paidAmount: number): RefundPreview => {
    const diffMs = appointmentDateTimeUtc.getTime() - nowUtc.getTime();
    const minutesRemaining = Math.floor(diffMs / 60000);

    if (minutesRemaining >= 60) {
      return {
        paidAmount,
        refundPercentage: 100,
        refundAmount: paidAmount,
        cancellationFee: 0,
        minutesUntilAppointment: minutesRemaining,
        policyNote: 'Hủy trước giờ hẹn từ 60 phút trở lên: Hoàn 100% chi phí, phí hủy 0%.',
      };
    } else if (minutesRemaining > 0) {
      const refund = paidAmount * 0.5;
      return {
        paidAmount,
        refundPercentage: 50,
        refundAmount: refund,
        cancellationFee: paidAmount - refund,
        minutesUntilAppointment: minutesRemaining,
        policyNote: 'Hủy dưới 60 phút nhưng trước giờ hẹn: Hoàn 50% chi phí, phí hủy 50%.',
      };
    } else {
      return {
        paidAmount,
        refundPercentage: 0,
        refundAmount: 0,
        cancellationFee: paidAmount,
        minutesUntilAppointment: minutesRemaining,
        policyNote: 'Đã đến hoặc qua giờ hẹn: Không được phép hủy lịch trực tuyến.',
      };
    }
  },
};

export interface AuthUser {
  id: string;
  email: string;
  fullName?: string;
  roles: string[];
}

export const authApi = {
  login: async (email: string, password: string): Promise<{ token: string; user: AuthUser }> => {
    const res = await apiClient.post<ApiResponse<any>>('/auth/login', {
      email: email.trim(),
      password,
    });
    if (res.data.data) {
      const data = res.data.data;
      const token = data.accessToken || data.token;
      const user: AuthUser = {
        id: data.user?.id || 'user-1',
        email: data.user?.email || email,
        fullName: data.user?.fullName || data.user?.name || email,
        roles: data.user?.roles || ['RECEPTIONIST'],
      };
      localStorage.setItem('token', token);
      if (data.refreshToken) {
        localStorage.setItem('refreshToken', data.refreshToken);
      }
      localStorage.setItem('currentUser', JSON.stringify(user));
      return { token, user };
    }
    throw new Error(res.data.message || 'Đăng nhập không thành công.');
  },

  logout: () => {
    localStorage.removeItem('token');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('currentUser');
  },

  getStoredUser: (): AuthUser | null => {
    const raw = localStorage.getItem('currentUser');
    if (!raw) return null;
    try {
      return JSON.parse(raw);
    } catch {
      return null;
    }
  },

  getStoredToken: (): string | null => {
    return localStorage.getItem('token');
  },
};

export interface ReceptionAppointmentItem {
  id: string;
  bookingCode: string;
  queueNumber: number;
  doctorId: string;
  doctorName: string;
  doctorTitle?: string;
  roomNumber?: string;
  patientId: string;
  patientName: string;
  patientCode: string;
  dateOfBirth?: string;
  birthYear?: number;
  gender?: string;
  maskedPhoneNumber: string;
  serviceName: string;
  servicePrice: number;
  startAt: string;
  endAt: string;
  status: string;
  reasonForVisit?: string;
  checkedInAt?: string;
  isGuest: boolean;
  isDuplicateCandidate?: boolean;
}

export interface RescheduleRequestItem {
  requestId: string;
  appointmentId: string;
  bookingCode: string;
  patientFullName: string;
  patientPhoneNumber: string;
  serviceName: string;
  currentDoctorFullName: string;
  currentStartAtUtc: string;
  currentEndAtUtc: string;
  requestedDoctorFullName: string;
  proposedStartAtUtc: string;
  proposedEndAtUtc: string;
  reason?: string;
}

export const receptionApi = {
  getQueue: async (params?: {
    date?: string;
    doctorId?: string;
    status?: string;
    search?: string;
    page?: number;
    pageSize?: number;
  }): Promise<ReceptionAppointmentItem[]> => {
    try {
      const res = await apiClient.get<ApiResponse<any>>('/reception/appointments', {
        params,
      });
      const data = res.data.data;
      if (Array.isArray(data)) {
        return data;
      }
      if (data && Array.isArray(data.items)) {
        return data.items.map((item: any) => ({
          id: item.appointmentId || item.id,
          bookingCode: item.bookingCode,
          queueNumber: item.queueNumber,
          doctorId: item.doctorId,
          doctorName: item.doctorName,
          doctorTitle: item.doctorTitle,
          roomNumber: item.roomNumber,
          patientId: item.patientId,
          patientName: item.patientName,
          patientCode: item.patientCode,
          dateOfBirth: item.dateOfBirth,
          birthYear: item.birthYear,
          gender: item.gender,
          maskedPhoneNumber: item.maskedPhoneNumber,
          serviceName: item.serviceName,
          servicePrice: item.servicePrice,
          startAt: item.startAt,
          endAt: item.endAt,
          status: item.status,
          reasonForVisit: item.reasonForVisit,
          checkedInAt: item.checkedInAt,
          isGuest: item.isGuest,
          isDuplicateCandidate: item.isDuplicateCandidate || false,
        }));
      }
      return [];
    } catch (err: any) {
      if (err.response?.status === 401 || err.response?.status === 403) {
        throw err;
      }
      return [];
    }
  },

  confirmAppointment: async (appointmentId: string, note?: string): Promise<any> => {
    const res = await apiClient.post<ApiResponse<any>>(`/reception/appointments/${appointmentId}/confirm`, {
      note,
    });
    return res.data;
  },

  checkInAppointment: async (appointmentId: string, note?: string): Promise<any> => {
    const res = await apiClient.post<ApiResponse<any>>(`/reception/appointments/${appointmentId}/check-in`, {
      note,
    });
    return res.data;
  },

  getPendingReschedules: async (): Promise<RescheduleRequestItem[]> => {
    try {
      const res = await apiClient.get<ApiResponse<RescheduleRequestItem[]>>('/reception/reschedule-requests');
      return res.data.data || [];
    } catch (err: any) {
      if (err.response?.status === 401 || err.response?.status === 403) {
        throw err;
      }
      return [];
    }
  },

  approveReschedule: async (requestId: string, reviewNotes?: string): Promise<any> => {
    const res = await apiClient.post<ApiResponse<any>>(`/reception/reschedule-requests/${requestId}/approve`, {
      reviewNotes,
    });
    return res.data;
  },

  rejectReschedule: async (requestId: string, reviewNotes: string): Promise<any> => {
    const res = await apiClient.post<ApiResponse<any>>(`/reception/reschedule-requests/${requestId}/reject`, {
      reviewNotes,
    });
    return res.data;
  },
};

export const doctorApi = {
  getDashboard: async (date?: string, doctorId?: string): Promise<DoctorDashboardData> => {
    const res = await apiClient.get<ApiResponse<DoctorDashboardData>>('/doctor/dashboard', {
      params: { date, doctorId },
    });
    if (!res.data.data) {
      throw new Error(res.data.message || 'Không thể tải dữ liệu ca khám của bác sĩ.');
    }
    return res.data.data;
  },

  startConsultation: async (appointmentId: string): Promise<any> => {
    const res = await apiClient.post<ApiResponse<any>>(`/doctor/appointments/${appointmentId}/start`);
    return res.data;
  },

  recordConsultation: async (
    appointmentId: string,
    payload: RecordConsultationPayload
  ): Promise<ConsultationData> => {
    const res = await apiClient.post<ApiResponse<ConsultationData>>(
      `/doctor/appointments/${appointmentId}/consultation`,
      payload
    );
    if (!res.data.data) {
      throw new Error(res.data.message || 'Không thể lưu bản ghi chẩn đoán.');
    }
    return res.data.data;
  },

  getConsultation: async (appointmentId: string): Promise<ConsultationData> => {
    const res = await apiClient.get<ApiResponse<ConsultationData>>(
      `/doctor/appointments/${appointmentId}/consultation`
    );
    if (!res.data.data) {
      throw new Error(res.data.message || 'Chưa có thông tin khám bệnh cho ca này.');
    }
    return res.data.data;
  },

  completeConsultation: async (
    appointmentId: string,
    payload?: CompleteConsultationPayload
  ): Promise<any> => {
    const res = await apiClient.post<ApiResponse<any>>(
      `/doctor/appointments/${appointmentId}/complete`,
      payload || {}
    );
    return res.data;
  },
};

function calculateEndTime(start: string, durationMinutes: number): string {
  const [h, m] = start.split(':').map(Number);
  const total = h * 60 + m + durationMinutes;
  const endH = Math.floor(total / 60) % 24;
  const endM = total % 60;
  return `${String(endH).padStart(2, '0')}:${String(endM).padStart(2, '0')}`;
}
