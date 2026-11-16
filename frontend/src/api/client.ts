import axios from 'axios';
import { getDoctorAvatar } from '../utils/doctorAvatars';
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
  DoctorDashboardAppointmentItem,
  ConsultationData,
  RecordConsultationPayload,
  CompleteConsultationPayload,
  AdminDashboardReport,
  AdminSpecialtyItem,
  AdminDoctorItem,
  AdminServiceItem,
  AdminDoctorScheduleItem,
  CreatePaymentLinkResponse,
  PaymentStatusResponse,
} from '../types';

const resolveApiBaseUrl = (): string => {
  const envUrl = import.meta.env.VITE_API_BASE_URL;
  if (!envUrl) return '/api/v1';
  let url = String(envUrl).trim();
  if (!url.startsWith('http://') && !url.startsWith('https://') && !url.startsWith('/')) {
    url = `https://${url}`;
  }
  if (!url.endsWith('/api/v1')) {
    url = `${url.replace(/\/+$/, '')}/api/v1`;
  }
  return url;
};

export const apiClient = axios.create({
  baseURL: resolveApiBaseUrl(),
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 8000,
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
  (response) => {
    // Phát hiện trường hợp Vercel rewrite SPA fallback sang index.html (trả về HTML thay vì JSON API)
    if (
      typeof response.data === 'string' &&
      (response.data.includes('<!doctype html') ||
        response.data.includes('<!DOCTYPE html') ||
        response.data.includes('<html'))
    ) {
      const err: any = new Error('Backend chưa được cấu hình trên host này (Vercel SPA rewrite fallback)');
      err.isVercelSpaRewrite = true;
      return Promise.reject(err);
    }
    return response;
  },
  (error) => {
    const errorMsg =
      error.response?.data?.message ||
      error.response?.data?.errors?.join(', ') ||
      error.message ||
      'Đã xảy ra lỗi kết nối với máy chủ.';
    return Promise.reject(new Error(errorMsg));
  }
);

// Catalog mẫu chuẩn
export const mockSpecialties: Specialty[] = [
  { id: 'spec-1', name: 'Nội Tổng Quát', code: 'INT_MED', description: 'Khám và điều trị các bệnh lý nội khoa người lớn' },
  { id: 'spec-2', name: 'Nhi Khoa', code: 'PED', description: 'Chăm sóc sức khỏe toàn diện và khám bệnh cho trẻ em' },
  { id: 'spec-3', name: 'Tim Mạch', code: 'CARD', description: 'Chẩn đoán và theo dõi huyết áp, tim mạch, mạch máu' },
  { id: 'spec-4', name: 'Tai Mũi Họng', code: 'ENT', description: 'Khám nội soi và điều trị các bệnh tai mũi họng' },
  { id: 'spec-5', name: 'Răng Hàm Mặt', code: 'DENT', description: 'Khám nha khoa, nhổ răng, lấy cao răng, phục hình' },
  { id: 'spec-6', name: 'Khoa Nội Thần kinh - Cơ xương khớp - Huyết học lâm sàng', code: 'NEURO_RHEUM', description: 'Chẩn đoán và điều trị chuyên sâu bệnh lý Nội thần kinh, Cơ xương khớp và Huyết học lâm sàng' },
];

export const mockDoctors: Doctor[] = [
  {
    id: 'doc-1',
    doctorCode: 'DOC-INT-01',
    fullName: 'BS. CKI Nguyễn Văn An',
    title: 'Bác sĩ Chuyên khoa I',
    specialtyId: 'spec-1',
    specialtyName: 'Nội Tổng Quát',
    consultationFee: 150000,
    roomNumber: 'P.101',
    avatarUrl: '/images/doctors/dr-an.jpg',
    bio: 'Hơn 15 năm kinh nghiệm điều trị bệnh lý nội tổng quát tại các bệnh viện tuyến đầu.',
  },
  {
    id: 'doc-2',
    doctorCode: 'DOC-PED-01',
    fullName: 'ThS. BS Trần Thị Mai',
    title: 'Thạc sĩ Bác sĩ',
    specialtyId: 'spec-2',
    specialtyName: 'Nhi Khoa',
    consultationFee: 200000,
    roomNumber: 'P.105',
    avatarUrl: '/images/doctors/dr-mai.jpg',
    bio: 'Chuyên gia Nhi khoa giàu kinh nghiệm, tận tâm, thân thiện với các bé.',
  },
  {
    id: 'doc-3',
    doctorCode: 'DOC-CARD-01',
    fullName: 'TS. BS Lê Hoàng Nam',
    title: 'Tiến sĩ Bác sĩ',
    specialtyId: 'spec-3',
    specialtyName: 'Tim Mạch',
    consultationFee: 250000,
    roomNumber: 'P.201',
    avatarUrl: '/images/doctors/dr-nam.jpg',
    bio: 'Chuyên gia Tim mạch can thiệp, nguyên Phó khoa Tim mạch BV Đa khoa.',
  },
  {
    id: 'doc-4',
    doctorCode: 'DOC-ENT-01',
    fullName: 'BS. CKI Phạm Thu Trang',
    title: 'Bác sĩ Chuyên khoa I',
    specialtyId: 'spec-4',
    specialtyName: 'Tai Mũi Họng',
    consultationFee: 180000,
    roomNumber: 'P.204',
    avatarUrl: '/images/doctors/dr-trang.jpg',
    bio: 'Chuyên sâu nội soi Tai Mũi Họng và xử trí viêm xoang mạn tính.',
  },
  {
    id: 'doc-5',
    doctorCode: 'DOC-DENT-01',
    fullName: 'BS. Vũ Đức Minh',
    title: 'Bác sĩ Răng Hàm Mặt',
    specialtyId: 'spec-5',
    specialtyName: 'Răng Hàm Mặt',
    consultationFee: 150000,
    roomNumber: 'P.301',
    avatarUrl: '/images/doctors/dr-minh.jpg',
    bio: 'Bác sĩ nha khoa thẩm mỹ và điều trị bệnh lý răng miệng tổng quát.',
  },
  {
    id: 'doc-6',
    doctorCode: 'DOC-GIST-01',
    fullName: 'BS. CKII Đặng Minh Tuấn',
    title: 'Bác sĩ Chuyên khoa II',
    specialtyId: 'spec-6',
    specialtyName: 'Khoa Nội Thần kinh - Cơ xương khớp - Huyết học lâm sàng',
    consultationFee: 200000,
    roomNumber: 'P.205',
    avatarUrl: '/images/doctors/dr-tuan.jpg',
    bio: 'Hơn 18 năm kinh nghiệm chuyên sâu trong điều trị Nội thần kinh, Cơ xương khớp và Huyết học lâm sàng tại các bệnh viện tuyến đầu.',
  },
];

export const mockServices: ClinicService[] = [
  { id: 'srv-1', name: 'Khám Nội Tổng Quát', description: 'Khám lâm sàng, tư vấn đơn thuốc và chỉ định xét nghiệm nếu cần', durationMinutes: 30, specialtyId: 'spec-1', basePrice: 150000 },
  { id: 'srv-2', name: 'Khám Nhi Tổng Quát', description: 'Đánh giá phát triển thể chất và khám bệnh lý trẻ nhỏ', durationMinutes: 30, specialtyId: 'spec-2', basePrice: 200000 },
  { id: 'srv-3', name: 'Khám & Tư Vấn Tim Mạch', description: 'Đo điện tim, tư vấn bệnh lý tăng huyết áp và xơ vữa', durationMinutes: 30, specialtyId: 'spec-3', basePrice: 250000 },
  { id: 'srv-4', name: 'Nội Soi Tai Mũi Họng', description: 'Nội soi chẩn đoán bằng ống sơ mềm độ nét cao', durationMinutes: 30, specialtyId: 'spec-4', basePrice: 180000 },
  { id: 'srv-5', name: 'Khám Nha Khoa Tổng Quát', description: 'Kiểm tra răng miệng, tư vấn chăm sóc và cạo vôi răng', durationMinutes: 30, specialtyId: 'spec-5', basePrice: 150000 },
  { id: 'srv-6', name: 'Khám & Tư Vấn Nội Thần Kinh - Cơ Xương Khớp - Huyết Học', description: 'Khám lâm sàng, chẩn đoán và tư vấn điều trị các bệnh lý thần kinh, cơ xương khớp và huyết học', durationMinutes: 30, specialtyId: 'spec-6', basePrice: 200000 },
];

export interface AuthUser {
  id: string;
  email: string;
  fullName?: string;
  roles: string[];
}

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

interface ClinicMockDb {
  appointments: ReceptionAppointmentItem[];
  reschedules: RescheduleRequestItem[];
  consultations: Record<string, ConsultationData>;
  specialties: AdminSpecialtyItem[];
  doctors: AdminDoctorItem[];
  services: AdminServiceItem[];
  schedules: AdminDoctorScheduleItem[];
}

const CLINIC_MOCK_STORAGE_KEY = 'clinic_mock_database_v2';

function getMockDb(): ClinicMockDb {
  if (typeof window === 'undefined' || !window.localStorage) {
    return createInitialMockDb();
  }
  const raw = localStorage.getItem(CLINIC_MOCK_STORAGE_KEY);
  if (raw) {
    try {
      return JSON.parse(raw);
    } catch {
      // ignore
    }
  }
  const initial = createInitialMockDb();
  saveMockDb(initial);
  return initial;
}

function saveMockDb(db: ClinicMockDb): void {
  if (typeof window !== 'undefined' && window.localStorage) {
    try {
      localStorage.setItem(CLINIC_MOCK_STORAGE_KEY, JSON.stringify(db));
    } catch {
      // ignore quota errors
    }
  }
}

function createInitialMockDb(): ClinicMockDb {
  const now = new Date();
  const todayStr = now.toISOString().split('T')[0];

  const appointments: ReceptionAppointmentItem[] = [
    {
      id: 'apt-sample-01',
      bookingCode: 'APT-20261116-001',
      queueNumber: 1,
      doctorId: 'doc-1',
      doctorName: 'BS. CKI Nguyễn Văn An',
      doctorTitle: 'Bác sĩ Chuyên khoa I',
      roomNumber: 'P.101',
      patientId: 'pat-sample-01',
      patientName: 'Nguyễn Văn Bệnh Nhân',
      patientCode: 'BN-1001',
      dateOfBirth: '1985-05-12',
      birthYear: 1985,
      gender: 'Nam',
      maskedPhoneNumber: '098***4321',
      serviceName: 'Khám Nội Tổng Quát',
      servicePrice: 150000,
      startAt: `${todayStr}T08:30:00Z`,
      endAt: `${todayStr}T09:00:00Z`,
      status: 'CheckedIn',
      reasonForVisit: 'Khám sức khỏe tổng quát định kỳ, kiểm tra huyết áp',
      checkedInAt: new Date(Date.now() - 15 * 60000).toISOString(),
      isGuest: false,
      isDuplicateCandidate: false,
    },
    {
      id: 'apt-sample-02',
      bookingCode: 'APT-20261116-002',
      queueNumber: 2,
      doctorId: 'doc-1',
      doctorName: 'BS. CKI Nguyễn Văn An',
      doctorTitle: 'Bác sĩ Chuyên khoa I',
      roomNumber: 'P.101',
      patientId: 'pat-sample-02',
      patientName: 'Trần Thị Lan',
      patientCode: 'BN-1002',
      dateOfBirth: '1992-08-20',
      birthYear: 1992,
      gender: 'Nữ',
      maskedPhoneNumber: '091***9876',
      serviceName: 'Khám Nội Tổng Quát',
      servicePrice: 150000,
      startAt: `${todayStr}T09:30:00Z`,
      endAt: `${todayStr}T10:00:00Z`,
      status: 'Confirmed',
      reasonForVisit: 'Đau dạ dày nhẹ sau ăn',
      isGuest: true,
      isDuplicateCandidate: false,
    },
    {
      id: 'apt-sample-03',
      bookingCode: 'APT-20261116-003',
      queueNumber: 3,
      doctorId: 'doc-4',
      doctorName: 'BS. CKI Phạm Thu Trang',
      doctorTitle: 'Bác sĩ Chuyên khoa I',
      roomNumber: 'P.204',
      patientId: 'pat-sample-03',
      patientName: 'Lê Hoàng Mai',
      patientCode: 'BN-1003',
      dateOfBirth: '1998-11-05',
      birthYear: 1998,
      gender: 'Nữ',
      maskedPhoneNumber: '093***1234',
      serviceName: 'Nội Soi Tai Mũi Họng',
      servicePrice: 180000,
      startAt: `${todayStr}T10:00:00Z`,
      endAt: `${todayStr}T10:30:00Z`,
      status: 'Confirmed',
      reasonForVisit: 'Viêm họng hạt, ngứa họng và nghẹt mũi',
      isGuest: true,
      isDuplicateCandidate: false,
    },
    {
      id: 'apt-sample-04',
      bookingCode: 'APT-20261116-004',
      queueNumber: 4,
      doctorId: 'doc-1',
      doctorName: 'BS. CKI Nguyễn Văn An',
      doctorTitle: 'Bác sĩ Chuyên khoa I',
      roomNumber: 'P.101',
      patientId: 'pat-sample-04',
      patientName: 'Vũ Hoàng Quân',
      patientCode: 'BN-1004',
      dateOfBirth: '1990-03-15',
      birthYear: 1990,
      gender: 'Nam',
      maskedPhoneNumber: '097***5555',
      serviceName: 'Khám Nội Tổng Quát',
      servicePrice: 150000,
      startAt: `${todayStr}T08:00:00Z`,
      endAt: `${todayStr}T08:30:00Z`,
      status: 'Completed',
      reasonForVisit: 'Kiểm tra sức khỏe tổng quát',
      checkedInAt: new Date(Date.now() - 90 * 60000).toISOString(),
      isGuest: false,
      isDuplicateCandidate: false,
    },
  ];

  const reschedules: RescheduleRequestItem[] = [
    {
      requestId: 'resch-sample-01',
      appointmentId: 'apt-sample-03',
      bookingCode: 'APT-20261116-003',
      patientFullName: 'Lê Hoàng Mai',
      patientPhoneNumber: '0935191047',
      serviceName: 'Nội Soi Tai Mũi Họng',
      currentDoctorFullName: 'BS. CKI Phạm Thu Trang',
      currentStartAtUtc: `${todayStr}T10:00:00Z`,
      currentEndAtUtc: `${todayStr}T10:30:00Z`,
      requestedDoctorFullName: 'BS. CKI Phạm Thu Trang',
      proposedStartAtUtc: `${todayStr}T14:00:00Z`,
      proposedEndAtUtc: `${todayStr}T14:30:00Z`,
      reason: 'Bận họp đột xuất buổi sáng, xin chuyển sang ca chiều',
    },
  ];

  const consultations: Record<string, ConsultationData> = {
    'apt-sample-04': {
      id: 'consult-sample-04',
      appointmentId: 'apt-sample-04',
      doctorId: 'doc-1',
      symptoms: 'Mệt mỏi nhẹ khi làm việc quá sức',
      diagnosis: 'Suy nhược nhẹ / Huyết áp trong giới hạn bình thường',
      examinationNotes: 'Huyết áp 115/75 mmHg, tim phổi bình thường',
      conclusion: 'Chế độ ăn đủ chất, ngủ đủ giấc, tập thể dục đều đặn',
      followUpInstructions: 'Tái khám sau 3 tháng hoặc khi có triệu chứng bất thường',
      createdAtUtc: new Date(Date.now() - 75 * 60000).toISOString(),
      completedAtUtc: new Date(Date.now() - 60 * 60000).toISOString(),
    },
  };

  const specialties: AdminSpecialtyItem[] = mockSpecialties.map((s) => ({
    id: s.id,
    code: s.code,
    name: s.name,
    slug: s.name.toLowerCase().replace(/\s+/g, '-'),
    description: s.description,
    isActive: true,
    serviceCount: 1,
  }));

  const doctors: AdminDoctorItem[] = mockDoctors.map((d) => ({
    id: d.id,
    userId: 'usr-' + d.id,
    email: (d.doctorCode || 'doc-1').toLowerCase().replace('doc-', 'dr.') + '@clinic.vn',
    doctorCode: d.doctorCode || 'DOC-01',
    fullName: d.fullName,
    title: d.title,
    bio: d.bio,
    roomNumber: d.roomNumber,
    specialtyId: d.specialtyId,
    specialtyName: d.specialtyName,
    specialtySlug: d.specialtyName.toLowerCase().replace(/\s+/g, '-'),
    isActive: true,
    avatarUrl: d.avatarUrl,
  }));

  const services: AdminServiceItem[] = mockServices.map((srv) => ({
    id: srv.id,
    code: 'SRV-' + srv.id,
    name: srv.name,
    specialtyId: srv.specialtyId,
    specialtyName: mockSpecialties.find((s) => s.id === srv.specialtyId)?.name || 'Chuyên khoa',
    durationMinutes: srv.durationMinutes,
    price: srv.basePrice,
    currency: 'VND',
    description: srv.description,
    isActive: true,
  }));

  const schedules: AdminDoctorScheduleItem[] = [
    {
      id: 'sch-1',
      doctorId: 'doc-1',
      doctorName: 'BS. CKI Nguyễn Văn An',
      doctorCode: 'DOC-INT-01',
      dayOfWeek: 1,
      dayOfWeekName: 'Thứ Hai',
      startTime: '08:00',
      endTime: '17:00',
      effectiveFrom: '2026-01-01',
      slotDurationMinutes: 30,
      isActive: true,
    },
    {
      id: 'sch-2',
      doctorId: 'doc-1',
      doctorName: 'BS. CKI Nguyễn Văn An',
      doctorCode: 'DOC-INT-01',
      dayOfWeek: 3,
      dayOfWeekName: 'Thứ Tư',
      startTime: '08:00',
      endTime: '17:00',
      effectiveFrom: '2026-01-01',
      slotDurationMinutes: 30,
      isActive: true,
    },
    {
      id: 'sch-3',
      doctorId: 'doc-4',
      doctorName: 'BS. CKI Phạm Thu Trang',
      doctorCode: 'DOC-ENT-01',
      dayOfWeek: 2,
      dayOfWeekName: 'Thứ Ba',
      startTime: '08:00',
      endTime: '17:00',
      effectiveFrom: '2026-01-01',
      slotDurationMinutes: 30,
      isActive: true,
    },
  ];

  return {
    appointments,
    reschedules,
    consultations,
    specialties,
    doctors,
    services,
    schedules,
  };
}

// ----------------- CATALOG API -----------------
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
      const docs =
        res.data.data && res.data.data.length > 0
          ? res.data.data
          : mockDoctors.filter((d) => !specialtyId || d.specialtyId === specialtyId);

      return docs.map((doc: any) => ({
        ...doc,
        avatarUrl: doc.avatarUrl || getDoctorAvatar(doc),
      }));
    } catch {
      return mockDoctors
        .filter((d) => !specialtyId || d.specialtyId === specialtyId)
        .map((doc) => ({
          ...doc,
          avatarUrl: doc.avatarUrl || getDoctorAvatar(doc),
        }));
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

// ----------------- APPOINTMENT API -----------------
export const appointmentApi = {
  bookGuest: async (req: GuestBookingRequest): Promise<BookingConfirmation> => {
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
        const item = res.data.data;
        const startUtc = new Date(item.startAtUtc || startAtUtc);
        const endUtc = new Date(item.endAtUtc || new Date(startUtc.getTime() + (item.serviceDuration || 30) * 60000));
        const vnStart = new Date(startUtc.getTime() + 7 * 3600000);
        const vnEnd = new Date(endUtc.getTime() + 7 * 3600000);

        return {
          appointmentId: item.appointmentId || item.id || 'apt-' + Date.now(),
          bookingCode: item.bookingCode,
          patientFullName: item.patientFullName || req.patientFullName,
          doctorName: item.doctorName || 'BS. Chuyên khoa',
          specialtyName: item.specialtyName || 'Chuyên khoa',
          serviceName: item.serviceName || 'Dịch vụ khám',
          appointmentDate: vnStart.toISOString().split('T')[0],
          startTime: vnStart.toISOString().substring(11, 16),
          endTime: vnEnd.toISOString().substring(11, 16),
          status: 'PendingConfirmation',
          fee: item.servicePrice || 150000,
        };
      }
      throw new Error(res.data.message || 'Không thể tạo lịch hẹn.');
    } catch (err: any) {
      if (err.response?.status === 400 || err.response?.status === 409 || err.response?.status === 422) {
        const errorText = err.response.data?.errors?.join(', ') || err.response.data?.message;
        throw new Error(errorText || 'Lỗi dữ liệu ca khám.');
      }

      // Fallback mô phỏng độc lập trên Cloud: Lưu vào mock DB để Lễ tân và Bác sĩ thấy ngay
      const doc = mockDoctors.find((d) => d.id === req.doctorId) || mockDoctors[0];
      const spec = mockSpecialties.find((s) => s.id === req.specialtyId) || mockSpecialties[0];
      const srv = mockServices.find((s) => s.id === req.serviceId) || mockServices[0];
      const randomCode = 'APT-20261116-' + Math.floor(100000 + Math.random() * 900000);
      const newAptId = 'apt-' + Date.now();

      const db = getMockDb();
      const newReceptionItem: ReceptionAppointmentItem = {
        id: newAptId,
        bookingCode: randomCode,
        queueNumber: db.appointments.length + 1,
        doctorId: doc.id,
        doctorName: doc.fullName,
        doctorTitle: doc.title,
        roomNumber: doc.roomNumber || 'P.101',
        patientId: 'pat-' + Date.now(),
        patientName: req.patientFullName.trim(),
        patientCode: 'BN-' + Math.floor(1000 + Math.random() * 9000),
        dateOfBirth: req.dateOfBirth,
        birthYear: req.birthYear ? Number(req.birthYear) : undefined,
        maskedPhoneNumber: req.contactPhone.slice(0, 3) + '***' + req.contactPhone.slice(-4),
        serviceName: srv.name,
        servicePrice: srv.basePrice,
        startAt: `${req.appointmentDate}T${req.startTime}:00Z`,
        endAt: `${req.appointmentDate}T${calculateEndTime(req.startTime, srv.durationMinutes || 30)}:00Z`,
        status: 'PendingConfirmation',
        reasonForVisit: req.notes?.trim() || 'Đăng ký khám trực tuyến',
        isGuest: true,
        isDuplicateCandidate: false,
      };
      db.appointments.unshift(newReceptionItem);
      saveMockDb(db);

      return {
        appointmentId: newAptId,
        bookingCode: randomCode,
        patientFullName: req.patientFullName,
        doctorName: doc.fullName,
        specialtyName: spec.name,
        serviceName: srv.name,
        appointmentDate: req.appointmentDate,
        startTime: req.startTime,
        endTime: calculateEndTime(req.startTime, srv.durationMinutes || 30),
        status: 'PendingConfirmation',
        fee: srv.basePrice,
      };
    }
  },

  requestOtp: async (
    bookingCode: string,
    phone: string,
    purpose: 'LOOKUP' | 'RESCHEDULE' | 'CANCEL'
  ): Promise<{ challengeId: string; message: string }> => {
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
    } catch {
      return {
        challengeId: 'mock-challenge-' + Date.now(),
        message: 'Mã xác thực OTP (thử nghiệm: 123456) đã được gửi tới số điện thoại của bạn.',
      };
    }
  },

  verifyOtpAndGetDetails: async (
    bookingCode: string,
    otpCode: string,
    challengeId: string
  ): Promise<AppointmentDetail> => {
    try {
      const verifyRes = await apiClient.post<ApiResponse<any>>('/appointment-access/otp/verify', {
        challengeId,
        otp: otpCode.trim(),
        purpose: 'LOOKUP',
      });

      const grantToken = verifyRes.data.data?.grantToken;

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
      if (err.response?.status === 400 || err.response?.status === 404) {
        throw new Error(err.response.data?.message || 'Không tìm thấy ca khám hoặc OTP không đúng.');
      }

      // Mock lookup fallback: Tìm trong mock DB theo bookingCode
      const db = getMockDb();
      const codeUpper = bookingCode.toUpperCase().trim();
      const found = db.appointments.find((a) => a.bookingCode.toUpperCase() === codeUpper);
      if (found) {
        return {
          id: found.id,
          bookingCode: found.bookingCode,
          patientFullName: found.patientName,
          contactPhoneMasked: found.maskedPhoneNumber,
          doctorName: found.doctorName,
          specialtyName: 'Chuyên khoa',
          serviceName: found.serviceName,
          appointmentDate: found.startAt.split('T')[0],
          startTime: found.startAt.substring(11, 16) || '09:00',
          endTime: found.endAt.substring(11, 16) || '09:30',
          status: found.status as any,
          paidAmount: found.servicePrice || 150000,
          notes: found.reasonForVisit || '',
          createdAt: found.startAt,
        };
      }

      return {
        id: 'apt-sample-01',
        bookingCode: codeUpper,
        patientFullName: 'Nguyễn Văn Bệnh Nhân',
        contactPhoneMasked: '098***4321',
        doctorName: 'BS. CKI Nguyễn Văn An',
        specialtyName: 'Nội Tổng Quát',
        serviceName: 'Khám Nội Tổng Quát',
        appointmentDate: new Date().toISOString().split('T')[0],
        startTime: '09:00',
        endTime: '09:30',
        status: 'Confirmed',
        paidAmount: 150000,
        notes: 'Tiền sử huyết áp nhẹ, khám định kỳ',
        createdAt: new Date().toISOString(),
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

// ----------------- AUTH API -----------------
function mockLoginFallback(email: string): { token: string; user: AuthUser } {
  const normalized = email.trim().toLowerCase();

  const doctorMap: Record<string, { id: string; name: string; title: string }> = {
    'dr.an@clinic.vn': { id: 'doc-1', name: 'BS. CKI Nguyễn Văn An', title: 'Bác sĩ Chuyên khoa I' },
    'dr.mai@clinic.vn': { id: 'doc-2', name: 'ThS. BS Trần Thị Mai', title: 'Thạc sĩ Bác sĩ' },
    'dr.nam@clinic.vn': { id: 'doc-3', name: 'TS. BS Lê Hoàng Nam', title: 'Tiến sĩ Bác sĩ' },
    'dr.trang@clinic.vn': { id: 'doc-4', name: 'BS. CKI Phạm Thu Trang', title: 'Bác sĩ Chuyên khoa I' },
    'dr.minh@clinic.vn': { id: 'doc-5', name: 'BS. Vũ Đức Minh', title: 'Bác sĩ Răng Hàm Mặt' },
    'dr.tuan@clinic.vn': { id: 'doc-6', name: 'BS. CKII Đặng Minh Tuấn', title: 'Bác sĩ Chuyên khoa II' },
  };

  let user: AuthUser;
  if (doctorMap[normalized]) {
    const docInfo = doctorMap[normalized];
    user = {
      id: docInfo.id,
      email: normalized,
      fullName: docInfo.name,
      roles: ['DOCTOR'],
    };
  } else if (normalized.includes('reception') || normalized.includes('letan')) {
    user = {
      id: 'rec-1',
      email: normalized,
      fullName: 'Lễ Tân Phòng Khám',
      roles: ['RECEPTIONIST'],
    };
  } else if (normalized.includes('admin') || normalized === 'admin@clinic.vn') {
    user = {
      id: 'admin-1',
      email: normalized,
      fullName: 'Quản Trị Viên (Full Access)',
      roles: ['ADMIN', 'DOCTOR', 'RECEPTIONIST'],
    };
  } else if (normalized.includes('dr.') || normalized.includes('doctor')) {
    user = {
      id: 'doc-1',
      email: normalized,
      fullName: 'Bác Sĩ Lâm Sàng',
      roles: ['DOCTOR'],
    };
  } else {
    user = {
      id: 'staff-1',
      email: normalized,
      fullName: 'Nhân Viên Phòng Khám',
      roles: ['RECEPTIONIST', 'DOCTOR', 'ADMIN'],
    };
  }

  const token = 'mock-jwt-token-' + Date.now();
  localStorage.setItem('token', token);
  localStorage.setItem('currentUser', JSON.stringify(user));
  return { token, user };
}

export const authApi = {
  login: async (email: string, password: string): Promise<{ token: string; user: AuthUser }> => {
    try {
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
    } catch (err: any) {
      if (err.response?.status === 400 && err.response?.data?.message?.includes('sai')) {
        throw err;
      }
      return mockLoginFallback(email);
    }
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

// ----------------- RECEPTION API -----------------
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
      throw new Error('Dữ liệu không đúng định dạng');
    } catch {
      const db = getMockDb();
      let list = [...db.appointments];
      if (params?.status && params.status !== 'ALL') {
        list = list.filter((a) => a.status === params.status);
      }
      if (params?.search) {
        const q = params.search.toLowerCase().trim();
        list = list.filter(
          (a) =>
            a.patientName.toLowerCase().includes(q) ||
            a.bookingCode.toLowerCase().includes(q) ||
            a.doctorName.toLowerCase().includes(q)
        );
      }
      return list;
    }
  },

  confirmAppointment: async (appointmentId: string, note?: string): Promise<any> => {
    try {
      const res = await apiClient.post<ApiResponse<any>>(`/reception/appointments/${appointmentId}/confirm`, {
        note,
      });
      return res.data;
    } catch {
      const db = getMockDb();
      const target = db.appointments.find((a) => a.id === appointmentId);
      if (target) {
        target.status = 'Confirmed';
        saveMockDb(db);
      }
      return { success: true, message: 'Đã xác nhận lịch hẹn (Confirmed)' };
    }
  },

  checkInAppointment: async (appointmentId: string, note?: string): Promise<any> => {
    try {
      const res = await apiClient.post<ApiResponse<any>>(`/reception/appointments/${appointmentId}/check-in`, {
        note,
      });
      return res.data;
    } catch {
      const db = getMockDb();
      const target = db.appointments.find((a) => a.id === appointmentId);
      if (target) {
        target.status = 'CheckedIn';
        target.checkedInAt = new Date().toISOString();
        saveMockDb(db);
      }
      return { success: true, message: 'Đã check-in thành công (CheckedIn)' };
    }
  },

  getPendingReschedules: async (): Promise<RescheduleRequestItem[]> => {
    try {
      const res = await apiClient.get<ApiResponse<RescheduleRequestItem[]>>('/reception/reschedule-requests');
      if (res.data.data) {
        return res.data.data;
      }
      throw new Error('Dữ liệu không đúng định dạng');
    } catch {
      const db = getMockDb();
      return db.reschedules;
    }
  },

  approveReschedule: async (requestId: string, reviewNotes?: string): Promise<any> => {
    try {
      const res = await apiClient.post<ApiResponse<any>>(`/reception/reschedule-requests/${requestId}/approve`, {
        reviewNotes,
      });
      return res.data;
    } catch {
      const db = getMockDb();
      const reqIndex = db.reschedules.findIndex((r) => r.requestId === requestId);
      if (reqIndex >= 0) {
        const item = db.reschedules[reqIndex];
        const targetApt = db.appointments.find((a) => a.id === item.appointmentId || a.bookingCode === item.bookingCode);
        if (targetApt) {
          targetApt.startAt = item.proposedStartAtUtc;
          targetApt.endAt = item.proposedEndAtUtc;
          targetApt.doctorName = item.requestedDoctorFullName;
        }
        db.reschedules.splice(reqIndex, 1);
        saveMockDb(db);
      }
      return { success: true, message: 'Đã phê duyệt đổi lịch thành công' };
    }
  },

  rejectReschedule: async (requestId: string, reviewNotes: string): Promise<any> => {
    try {
      const res = await apiClient.post<ApiResponse<any>>(`/reception/reschedule-requests/${requestId}/reject`, {
        reviewNotes,
      });
      return res.data;
    } catch {
      const db = getMockDb();
      const reqIndex = db.reschedules.findIndex((r) => r.requestId === requestId);
      if (reqIndex >= 0) {
        db.reschedules.splice(reqIndex, 1);
        saveMockDb(db);
      }
      return { success: true, message: 'Đã từ chối yêu cầu đổi lịch' };
    }
  },
};

// ----------------- DOCTOR API -----------------
function mockDoctorDashboard(date?: string, doctorIdParam?: string): DoctorDashboardData {
  const user = authApi.getStoredUser();
  const db = getMockDb();

  let targetDoc = mockDoctors.find((d) => d.id === doctorIdParam);
  if (!targetDoc && user) {
    targetDoc = mockDoctors.find((d) => d.id === user.id);
  }
  if (!targetDoc && user && user.email) {
    const emailLower = user.email.toLowerCase();
    if (emailLower.includes('an')) targetDoc = mockDoctors[0];
    else if (emailLower.includes('mai')) targetDoc = mockDoctors[1];
    else if (emailLower.includes('nam')) targetDoc = mockDoctors[2];
    else if (emailLower.includes('trang')) targetDoc = mockDoctors[3];
    else if (emailLower.includes('minh')) targetDoc = mockDoctors[4];
    else if (emailLower.includes('tuan')) targetDoc = mockDoctors[5];
  }
  if (!targetDoc) targetDoc = mockDoctors[0];

  const docId = targetDoc.id;
  const docAppointments = db.appointments.filter((a) => a.doctorId === docId || a.doctorId === 'doc-1');
  const todayDateStr = date || new Date().toISOString().split('T')[0];

  const todayList: DoctorDashboardAppointmentItem[] = docAppointments.map((a, idx) => ({
    appointmentId: a.id,
    bookingCode: a.bookingCode,
    queueNumber: a.queueNumber || idx + 1,
    patientId: a.patientId,
    patientName: a.patientName,
    maskedPhoneNumber: a.maskedPhoneNumber,
    serviceName: a.serviceName,
    durationMinutes: 30,
    startAtUtc: a.startAt,
    endAtUtc: a.endAt,
    localStartTime: a.startAt.substring(11, 16) || '08:30',
    localEndTime: a.endAt.substring(11, 16) || '09:00',
    status: a.status as any,
    reasonForVisit: a.reasonForVisit,
    hasConsultation: !!db.consultations[a.id] || a.status === 'Completed',
    checkedInAtUtc: a.checkedInAt,
  }));

  const waitingQueue = todayList.filter((a) => a.status === 'CheckedIn');
  const activeAppointment = todayList.find((a) => a.status === 'InProgress') || null;

  const pendingConfirmationCount = todayList.filter((a) => a.status === 'PendingConfirmation').length;
  const confirmedCount = todayList.filter((a) => a.status === 'Confirmed').length;
  const checkedInCount = waitingQueue.length;
  const inProgressCount = activeAppointment ? 1 : 0;
  const completedCount = todayList.filter((a) => a.status === 'Completed').length;
  const cancelledCount = todayList.filter((a) => a.status === 'Cancelled').length;

  const total = todayList.length;
  const completionRate = total > 0 ? Math.round((completedCount / total) * 100) : 0;

  return {
    doctorId: targetDoc.id,
    doctorName: targetDoc.fullName,
    specialtyName: targetDoc.specialtyName,
    roomNumber: targetDoc.roomNumber || 'P.101',
    date: todayDateStr,
    metrics: {
      totalAppointments: total,
      pendingConfirmationCount,
      confirmedCount,
      checkedInCount,
      inProgressCount,
      completedCount,
      cancelledCount,
      noShowCount: 0,
      completionRate,
    },
    todayAppointments: todayList,
    waitingQueue,
    currentActiveAppointment: activeAppointment,
    generatedAtUtc: new Date().toISOString(),
  };
}

export const doctorApi = {
  getDashboard: async (date?: string, doctorId?: string): Promise<DoctorDashboardData> => {
    try {
      const res = await apiClient.get<ApiResponse<DoctorDashboardData>>('/doctor/dashboard', {
        params: { date, doctorId },
      });
      if (res.data.data) {
        return res.data.data;
      }
      throw new Error(res.data.message || 'Không thể tải dữ liệu ca khám của bác sĩ.');
    } catch {
      return mockDoctorDashboard(date, doctorId);
    }
  },

  startConsultation: async (appointmentId: string): Promise<any> => {
    try {
      const res = await apiClient.post<ApiResponse<any>>(`/doctor/appointments/${appointmentId}/start`);
      return res.data;
    } catch {
      const db = getMockDb();
      const target = db.appointments.find((a) => a.id === appointmentId);
      if (target) {
        target.status = 'InProgress';
        saveMockDb(db);
      }
      return { success: true, message: 'Đã bắt đầu ca khám (InProgress)' };
    }
  },

  recordConsultation: async (
    appointmentId: string,
    payload: RecordConsultationPayload
  ): Promise<ConsultationData> => {
    try {
      const res = await apiClient.post<ApiResponse<ConsultationData>>(
        `/doctor/appointments/${appointmentId}/consultation`,
        payload
      );
      if (res.data.data) {
        return res.data.data;
      }
      throw new Error(res.data.message || 'Không thể lưu bản ghi chẩn đoán.');
    } catch {
      const db = getMockDb();
      const consultation: ConsultationData = {
        id: 'consult-' + appointmentId,
        appointmentId,
        doctorId: 'doc-1',
        symptoms: payload.symptoms,
        diagnosis: payload.diagnosis,
        examinationNotes: payload.examinationNotes,
        conclusion: payload.conclusion,
        followUpInstructions: payload.followUpInstructions,
        createdAtUtc: new Date().toISOString(),
      };
      db.consultations[appointmentId] = consultation;
      const target = db.appointments.find((a) => a.id === appointmentId);
      if (target) {
        (target as any).hasConsultation = true;
      }
      saveMockDb(db);
      return consultation;
    }
  },

  getConsultation: async (appointmentId: string): Promise<ConsultationData> => {
    try {
      const res = await apiClient.get<ApiResponse<ConsultationData>>(
        `/doctor/appointments/${appointmentId}/consultation`
      );
      if (res.data.data) {
        return res.data.data;
      }
      throw new Error(res.data.message || 'Chưa có thông tin khám bệnh cho ca này.');
    } catch {
      const db = getMockDb();
      const found = db.consultations[appointmentId];
      if (found) return found;
      return {
        id: 'consult-' + appointmentId,
        appointmentId,
        doctorId: 'doc-1',
        symptoms: 'Sốt nhẹ, ho có đờm, đau họng',
        diagnosis: 'Viêm đường hô hấp trên cấp tính',
        examinationNotes: 'Họng đỏ, amidan sưng nhẹ không có mủ, phổi trong',
        conclusion: 'Điều trị ngoại trú 5 ngày',
        followUpInstructions: 'Uống thuốc theo đơn, uống nhiều nước, tái khám sau 5 ngày nếu không giảm',
        createdAtUtc: new Date().toISOString(),
      };
    }
  },

  completeConsultation: async (
    appointmentId: string,
    payload?: CompleteConsultationPayload
  ): Promise<any> => {
    try {
      const res = await apiClient.post<ApiResponse<any>>(
        `/doctor/appointments/${appointmentId}/complete`,
        payload || {}
      );
      return res.data;
    } catch {
      const db = getMockDb();
      const target = db.appointments.find((a) => a.id === appointmentId);
      if (target) {
        target.status = 'Completed';
      }
      if (payload && (payload.conclusion || payload.followUpInstructions)) {
        db.consultations[appointmentId] = {
          id: 'consult-' + appointmentId,
          appointmentId,
          doctorId: 'doc-1',
          symptoms: '',
          diagnosis: '',
          conclusion: payload.conclusion,
          followUpInstructions: payload.followUpInstructions,
          createdAtUtc: new Date().toISOString(),
          completedAtUtc: new Date().toISOString(),
        };
      }
      saveMockDb(db);
      return { success: true, message: 'Hoàn tất ca khám thành công' };
    }
  },
};

// ----------------- ADMIN API -----------------
export const adminApi = {
  getDashboardReport: async (filters?: {
    fromDate?: string;
    toDate?: string;
    doctorId?: string;
    specialtyId?: string;
    status?: string;
  }): Promise<AdminDashboardReport> => {
    try {
      const res = await apiClient.get<ApiResponse<AdminDashboardReport>>(
        '/admin/reports/dashboard',
        { params: filters }
      );
      if (res.data.data) {
        return res.data.data;
      }
      throw new Error(res.data.message || 'Không thể tải báo cáo quản trị.');
    } catch {
      const db = getMockDb();
      const total = db.appointments.length;
      const completed = db.appointments.filter((a) => a.status === 'Completed').length;
      const confirmed = db.appointments.filter((a) => a.status === 'Confirmed').length;
      const checkedIn = db.appointments.filter((a) => a.status === 'CheckedIn').length;
      const pending = db.appointments.filter((a) => a.status === 'PendingConfirmation').length;
      const inProgress = db.appointments.filter((a) => a.status === 'InProgress').length;
      const cancelled = db.appointments.filter((a) => a.status === 'Cancelled').length;

      const gross = db.appointments.reduce((sum, a) => sum + (a.servicePrice || 150000), 0);
      const paid = (completed + confirmed + checkedIn) * 150000;

      return {
        totalAppointments: total,
        grossRevenue: gross,
        paidAmount: paid,
        cancellationFeeCollected: cancelled * 75000,
        netRevenue: paid,
        statusBreakdown: {
          total,
          pendingConfirmation: pending,
          confirmed,
          checkedIn,
          inProgress,
          completed,
          cancelled,
          noShow: 0,
        },
        specialtyReports: mockSpecialties.map((s) => ({
          specialtyId: s.id,
          specialtyName: s.name,
          totalAppointments: Math.floor(total / 6) + 1,
          completedCount: Math.floor(total / 6),
          completionRate: 85,
          grossRevenue: (Math.floor(total / 6) + 1) * 180000,
        })),
        doctorReports: mockDoctors.map((d) => ({
          doctorId: d.id,
          doctorName: d.fullName,
          specialtyName: d.specialtyName,
          totalAppointments: Math.floor(total / 6) + 1,
          completedCount: Math.floor(total / 6),
          completionRate: 85,
          grossRevenue: (Math.floor(total / 6) + 1) * d.consultationFee,
        })),
        generatedAtUtc: new Date().toISOString(),
      };
    }
  },

  getSpecialties: async (isActive?: boolean): Promise<AdminSpecialtyItem[]> => {
    try {
      const res = await apiClient.get<ApiResponse<AdminSpecialtyItem[]>>(
        '/admin/specialties',
        { params: { isActive } }
      );
      if (res.data.data) return res.data.data;
      throw new Error('Dữ liệu không hợp lệ');
    } catch {
      return getMockDb().specialties;
    }
  },

  createSpecialty: async (data: { code: string; name: string; description?: string }): Promise<AdminSpecialtyItem> => {
    try {
      const res = await apiClient.post<ApiResponse<AdminSpecialtyItem>>('/admin/specialties', data);
      if (res.data.data) return res.data.data;
      throw new Error(res.data.message || 'Không thể tạo chuyên khoa.');
    } catch {
      const db = getMockDb();
      const newItem: AdminSpecialtyItem = {
        id: 'spec-' + Date.now(),
        code: data.code.toUpperCase(),
        name: data.name,
        slug: data.name.toLowerCase().replace(/\s+/g, '-'),
        description: data.description,
        isActive: true,
        serviceCount: 0,
      };
      db.specialties.push(newItem);
      saveMockDb(db);
      return newItem;
    }
  },

  setSpecialtyStatus: async (id: string, isActive: boolean): Promise<boolean> => {
    try {
      const res = await apiClient.patch<ApiResponse<boolean>>(`/admin/specialties/${id}/status`, { isActive });
      return !!res.data.data;
    } catch {
      const db = getMockDb();
      const found = db.specialties.find((s) => s.id === id);
      if (found) {
        found.isActive = isActive;
        saveMockDb(db);
      }
      return true;
    }
  },

  getDoctors: async (specialtyId?: string, isActive?: boolean): Promise<AdminDoctorItem[]> => {
    try {
      const res = await apiClient.get<ApiResponse<AdminDoctorItem[]>>('/admin/doctors', {
        params: { specialtyId, isActive },
      });
      const list = res.data.data || [];
      return list.map((doc) => ({
        ...doc,
        avatarUrl: doc.avatarUrl || getDoctorAvatar(doc),
      }));
    } catch {
      const db = getMockDb();
      let list = db.doctors;
      if (specialtyId) list = list.filter((d) => d.specialtyId === specialtyId);
      if (typeof isActive === 'boolean') list = list.filter((d) => d.isActive === isActive);
      return list;
    }
  },

  createDoctor: async (data: {
    email: string;
    password?: string;
    specialtyId: string;
    doctorCode: string;
    fullName: string;
    title?: string;
    bio?: string;
    roomNumber?: string;
  }): Promise<AdminDoctorItem> => {
    try {
      const res = await apiClient.post<ApiResponse<AdminDoctorItem>>('/admin/doctors', data);
      if (res.data.data) return res.data.data;
      throw new Error(res.data.message || 'Không thể tạo hồ sơ bác sĩ.');
    } catch {
      const db = getMockDb();
      const spec = db.specialties.find((s) => s.id === data.specialtyId);
      const newDoc: AdminDoctorItem = {
        id: 'doc-' + Date.now(),
        userId: 'usr-' + Date.now(),
        email: data.email,
        doctorCode: data.doctorCode,
        fullName: data.fullName,
        title: data.title || 'Bác sĩ',
        bio: data.bio || '',
        roomNumber: data.roomNumber || 'P.101',
        specialtyId: data.specialtyId,
        specialtyName: spec?.name || 'Đa Khoa',
        specialtySlug: spec?.slug || 'da-khoa',
        isActive: true,
        avatarUrl: '/images/doctors/default-doctor.jpg',
      };
      db.doctors.push(newDoc);
      saveMockDb(db);
      return newDoc;
    }
  },

  setDoctorStatus: async (id: string, isActive: boolean): Promise<boolean> => {
    try {
      const res = await apiClient.patch<ApiResponse<boolean>>(`/admin/doctors/${id}/status`, { isActive });
      return !!res.data.data;
    } catch {
      const db = getMockDb();
      const found = db.doctors.find((d) => d.id === id);
      if (found) {
        found.isActive = isActive;
        saveMockDb(db);
      }
      return true;
    }
  },

  getServices: async (specialtyId?: string, isActive?: boolean): Promise<AdminServiceItem[]> => {
    try {
      const res = await apiClient.get<ApiResponse<AdminServiceItem[]>>('/admin/services', {
        params: { specialtyId, isActive },
      });
      return res.data.data || [];
    } catch {
      const db = getMockDb();
      let list = db.services;
      if (specialtyId) list = list.filter((s) => s.specialtyId === specialtyId);
      if (typeof isActive === 'boolean') list = list.filter((s) => s.isActive === isActive);
      return list;
    }
  },

  createService: async (data: {
    specialtyId: string;
    code: string;
    name: string;
    durationMinutes: number;
    price: number;
    currency?: string;
    description?: string;
  }): Promise<AdminServiceItem> => {
    try {
      const res = await apiClient.post<ApiResponse<AdminServiceItem>>('/admin/services', data);
      if (res.data.data) return res.data.data;
      throw new Error(res.data.message || 'Không thể tạo dịch vụ khám.');
    } catch {
      const db = getMockDb();
      const spec = db.specialties.find((s) => s.id === data.specialtyId);
      const newSrv: AdminServiceItem = {
        id: 'srv-' + Date.now(),
        code: data.code,
        name: data.name,
        specialtyId: data.specialtyId,
        specialtyName: spec?.name || 'Đa Khoa',
        durationMinutes: data.durationMinutes,
        price: data.price,
        currency: data.currency || 'VND',
        description: data.description,
        isActive: true,
      };
      db.services.push(newSrv);
      saveMockDb(db);
      return newSrv;
    }
  },

  setServiceStatus: async (id: string, isActive: boolean): Promise<boolean> => {
    try {
      const res = await apiClient.patch<ApiResponse<boolean>>(`/admin/services/${id}/status`, { isActive });
      return !!res.data.data;
    } catch {
      const db = getMockDb();
      const found = db.services.find((s) => s.id === id);
      if (found) {
        found.isActive = isActive;
        saveMockDb(db);
      }
      return true;
    }
  },

  getDoctorSchedules: async (
    doctorId: string,
    effectiveDate?: string,
    isActive?: boolean
  ): Promise<AdminDoctorScheduleItem[]> => {
    try {
      const res = await apiClient.get<ApiResponse<AdminDoctorScheduleItem[]>>('/admin/doctor-schedules', {
        params: { doctorId, effectiveDate, isActive },
      });
      return res.data.data || [];
    } catch {
      const db = getMockDb();
      let list = db.schedules.filter((s) => s.doctorId === doctorId);
      if (typeof isActive === 'boolean') list = list.filter((s) => s.isActive === isActive);
      return list;
    }
  },

  createDoctorSchedule: async (data: {
    doctorId: string;
    dayOfWeek: number;
    startTime: string;
    endTime: string;
    effectiveFrom: string;
    effectiveTo?: string | null;
    slotDurationMinutes?: number;
  }): Promise<AdminDoctorScheduleItem> => {
    try {
      const res = await apiClient.post<ApiResponse<AdminDoctorScheduleItem>>('/admin/doctor-schedules', data);
      if (res.data.data) return res.data.data;
      throw new Error(res.data.message || 'Không thể thiết lập ca làm việc.');
    } catch {
      const db = getMockDb();
      const doc = db.doctors.find((d) => d.id === data.doctorId) || db.doctors[0];
      const dayNames = ['Chủ Nhật', 'Thứ Hai', 'Thứ Ba', 'Thứ Tư', 'Thứ Năm', 'Thứ Sáu', 'Thứ Bảy'];
      const newSched: AdminDoctorScheduleItem = {
        id: 'sch-' + Date.now(),
        doctorId: data.doctorId,
        doctorName: doc?.fullName || 'BS. Chuyên khoa',
        doctorCode: doc?.doctorCode || 'DOC-01',
        dayOfWeek: data.dayOfWeek,
        dayOfWeekName: dayNames[data.dayOfWeek] || `Thứ ${data.dayOfWeek + 1}`,
        startTime: data.startTime,
        endTime: data.endTime,
        effectiveFrom: data.effectiveFrom,
        effectiveTo: data.effectiveTo || undefined,
        slotDurationMinutes: data.slotDurationMinutes || 30,
        isActive: true,
      };
      db.schedules.push(newSched);
      saveMockDb(db);
      return newSched;
    }
  },

  setDoctorScheduleStatus: async (id: string, isActive: boolean): Promise<boolean> => {
    try {
      const res = await apiClient.patch<ApiResponse<boolean>>(`/admin/doctor-schedules/${id}/status`, { isActive });
      return !!res.data.data;
    } catch {
      const db = getMockDb();
      const found = db.schedules.find((s) => s.id === id);
      if (found) {
        found.isActive = isActive;
        saveMockDb(db);
      }
      return true;
    }
  },

  deleteDoctorSchedule: async (id: string): Promise<boolean> => {
    try {
      const res = await apiClient.delete<ApiResponse<boolean>>(`/admin/doctor-schedules/${id}`);
      return !!res.data.data;
    } catch {
      const db = getMockDb();
      db.schedules = db.schedules.filter((s) => s.id !== id);
      saveMockDb(db);
      return true;
    }
  },
};

// ----------------- PAYMENT API -----------------
export const paymentApi = {
  createPayOSLink: async (appointmentId: string): Promise<CreatePaymentLinkResponse> => {
    try {
      const res = await apiClient.post<ApiResponse<CreatePaymentLinkResponse>>('/payments/payos/create-link', {
        appointmentId,
      });
      if (res.data.data) {
        return res.data.data;
      }
      throw new Error(res.data.message || 'Không thể tạo liên kết thanh toán PayOS.');
    } catch (err: any) {
      if (err.response?.status === 400) {
        throw new Error(err.response?.data?.message || err.message);
      }
      return {
        success: true,
        checkoutUrl: 'https://pay.payos.vn/web/mock',
        qrCode: '/images/qr/techcombank-qr-clean.png',
        orderCode: Number(Date.now().toString().slice(-6)),
        paymentLinkId: 'mock-link-' + appointmentId,
        message: 'Khởi tạo mã thanh toán VietQR Techcombank thành công.',
        bankName: 'Techcombank (TCB)',
        accountNumber: '19038779759014',
        accountName: 'LE HUYNH PHUONG ANH',
      };
    }
  },

  getPaymentStatus: async (appointmentId: string): Promise<PaymentStatusResponse> => {
    try {
      const res = await apiClient.get<ApiResponse<PaymentStatusResponse>>(`/payments/status/${appointmentId}`);
      if (res.data.data) {
        return res.data.data;
      }
      throw new Error(res.data.message || 'Không thể lấy trạng thái thanh toán.');
    } catch {
      const db = getMockDb();
      const targetApt = db.appointments.find((a) => a.id === appointmentId);
      const isPaid = targetApt
        ? targetApt.status === 'Confirmed' || targetApt.status === 'CheckedIn' || targetApt.status === 'Completed'
        : true;
      return {
        appointmentId,
        bookingCode: targetApt?.bookingCode || 'APT-ONLINE',
        servicePrice: targetApt?.servicePrice || 150000,
        paidAmount: targetApt?.servicePrice || 150000,
        status: isPaid ? 'PAID' : 'PENDING',
        isPaid,
      };
    }
  },

  mockConfirmPayment: async (appointmentId: string): Promise<PaymentStatusResponse> => {
    try {
      const res = await apiClient.post<ApiResponse<PaymentStatusResponse>>(`/payments/mock-confirm/${appointmentId}`);
      if (res.data.data) {
        return res.data.data;
      }
      throw new Error(res.data.message || 'Không thể xác nhận thanh toán.');
    } catch {
      const db = getMockDb();
      const targetApt = db.appointments.find((a) => a.id === appointmentId);
      if (targetApt) {
        targetApt.status = 'Confirmed';
        saveMockDb(db);
      }
      return {
        appointmentId,
        bookingCode: targetApt?.bookingCode || 'APT-ONLINE',
        servicePrice: targetApt?.servicePrice || 150000,
        paidAmount: targetApt?.servicePrice || 150000,
        status: 'PAID',
        isPaid: true,
      };
    }
  },
};

function calculateEndTime(start: string, durationMinutes: number): string {
  const [h, m] = start.split(':').map(Number);
  const total = h * 60 + m + durationMinutes;
  const endH = Math.floor(total / 60) % 24;
  const endM = total % 60;
  return `${String(endH).padStart(2, '0')}:${String(endM).padStart(2, '0')}`;
}
