export interface ApiResponse<T> {
  isSuccess?: boolean;
  success?: boolean;
  statusCode: number;
  message: string;
  data?: T;
  errors?: string[];
}

export type AppointmentStatus =
  | 'PendingConfirmation'
  | 'Confirmed'
  | 'CheckedIn'
  | 'InProgress'
  | 'Completed'
  | 'Cancelled'
  | 'NoShow';

export type RescheduleStatus =
  | 'PendingReview'
  | 'Approved'
  | 'Rejected'
  | 'Cancelled'
  | 'Expired';

export interface Specialty {
  id: string;
  name: string;
  description: string;
  code: string;
}

export interface Doctor {
  id: string;
  fullName: string;
  title: string;
  specialtyId: string;
  specialtyName: string;
  bio?: string;
  consultationFee: number;
  roomNumber?: string;
}

export interface ClinicService {
  id: string;
  name: string;
  description: string;
  durationMinutes: number;
  basePrice: number;
  specialtyId: string;
}

export interface AvailableSlot {
  startTime: string; // '08:00'
  endTime: string;   // '08:30'
  isAvailable: boolean;
}

export interface GuestBookingRequest {
  patientFullName: string;
  dateOfBirth?: string;
  birthYear?: number;
  gender?: string;
  contactPhone: string;
  email?: string;
  citizenId?: string;
  healthInsuranceNumber?: string;
  specialtyId: string;
  doctorId: string;
  serviceId: string;
  appointmentDate: string; // 'YYYY-MM-DD'
  startTime: string;       // 'HH:mm'
  notes?: string;
}

export interface BookingConfirmation {
  appointmentId: string;
  bookingCode: string;
  patientFullName: string;
  doctorName: string;
  specialtyName: string;
  serviceName: string;
  appointmentDate: string;
  startTime: string;
  endTime: string;
  status: AppointmentStatus;
  fee: number;
}

export interface AppointmentDetail {
  id: string;
  bookingCode: string;
  patientFullName: string;
  contactPhoneMasked: string;
  doctorName: string;
  specialtyName: string;
  serviceName: string;
  appointmentDate: string;
  startTime: string;
  endTime: string;
  status: AppointmentStatus;
  paidAmount: number;
  notes?: string;
  createdAt: string;
}

export interface RefundPreview {
  paidAmount: number;
  refundPercentage: number;
  refundAmount: number;
  cancellationFee: number;
  minutesUntilAppointment: number;
  policyNote: string;
}

export interface DoctorDashboardAppointmentItem {
  appointmentId: string;
  bookingCode: string;
  queueNumber: number;
  patientId: string;
  patientName: string;
  maskedPhoneNumber: string;
  serviceName: string;
  durationMinutes: number;
  startAtUtc: string;
  endAtUtc: string;
  localStartTime: string;
  localEndTime: string;
  status: AppointmentStatus;
  reasonForVisit?: string;
  hasConsultation: boolean;
  checkedInAtUtc?: string;
}

export interface DoctorDashboardMetrics {
  totalAppointments: number;
  pendingConfirmationCount: number;
  confirmedCount: number;
  checkedInCount: number;
  inProgressCount: number;
  completedCount: number;
  cancelledCount: number;
  noShowCount: number;
  completionRate: number;
}

export interface DoctorDashboardData {
  doctorId: string;
  doctorName: string;
  specialtyName: string;
  roomNumber?: string;
  date: string;
  metrics: DoctorDashboardMetrics;
  todayAppointments: DoctorDashboardAppointmentItem[];
  waitingQueue: DoctorDashboardAppointmentItem[];
  currentActiveAppointment?: DoctorDashboardAppointmentItem | null;
  generatedAtUtc: string;
}

export interface ConsultationData {
  id: string;
  appointmentId: string;
  doctorId: string;
  symptoms: string;
  diagnosis: string;
  examinationNotes?: string;
  conclusion?: string;
  followUpInstructions?: string;
  createdAtUtc: string;
  completedAtUtc?: string;
}

export interface RecordConsultationPayload {
  symptoms: string;
  diagnosis: string;
  examinationNotes?: string;
  conclusion?: string;
  followUpInstructions?: string;
}

export interface CompleteConsultationPayload {
  conclusion?: string;
  followUpInstructions?: string;
}
