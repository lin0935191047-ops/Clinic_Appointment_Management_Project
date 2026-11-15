import React, { useState, useEffect, useCallback } from 'react';
import {
  Stethoscope,
  Clock,
  User,
  CheckCircle2,
  AlertCircle,
  FileText,
  RotateCw,
  LogOut,
  Play,
  Check,
  Activity,
  Calendar,
  Lock,
  Eye,
  X,
  Phone,
} from 'lucide-react';
import { doctorApi, authApi } from '../api/client';
import { getDoctorAvatar, getDoctorLocalFallback } from '../utils/doctorAvatars';
import {
  DoctorDashboardData,
  DoctorDashboardAppointmentItem,
  ConsultationData,
  RecordConsultationPayload,
} from '../types';

export const DoctorDashboardPage: React.FC = () => {
  // Auth state
  const [currentUser, setCurrentUser] = useState<any>(authApi.getStoredUser());
  const [token, setToken] = useState<string | null>(authApi.getStoredToken());
  const [loginEmail, setLoginEmail] = useState('');
  const [loginPassword, setLoginPassword] = useState('');
  const [loginLoading, setLoginLoading] = useState(false);
  const [loginError, setLoginError] = useState('');

  // Dashboard state
  const [selectedDate, setSelectedDate] = useState<string>(
    new Date().toISOString().split('T')[0]
  );
  const [dashboardData, setDashboardData] = useState<DoctorDashboardData | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  // Tab filter for today appointments
  const [statusFilter, setStatusFilter] = useState<string>('ALL');

  // Consultation form state (for active in-progress appointment)
  const [consultationForm, setConsultationForm] = useState<RecordConsultationPayload>({
    symptoms: '',
    diagnosis: '',
    examinationNotes: '',
    conclusion: '',
    followUpInstructions: '',
  });
  const [savingConsultation, setSavingConsultation] = useState(false);
  const [completingConsultation, setCompletingConsultation] = useState(false);
  const [startingAppointmentId, setStartingAppointmentId] = useState<string | null>(null);

  // Read-only Consultation modal (for completed appointments)
  const [viewConsultationModal, setViewConsultationModal] = useState<{
    isOpen: boolean;
    appointment: DoctorDashboardAppointmentItem | null;
    consultation: ConsultationData | null;
    loading: boolean;
    error: string | null;
  }>({
    isOpen: false,
    appointment: null,
    consultation: null,
    loading: false,
    error: null,
  });

  // Confirm complete modal
  const [showCompleteConfirmModal, setShowCompleteConfirmModal] = useState(false);

  // Role check
  const isAuthorizedDoctorOrAdmin =
    currentUser &&
    (currentUser.roles?.includes('DOCTOR') ||
      currentUser.roles?.includes('ADMIN'));

  // Load dashboard
  const fetchDashboard = useCallback(async () => {
    if (!token || !isAuthorizedDoctorOrAdmin) return;
    setLoading(true);
    setError(null);
    try {
      const data = await doctorApi.getDashboard(selectedDate);
      setDashboardData(data);

      // If there is an active appointment with existing consultation data, try to load it
      if (data.currentActiveAppointment && data.currentActiveAppointment.hasConsultation) {
        try {
          const consult = await doctorApi.getConsultation(
            data.currentActiveAppointment.appointmentId
          );
          setConsultationForm({
            symptoms: consult.symptoms || '',
            diagnosis: consult.diagnosis || '',
            examinationNotes: consult.examinationNotes || '',
            conclusion: consult.conclusion || '',
            followUpInstructions: consult.followUpInstructions || '',
          });
        } catch {
          // Keep form as is if consultation couldn't be loaded
        }
      } else if (!data.currentActiveAppointment) {
        // Reset form if no active appointment
        setConsultationForm({
          symptoms: '',
          diagnosis: '',
          examinationNotes: '',
          conclusion: '',
          followUpInstructions: '',
        });
      }
    } catch (err: any) {
      if (err.response?.status === 401 || err.response?.status === 403) {
        setError('Phiên làm việc hết hạn hoặc bạn không có quyền truy cập kênh Bác sĩ.');
        handleLogout();
      } else {
        setError(err.message || 'Không thể tải bảng điều khiển bác sĩ.');
      }
    } finally {
      setLoading(false);
    }
  }, [token, isAuthorizedDoctorOrAdmin, selectedDate]);

  useEffect(() => {
    if (token && isAuthorizedDoctorOrAdmin) {
      fetchDashboard();
    }
  }, [fetchDashboard, token, isAuthorizedDoctorOrAdmin]);

  // Handle Login
  const handleLogin = async (e?: React.FormEvent, demoEmail?: string, demoPass?: string) => {
    if (e) e.preventDefault();
    const emailToUse = demoEmail || loginEmail;
    const passToUse = demoPass || loginPassword;
    if (!emailToUse || !passToUse) {
      setLoginError('Vui lòng nhập email và mật khẩu');
      return;
    }
    setLoginLoading(true);
    setLoginError('');
    try {
      const res = await authApi.login(emailToUse, passToUse);
      const user = res.user;
      const isDoctorOrAdmin =
        user?.roles?.includes('DOCTOR') ||
        user?.roles?.includes('ADMIN');

      if (!isDoctorOrAdmin) {
        setLoginError('Tài khoản của bạn không có vai trò Bác sĩ (DOCTOR) hoặc Quản trị viên (ADMIN).');
        authApi.logout();
        return;
      }

      setToken(res.token);
      setCurrentUser(user);
      setSuccessMessage(`Đăng nhập thành công! Chào mừng Bác sĩ ${user.fullName || user.email}.`);
      setTimeout(() => setSuccessMessage(null), 4000);
    } catch (err: any) {
      setLoginError(err.message || 'Đăng nhập thất bại. Vui lòng kiểm tra lại thông tin.');
    } finally {
      setLoginLoading(false);
    }
  };

  const handleLogout = () => {
    authApi.logout();
    setToken(null);
    setCurrentUser(null);
    setDashboardData(null);
  };

  // Start Consultation
  const handleStartConsultation = async (appointmentId: string) => {
    setStartingAppointmentId(appointmentId);
    setError(null);
    setSuccessMessage(null);
    try {
      await doctorApi.startConsultation(appointmentId);
      setSuccessMessage('Đã bắt đầu ca khám! Bệnh nhân đã được chuyển vào phòng khám.');
      setTimeout(() => setSuccessMessage(null), 4000);
      await fetchDashboard();
    } catch (err: any) {
      setError(err.message || 'Không thể bắt đầu ca khám.');
    } finally {
      setStartingAppointmentId(null);
    }
  };

  // Save/Record Consultation
  const handleSaveConsultation = async (e?: React.FormEvent) => {
    if (e) e.preventDefault();
    if (!dashboardData?.currentActiveAppointment) return;

    if (!consultationForm.symptoms.trim() || !consultationForm.diagnosis.trim()) {
      setError('Vui lòng nhập đầy đủ Triệu chứng lâm sàng và Chẩn đoán.');
      return;
    }

    setSavingConsultation(true);
    setError(null);
    try {
      await doctorApi.recordConsultation(
        dashboardData.currentActiveAppointment.appointmentId,
        consultationForm
      );
      setSuccessMessage('Đã lưu thành công bản ghi chẩn đoán và thăm khám!');
      setTimeout(() => setSuccessMessage(null), 4000);
      await fetchDashboard();
    } catch (err: any) {
      setError(err.message || 'Không thể lưu bản ghi khám bệnh.');
    } finally {
      setSavingConsultation(false);
    }
  };

  // Complete Consultation
  const handleCompleteConsultation = async () => {
    if (!dashboardData?.currentActiveAppointment) return;

    if (!consultationForm.symptoms.trim() || !consultationForm.diagnosis.trim()) {
      setError('Bắt buộc phải ghi nhận Triệu chứng lâm sàng và Chẩn đoán trước khi hoàn tất ca khám.');
      setShowCompleteConfirmModal(false);
      return;
    }

    setCompletingConsultation(true);
    setError(null);
    try {
      // First ensure consultation record is saved
      await doctorApi.recordConsultation(
        dashboardData.currentActiveAppointment.appointmentId,
        consultationForm
      );

      // Complete consultation
      await doctorApi.completeConsultation(
        dashboardData.currentActiveAppointment.appointmentId,
        {
          conclusion: consultationForm.conclusion,
          followUpInstructions: consultationForm.followUpInstructions,
        }
      );

      setShowCompleteConfirmModal(false);
      setSuccessMessage('Ca khám đã hoàn tất thành công! Hồ sơ y khoa đã được lưu vết và khóa.');
      setTimeout(() => setSuccessMessage(null), 5000);

      // Reset form
      setConsultationForm({
        symptoms: '',
        diagnosis: '',
        examinationNotes: '',
        conclusion: '',
        followUpInstructions: '',
      });

      await fetchDashboard();
    } catch (err: any) {
      setError(err.message || 'Không thể hoàn tất ca khám.');
    } finally {
      setCompletingConsultation(false);
    }
  };

  // View Read-only Consultation
  const handleOpenConsultationModal = async (appointment: DoctorDashboardAppointmentItem) => {
    setViewConsultationModal({
      isOpen: true,
      appointment,
      consultation: null,
      loading: true,
      error: null,
    });
    try {
      const consult = await doctorApi.getConsultation(appointment.appointmentId);
      setViewConsultationModal((prev) => ({
        ...prev,
        consultation: consult,
        loading: false,
      }));
    } catch (err: any) {
      setViewConsultationModal((prev) => ({
        ...prev,
        loading: false,
        error: err.message || 'Không tìm thấy hồ sơ khám bệnh.',
      }));
    }
  };

  // Status Badge Helper
  const renderStatusBadge = (status: string) => {
    switch (status) {
      case 'CheckedIn':
        return (
          <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold bg-amber-100 text-amber-800 border border-amber-300">
            <Clock className="w-3 h-3 mr-1" /> Chờ khám
          </span>
        );
      case 'InProgress':
        return (
          <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold bg-blue-100 text-blue-800 border border-blue-300 animate-pulse">
            <Activity className="w-3 h-3 mr-1" /> Đang khám
          </span>
        );
      case 'Completed':
        return (
          <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold bg-emerald-100 text-emerald-800 border border-emerald-300">
            <CheckCircle2 className="w-3 h-3 mr-1" /> Đã hoàn tất
          </span>
        );
      case 'Confirmed':
        return (
          <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold bg-teal-100 text-teal-800 border border-teal-300">
            Đã xác nhận
          </span>
        );
      case 'Cancelled':
        return (
          <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold bg-red-100 text-red-800 border border-red-300">
            Đã hủy
          </span>
        );
      default:
        return (
          <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold bg-gray-100 text-gray-800">
            {status}
          </span>
        );
    }
  };

  // Filtered Appointments
  const filteredAppointments = dashboardData?.todayAppointments.filter((a) => {
    if (statusFilter === 'ALL') return true;
    return a.status === statusFilter;
  }) || [];

  // ==========================================
  // VIEW 1: LOGIN FORM (If not authenticated)
  // ==========================================
  if (!token || !isAuthorizedDoctorOrAdmin) {
    return (
      <div className="min-h-[80vh] flex items-center justify-center py-12 px-4 sm:px-6 lg:px-8 bg-gray-50">
        <div className="max-w-md w-full space-y-8 bg-white p-8 rounded-2xl shadow-xl border border-gray-100">
          <div className="text-center">
            <div className="mx-auto h-16 w-16 bg-blue-600 text-white rounded-2xl flex items-center justify-center shadow-lg shadow-blue-200">
              <Stethoscope className="h-8 w-8" />
            </div>
            <h2 className="mt-4 text-2xl font-black text-gray-900 tracking-tight">
              Kênh Bác Sĩ Lâm Sàng
            </h2>
            <p className="mt-2 text-sm text-gray-500">
              Đăng nhập tài khoản Bác sĩ để theo dõi lịch trực, tiếp nhận bệnh nhân và ghi hồ sơ bệnh án.
            </p>
          </div>

          {loginError && (
            <div className="p-3 bg-red-50 border border-red-200 rounded-lg text-sm text-red-700 flex items-start space-x-2">
              <AlertCircle className="w-5 h-5 flex-shrink-0 mt-0.5" />
              <span>{loginError}</span>
            </div>
          )}

          <form className="mt-6 space-y-4" onSubmit={(e) => handleLogin(e)}>
            <div>
              <label className="block text-xs font-semibold uppercase text-gray-700 mb-1">
                Email Bác sĩ / Quản trị
              </label>
              <input
                type="email"
                required
                value={loginEmail}
                onChange={(e) => setLoginEmail(e.target.value)}
                placeholder="dr.an@clinic.vn"
                className="w-full px-3.5 py-2.5 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition"
              />
            </div>
            <div>
              <label className="block text-xs font-semibold uppercase text-gray-700 mb-1">
                Mật khẩu
              </label>
              <input
                type="password"
                required
                value={loginPassword}
                onChange={(e) => setLoginPassword(e.target.value)}
                placeholder="••••••••"
                className="w-full px-3.5 py-2.5 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition"
              />
            </div>

            <button
              type="submit"
              disabled={loginLoading}
              className="w-full flex justify-center items-center py-2.5 px-4 border border-transparent rounded-lg shadow-md text-sm font-bold text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50 transition"
            >
              {loginLoading ? (
                <RotateCw className="w-5 h-5 animate-spin mr-2" />
              ) : (
                <Stethoscope className="w-5 h-5 mr-2" />
              )}
              Đăng Nhập Kênh Bác Sĩ
            </button>
          </form>

          {/* Quick Demo Login Buttons */}
          <div className="mt-6 pt-6 border-t border-gray-100">
            <p className="text-xs font-semibold text-gray-400 uppercase tracking-wider text-center mb-3">
              Tài Khoản Mẫu Để Đăng Nhập Nhanh
            </p>
            <div className="grid grid-cols-1 gap-2">
              <button
                type="button"
                onClick={() => handleLogin(undefined, 'dr.an@clinic.vn', 'Doctor@123')}
                disabled={loginLoading}
                className="w-full text-left px-3 py-2 text-xs font-medium bg-blue-50 hover:bg-blue-100 text-blue-800 rounded-lg border border-blue-200 transition flex items-center justify-between"
              >
                <span>
                  🩺 <strong>BS. Nguyễn Văn An</strong> (Tim Mạch)
                </span>
                <span className="text-blue-600 font-mono text-[10px]">dr.an@clinic.vn</span>
              </button>
              <button
                type="button"
                onClick={() => handleLogin(undefined, 'dr.binh@clinic.vn', 'Doctor@123')}
                disabled={loginLoading}
                className="w-full text-left px-3 py-2 text-xs font-medium bg-emerald-50 hover:bg-emerald-100 text-emerald-800 rounded-lg border border-emerald-200 transition flex items-center justify-between"
              >
                <span>
                  🩺 <strong>BS. Trần Thị Bình</strong> (Da Liễu)
                </span>
                <span className="text-emerald-600 font-mono text-[10px]">dr.binh@clinic.vn</span>
              </button>
              <button
                type="button"
                onClick={() => handleLogin(undefined, 'dr.chi@clinic.vn', 'Doctor@123')}
                disabled={loginLoading}
                className="w-full text-left px-3 py-2 text-xs font-medium bg-purple-50 hover:bg-purple-100 text-purple-800 rounded-lg border border-purple-200 transition flex items-center justify-between"
              >
                <span>
                  🩺 <strong>BS. Lê Hoàng Chi</strong> (Nhi Khoa)
                </span>
                <span className="text-purple-600 font-mono text-[10px]">dr.chi@clinic.vn</span>
              </button>
              <button
                type="button"
                onClick={() => handleLogin(undefined, 'admin@clinic.vn', 'Admin@Clinic2026!')}
                disabled={loginLoading}
                className="w-full text-left px-3 py-2 text-xs font-medium bg-gray-100 hover:bg-gray-200 text-gray-800 rounded-lg border border-gray-300 transition flex items-center justify-between"
              >
                <span>
                  🛡️ <strong>Quản Trị Viên</strong> (Full Access)
                </span>
                <span className="text-gray-600 font-mono text-[10px]">admin@clinic.vn</span>
              </button>
            </div>
          </div>
        </div>
      </div>
    );
  }

  // ==========================================
  // VIEW 2: DOCTOR DASHBOARD & CLINICAL WORKFLOW
  // ==========================================
  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 space-y-6">
      {/* Top Header Bar */}
      <div className="bg-white p-6 rounded-2xl shadow-sm border border-gray-200 flex flex-col md:flex-row md:items-center md:justify-between gap-4">
        <div className="flex items-center space-x-4">
          <div className="relative w-14 h-14 rounded-2xl overflow-hidden bg-blue-50 border-2 border-blue-200 flex items-center justify-center shadow-md flex-shrink-0">
            <img
              src={getDoctorAvatar({ fullName: dashboardData?.doctorName || currentUser?.fullName })}
              alt="Bác sĩ"
              className="w-full h-full object-cover"
              loading="lazy"
              onError={(e) => {
                const target = e.target as HTMLImageElement;
                const fallback = getDoctorLocalFallback({ fullName: dashboardData?.doctorName || currentUser?.fullName });
                if (!target.src.endsWith(fallback)) {
                  target.src = fallback;
                }
              }}
            />
          </div>
          <div>
            <div className="flex items-center space-x-2">
              <h1 className="text-2xl font-bold text-gray-900">
                {dashboardData?.doctorName || currentUser?.fullName || 'Bác Sĩ Khám Bệnh'}
              </h1>
              <span className="px-2.5 py-0.5 rounded-full text-xs font-bold bg-blue-100 text-blue-800 uppercase tracking-wide">
                {dashboardData?.specialtyName || 'Chuyên Khoa'}
              </span>
            </div>
            <p className="text-sm text-gray-500 mt-0.5 flex items-center space-x-3">
              <span>
                Phòng khám: <strong>{dashboardData?.roomNumber || 'P.101'}</strong>
              </span>
              <span>•</span>
              <span className="flex items-center">
                <Calendar className="w-3.5 h-3.5 mr-1 text-gray-400" />
                Ngày làm việc:{' '}
                <input
                  type="date"
                  value={selectedDate}
                  onChange={(e) => setSelectedDate(e.target.value)}
                  className="ml-1 px-2 py-0.5 border border-gray-300 rounded text-xs font-semibold focus:outline-none focus:ring-1 focus:ring-blue-500"
                />
              </span>
            </p>
          </div>
        </div>

        <div className="flex items-center space-x-3 self-end md:self-auto">
          <button
            onClick={fetchDashboard}
            disabled={loading}
            className="inline-flex items-center px-3.5 py-2 border border-gray-300 rounded-lg text-sm font-medium text-gray-700 bg-white hover:bg-gray-50 shadow-sm transition"
            title="Tải lại dữ liệu"
          >
            <RotateCw className={`w-4 h-4 mr-1.5 ${loading ? 'animate-spin text-blue-600' : ''}`} />
            Làm Mới
          </button>
          <button
            onClick={handleLogout}
            className="inline-flex items-center px-3.5 py-2 border border-transparent rounded-lg text-sm font-medium text-red-700 bg-red-50 hover:bg-red-100 transition"
          >
            <LogOut className="w-4 h-4 mr-1.5" />
            Đăng Xuất
          </button>
        </div>
      </div>

      {/* Global Alerts */}
      {error && (
        <div className="p-4 bg-red-50 border-l-4 border-red-500 rounded-r-lg text-sm text-red-700 flex items-start justify-between">
          <div className="flex items-start space-x-2">
            <AlertCircle className="w-5 h-5 flex-shrink-0 mt-0.5" />
            <span>{error}</span>
          </div>
          <button onClick={() => setError(null)} className="text-red-500 hover:text-red-700">
            <X className="w-4 h-4" />
          </button>
        </div>
      )}

      {successMessage && (
        <div className="p-4 bg-emerald-50 border-l-4 border-emerald-500 rounded-r-lg text-sm text-emerald-800 flex items-start justify-between">
          <div className="flex items-start space-x-2">
            <CheckCircle2 className="w-5 h-5 flex-shrink-0 mt-0.5 text-emerald-600" />
            <span>{successMessage}</span>
          </div>
          <button
            onClick={() => setSuccessMessage(null)}
            className="text-emerald-500 hover:text-emerald-700"
          >
            <X className="w-4 h-4" />
          </button>
        </div>
      )}

      {/* Metrics Row */}
      {dashboardData?.metrics && (
        <div className="grid grid-cols-2 sm:grid-cols-4 lg:grid-cols-6 gap-3">
          <div className="bg-white p-4 rounded-xl border border-gray-200 shadow-sm">
            <span className="text-xs font-semibold text-gray-500 uppercase">Tổng Ca Khám</span>
            <div className="mt-1 flex items-baseline justify-between">
              <span className="text-2xl font-black text-gray-900">
                {dashboardData.metrics.totalAppointments}
              </span>
              <Activity className="w-4 h-4 text-gray-400" />
            </div>
          </div>

          <div className="bg-white p-4 rounded-xl border border-amber-200 bg-amber-50/30 shadow-sm">
            <span className="text-xs font-semibold text-amber-700 uppercase">Đang Chờ Khám</span>
            <div className="mt-1 flex items-baseline justify-between">
              <span className="text-2xl font-black text-amber-600">
                {dashboardData.metrics.checkedInCount}
              </span>
              <Clock className="w-4 h-4 text-amber-500" />
            </div>
          </div>

          <div className="bg-white p-4 rounded-xl border border-blue-200 bg-blue-50/30 shadow-sm">
            <span className="text-xs font-semibold text-blue-700 uppercase">Đang Khám</span>
            <div className="mt-1 flex items-baseline justify-between">
              <span className="text-2xl font-black text-blue-600">
                {dashboardData.metrics.inProgressCount}
              </span>
              <Activity className="w-4 h-4 text-blue-500 animate-pulse" />
            </div>
          </div>

          <div className="bg-white p-4 rounded-xl border border-emerald-200 bg-emerald-50/30 shadow-sm">
            <span className="text-xs font-semibold text-emerald-700 uppercase">Đã Hoàn Tất</span>
            <div className="mt-1 flex items-baseline justify-between">
              <span className="text-2xl font-black text-emerald-600">
                {dashboardData.metrics.completedCount}
              </span>
              <CheckCircle2 className="w-4 h-4 text-emerald-500" />
            </div>
          </div>

          <div className="bg-white p-4 rounded-xl border border-gray-200 shadow-sm">
            <span className="text-xs font-semibold text-gray-500 uppercase">Đã Xác Nhận</span>
            <div className="mt-1 flex items-baseline justify-between">
              <span className="text-2xl font-black text-gray-700">
                {dashboardData.metrics.confirmedCount}
              </span>
              <span className="text-xs text-gray-400">chưa check-in</span>
            </div>
          </div>

          <div className="bg-white p-4 rounded-xl border border-gray-200 shadow-sm">
            <span className="text-xs font-semibold text-gray-500 uppercase">Tỷ Lệ Hoàn Tất</span>
            <div className="mt-1 flex items-baseline justify-between">
              <span className="text-2xl font-black text-teal-600">
                {dashboardData.metrics.completionRate}%
              </span>
              <span className="text-xs text-gray-400">hiệu suất</span>
            </div>
          </div>
        </div>
      )}

      {/* Main Grid: Left = Consultation & Active, Right = Waiting Queue */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
        {/* Left Column: Active Consultation (7 cols) */}
        <div className="lg:col-span-7 space-y-6">
          <div className="bg-white rounded-2xl shadow-sm border-2 border-blue-200 overflow-hidden">
            <div className="bg-blue-600 px-6 py-4 text-white flex items-center justify-between">
              <div className="flex items-center space-x-2">
                <Activity className="w-5 h-5 animate-pulse" />
                <h2 className="text-lg font-bold">Ca Khám Đang Diễn Ra (In-Progress)</h2>
              </div>
              {dashboardData?.currentActiveAppointment && (
                <span className="bg-blue-800 text-blue-100 px-3 py-1 rounded-full text-xs font-mono font-bold">
                  {dashboardData.currentActiveAppointment.bookingCode}
                </span>
              )}
            </div>

            <div className="p-6">
              {dashboardData?.currentActiveAppointment ? (
                <div className="space-y-6">
                  {/* Patient Info Card */}
                  <div className="bg-blue-50/50 p-4 rounded-xl border border-blue-100 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
                    <div className="space-y-1">
                      <div className="flex items-center space-x-2">
                        <span className="w-7 h-7 rounded-full bg-blue-600 text-white flex items-center justify-center font-bold text-xs">
                          #{dashboardData.currentActiveAppointment.queueNumber}
                        </span>
                        <h3 className="text-lg font-bold text-gray-900">
                          {dashboardData.currentActiveAppointment.patientName}
                        </h3>
                      </div>
                      <p className="text-xs text-gray-600 flex items-center space-x-2">
                        <span className="flex items-center">
                          <Phone className="w-3 h-3 mr-1 text-gray-400" />
                          {dashboardData.currentActiveAppointment.maskedPhoneNumber}
                        </span>
                        <span>•</span>
                        <span>Dịch vụ: <strong>{dashboardData.currentActiveAppointment.serviceName}</strong></span>
                      </p>
                      {dashboardData.currentActiveAppointment.reasonForVisit && (
                        <p className="text-xs text-gray-500 italic">
                          Lý do khám: "{dashboardData.currentActiveAppointment.reasonForVisit}"
                        </p>
                      )}
                    </div>

                    <div className="text-right sm:text-right">
                      <span className="text-xs text-gray-500 block">Khung giờ hẹn:</span>
                      <span className="text-sm font-bold text-blue-800">
                        {dashboardData.currentActiveAppointment.localStartTime} -{' '}
                        {dashboardData.currentActiveAppointment.localEndTime}
                      </span>
                    </div>
                  </div>

                  {/* Consultation Form */}
                  <form onSubmit={handleSaveConsultation} className="space-y-4">
                    <div>
                      <div className="flex items-center justify-between mb-1">
                        <label className="text-xs font-bold uppercase text-gray-700 flex items-center">
                          <FileText className="w-3.5 h-3.5 mr-1 text-blue-600" />
                          1. Triệu Chứng Lâm Sàng <span className="text-red-500 ml-1">*</span>
                        </label>
                        <span className="text-[11px] text-gray-400">Bắt buộc</span>
                      </div>
                      <textarea
                        rows={3}
                        required
                        value={consultationForm.symptoms}
                        onChange={(e) =>
                          setConsultationForm({ ...consultationForm, symptoms: e.target.value })
                        }
                        placeholder="Mô tả các triệu chứng lâm sàng của bệnh nhân (VD: Đau ngực trái, khó thở khi gắng sức 3 ngày nay...)"
                        className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                      />
                    </div>

                    <div>
                      <div className="flex items-center justify-between mb-1">
                        <label className="text-xs font-bold uppercase text-gray-700 flex items-center">
                          <Stethoscope className="w-3.5 h-3.5 mr-1 text-blue-600" />
                          2. Chẩn Đoán Y Khoa <span className="text-red-500 ml-1">*</span>
                        </label>
                        <span className="text-[11px] text-gray-400">Bắt buộc</span>
                      </div>
                      <textarea
                        rows={2}
                        required
                        value={consultationForm.diagnosis}
                        onChange={(e) =>
                          setConsultationForm({ ...consultationForm, diagnosis: e.target.value })
                        }
                        placeholder="Chẩn đoán sơ bộ hoặc xác định (VD: Cơn đau thắt ngực ổn định - Theo dõi thiếu máu cơ tim...)"
                        className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                      />
                    </div>

                    <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                      <div>
                        <label className="block text-xs font-semibold uppercase text-gray-700 mb-1">
                          3. Ghi Chú Thăm Khám (Tùy chọn)
                        </label>
                        <textarea
                          rows={2}
                          value={consultationForm.examinationNotes || ''}
                          onChange={(e) =>
                            setConsultationForm({
                              ...consultationForm,
                              examinationNotes: e.target.value,
                            })
                          }
                          placeholder="Huyết áp 120/80, tim đều, phổi trong..."
                          className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                        />
                      </div>

                      <div>
                        <label className="block text-xs font-semibold uppercase text-gray-700 mb-1">
                          4. Kết Luận & Đơn Thuốc (Tùy chọn)
                        </label>
                        <textarea
                          rows={2}
                          value={consultationForm.conclusion || ''}
                          onChange={(e) =>
                            setConsultationForm({
                              ...consultationForm,
                              conclusion: e.target.value,
                            })
                          }
                          placeholder="Kê đơn điều trị ngoại trú..."
                          className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                        />
                      </div>
                    </div>

                    <div>
                      <label className="block text-xs font-semibold uppercase text-gray-700 mb-1">
                        5. Hướng Dẫn Tái Khám / Lời Dặn Bác Sĩ (Tùy chọn)
                      </label>
                      <input
                        type="text"
                        value={consultationForm.followUpInstructions || ''}
                        onChange={(e) =>
                          setConsultationForm({
                            ...consultationForm,
                            followUpInstructions: e.target.value,
                          })
                        }
                        placeholder="Nghỉ ngơi, hạn chế vận động mạnh, tái khám sau 7 ngày..."
                        className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                      />
                    </div>

                    {/* Action Buttons */}
                    <div className="pt-4 border-t border-gray-200 flex flex-col sm:flex-row items-center justify-between gap-3">
                      <button
                        type="submit"
                        disabled={savingConsultation}
                        className="w-full sm:w-auto inline-flex items-center justify-center px-4 py-2.5 border border-gray-300 rounded-lg text-sm font-semibold text-gray-700 bg-white hover:bg-gray-50 shadow-sm transition disabled:opacity-50"
                      >
                        {savingConsultation ? (
                          <RotateCw className="w-4 h-4 animate-spin mr-1.5" />
                        ) : (
                          <FileText className="w-4 h-4 mr-1.5 text-blue-600" />
                        )}
                        Lưu Nháp Bệnh Án
                      </button>

                      <button
                        type="button"
                        onClick={() => setShowCompleteConfirmModal(true)}
                        disabled={completingConsultation}
                        className="w-full sm:w-auto inline-flex items-center justify-center px-5 py-2.5 border border-transparent rounded-lg shadow-md text-sm font-bold text-white bg-emerald-600 hover:bg-emerald-700 focus:ring-2 focus:ring-offset-2 focus:ring-emerald-500 transition disabled:opacity-50"
                      >
                        <Check className="w-4 h-4 mr-1.5" />
                        Hoàn Tất Ca Khám
                      </button>
                    </div>
                  </form>
                </div>
              ) : (
                <div className="py-12 text-center space-y-3">
                  <div className="w-16 h-16 rounded-full bg-blue-50 text-blue-500 mx-auto flex items-center justify-center">
                    <Stethoscope className="w-8 h-8" />
                  </div>
                  <h3 className="text-base font-bold text-gray-800">
                    Chưa có ca khám nào đang diễn ra
                  </h3>
                  <p className="text-xs text-gray-500 max-w-sm mx-auto">
                    Bác sĩ vui lòng chọn bệnh nhân trong <strong>Hàng Đợi Chờ Khám</strong> bên cạnh và nhấn nút <strong>"Bắt Đầu Khám"</strong> để tiếp nhận.
                  </p>
                </div>
              )}
            </div>
          </div>
        </div>

        {/* Right Column: Waiting Queue (5 cols) */}
        <div className="lg:col-span-5 space-y-6">
          <div className="bg-white rounded-2xl shadow-sm border border-gray-200 overflow-hidden">
            <div className="p-4 bg-amber-500 text-white flex items-center justify-between">
              <div className="flex items-center space-x-2">
                <Clock className="w-5 h-5" />
                <h2 className="text-base font-bold">Hàng Đợi Chờ Khám (Waiting Queue)</h2>
              </div>
              <span className="bg-amber-700 text-amber-100 text-xs px-2.5 py-0.5 rounded-full font-bold">
                {dashboardData?.waitingQueue.length || 0} bệnh nhân
              </span>
            </div>

            <div className="p-4 divide-y divide-gray-100 max-h-[560px] overflow-y-auto">
              {dashboardData?.waitingQueue && dashboardData.waitingQueue.length > 0 ? (
                dashboardData.waitingQueue.map((item) => (
                  <div
                    key={item.appointmentId}
                    className="py-3.5 first:pt-0 last:pb-0 flex items-center justify-between gap-3 hover:bg-gray-50/80 p-2 rounded-xl transition"
                  >
                    <div className="flex items-center space-x-3">
                      <div className="w-10 h-10 rounded-xl bg-amber-100 text-amber-800 flex items-center justify-center font-bold text-sm shadow-sm flex-shrink-0">
                        #{item.queueNumber}
                      </div>
                      <div>
                        <div className="flex items-center space-x-1.5">
                          <h4 className="text-sm font-bold text-gray-900">{item.patientName}</h4>
                          <span className="text-[10px] font-mono text-gray-400">
                            ({item.bookingCode})
                          </span>
                        </div>
                        <p className="text-xs text-gray-500 flex items-center space-x-2 mt-0.5">
                          <span>{item.serviceName}</span>
                          <span>•</span>
                          <span className="text-amber-700 font-semibold">
                            {item.localStartTime}
                          </span>
                        </p>
                      </div>
                    </div>

                    <button
                      onClick={() => handleStartConsultation(item.appointmentId)}
                      disabled={
                        startingAppointmentId === item.appointmentId ||
                        !!dashboardData?.currentActiveAppointment
                      }
                      title={
                        dashboardData?.currentActiveAppointment
                          ? 'Vui lòng hoàn tất ca khám hiện tại trước khi bắt đầu ca mới'
                          : 'Bắt đầu khám ca này'
                      }
                      className="inline-flex items-center px-3 py-1.5 rounded-lg text-xs font-bold text-white bg-blue-600 hover:bg-blue-700 shadow-sm transition disabled:opacity-40 disabled:cursor-not-allowed flex-shrink-0"
                    >
                      {startingAppointmentId === item.appointmentId ? (
                        <RotateCw className="w-3.5 h-3.5 animate-spin mr-1" />
                      ) : (
                        <Play className="w-3.5 h-3.5 mr-1" />
                      )}
                      Khám Ngay
                    </button>
                  </div>
                ))
              ) : (
                <div className="py-12 text-center text-gray-400">
                  <User className="w-10 h-10 mx-auto text-gray-300 mb-2" />
                  <p className="text-xs">Hiện không có bệnh nhân nào đang chờ khám tại phòng.</p>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* Bottom Section: All Today Appointments */}
      <div className="bg-white rounded-2xl shadow-sm border border-gray-200 overflow-hidden">
        <div className="p-5 border-b border-gray-200 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
          <div>
            <h2 className="text-lg font-bold text-gray-900">
              Toàn Bộ Lịch Khám Trong Ngày ({filteredAppointments.length})
            </h2>
            <p className="text-xs text-gray-500 mt-0.5">
              Danh sách chi tiết tất cả các ca khám của bác sĩ theo ngày làm việc
            </p>
          </div>

          {/* Filter Pills */}
          <div className="flex flex-wrap gap-1.5">
            {[
              { id: 'ALL', label: 'Tất Cả' },
              { id: 'CheckedIn', label: 'Chờ Khám' },
              { id: 'InProgress', label: 'Đang Khám' },
              { id: 'Completed', label: 'Đã Hoàn Tất' },
              { id: 'Confirmed', label: 'Đã Xác Nhận' },
              { id: 'Cancelled', label: 'Đã Hủy' },
            ].map((f) => (
              <button
                key={f.id}
                onClick={() => setStatusFilter(f.id)}
                className={`px-3 py-1 rounded-full text-xs font-semibold transition ${
                  statusFilter === f.id
                    ? 'bg-blue-600 text-white shadow-sm'
                    : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
                }`}
              >
                {f.label}
              </button>
            ))}
          </div>
        </div>

        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200 text-left text-sm">
            <thead className="bg-gray-50 text-gray-500 text-xs uppercase font-semibold">
              <tr>
                <th className="px-4 py-3">STT</th>
                <th className="px-4 py-3">Giờ Khám</th>
                <th className="px-4 py-3">Mã Đặt Lịch</th>
                <th className="px-4 py-3">Bệnh Nhân</th>
                <th className="px-4 py-3">Dịch Vụ</th>
                <th className="px-4 py-3">Trạng Thái</th>
                <th className="px-4 py-3 text-right">Thao Tác</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {filteredAppointments.length > 0 ? (
                filteredAppointments.map((a) => (
                  <tr key={a.appointmentId} className="hover:bg-gray-50/60 transition">
                    <td className="px-4 py-3 text-xs font-bold text-gray-500">
                      {a.queueNumber ? `#${a.queueNumber}` : '-'}
                    </td>
                    <td className="px-4 py-3 font-semibold text-gray-900 whitespace-nowrap">
                      {a.localStartTime} - {a.localEndTime}
                    </td>
                    <td className="px-4 py-3 font-mono text-xs text-blue-700 font-bold whitespace-nowrap">
                      {a.bookingCode}
                    </td>
                    <td className="px-4 py-3">
                      <div className="font-semibold text-gray-900">{a.patientName}</div>
                      <div className="text-xs text-gray-400">{a.maskedPhoneNumber}</div>
                    </td>
                    <td className="px-4 py-3 text-xs text-gray-600">{a.serviceName}</td>
                    <td className="px-4 py-3 whitespace-nowrap">{renderStatusBadge(a.status)}</td>
                    <td className="px-4 py-3 text-right whitespace-nowrap">
                      {a.status === 'Completed' && (
                        <button
                          onClick={() => handleOpenConsultationModal(a)}
                          className="inline-flex items-center px-2.5 py-1 rounded text-xs font-semibold text-blue-700 bg-blue-50 hover:bg-blue-100 transition"
                        >
                          <Eye className="w-3.5 h-3.5 mr-1" /> Xem Bệnh Án
                        </button>
                      )}
                      {a.status === 'CheckedIn' && (
                        <button
                          onClick={() => handleStartConsultation(a.appointmentId)}
                          disabled={
                            startingAppointmentId === a.appointmentId ||
                            !!dashboardData?.currentActiveAppointment
                          }
                          className="inline-flex items-center px-2.5 py-1 rounded text-xs font-semibold text-white bg-blue-600 hover:bg-blue-700 transition disabled:opacity-40"
                        >
                          <Play className="w-3.5 h-3.5 mr-1" /> Khám
                        </button>
                      )}
                    </td>
                  </tr>
                ))
              ) : (
                <tr>
                  <td colSpan={7} className="px-4 py-8 text-center text-xs text-gray-400">
                    Không có ca khám nào phù hợp với bộ lọc.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Modal 1: Read-only Consultation Record */}
      {viewConsultationModal.isOpen && (
        <div className="fixed inset-0 z-50 overflow-y-auto bg-black/50 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="bg-white rounded-2xl max-w-2xl w-full p-6 shadow-2xl space-y-4 border border-gray-100">
            <div className="flex items-center justify-between pb-3 border-b border-gray-100">
              <div className="flex items-center space-x-2">
                <FileText className="w-5 h-5 text-blue-600" />
                <h3 className="text-lg font-bold text-gray-900">
                  Hồ Sơ Thăm Khám Y Khoa (Đã Khóa)
                </h3>
              </div>
              <button
                onClick={() =>
                  setViewConsultationModal({
                    isOpen: false,
                    appointment: null,
                    consultation: null,
                    loading: false,
                    error: null,
                  })
                }
                className="text-gray-400 hover:text-gray-600"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            {viewConsultationModal.loading ? (
              <div className="py-12 text-center text-gray-500">
                <RotateCw className="w-8 h-8 animate-spin mx-auto text-blue-600 mb-2" />
                <p className="text-xs">Đang tải hồ sơ bệnh án...</p>
              </div>
            ) : viewConsultationModal.error ? (
              <div className="p-4 bg-red-50 text-red-700 text-xs rounded-lg">
                {viewConsultationModal.error}
              </div>
            ) : viewConsultationModal.consultation ? (
              <div className="space-y-4 text-sm">
                <div className="bg-gray-50 p-3 rounded-xl flex justify-between items-center text-xs">
                  <div>
                    Bệnh nhân: <strong>{viewConsultationModal.appointment?.patientName}</strong>
                  </div>
                  <div className="font-mono text-gray-500">
                    Mã ca: {viewConsultationModal.appointment?.bookingCode}
                  </div>
                </div>

                <div className="space-y-3">
                  <div>
                    <h4 className="text-xs font-bold uppercase text-gray-500 mb-1">
                      Triệu chứng lâm sàng
                    </h4>
                    <p className="p-3 bg-gray-50 rounded-lg text-gray-800 border border-gray-200">
                      {viewConsultationModal.consultation.symptoms}
                    </p>
                  </div>

                  <div>
                    <h4 className="text-xs font-bold uppercase text-gray-500 mb-1">
                      Chẩn đoán y khoa
                    </h4>
                    <p className="p-3 bg-blue-50/50 rounded-lg text-blue-900 font-semibold border border-blue-200">
                      {viewConsultationModal.consultation.diagnosis}
                    </p>
                  </div>

                  {viewConsultationModal.consultation.examinationNotes && (
                    <div>
                      <h4 className="text-xs font-bold uppercase text-gray-500 mb-1">
                        Ghi chú thăm khám
                      </h4>
                      <p className="p-3 bg-gray-50 rounded-lg text-gray-700 border border-gray-200">
                        {viewConsultationModal.consultation.examinationNotes}
                      </p>
                    </div>
                  )}

                  {viewConsultationModal.consultation.conclusion && (
                    <div>
                      <h4 className="text-xs font-bold uppercase text-gray-500 mb-1">
                        Kết luận & Kê đơn
                      </h4>
                      <p className="p-3 bg-emerald-50/50 rounded-lg text-emerald-900 border border-emerald-200">
                        {viewConsultationModal.consultation.conclusion}
                      </p>
                    </div>
                  )}

                  {viewConsultationModal.consultation.followUpInstructions && (
                    <div>
                      <h4 className="text-xs font-bold uppercase text-gray-500 mb-1">
                        Dặn dò & Hẹn tái khám
                      </h4>
                      <p className="p-3 bg-amber-50/50 rounded-lg text-amber-900 border border-amber-200">
                        {viewConsultationModal.consultation.followUpInstructions}
                      </p>
                    </div>
                  )}
                </div>

                <div className="pt-3 border-t border-gray-100 flex items-center justify-between text-xs text-gray-400">
                  <span className="flex items-center">
                    <Lock className="w-3.5 h-3.5 mr-1 text-emerald-600" /> Hồ sơ bất biến đã khóa
                  </span>
                  <button
                    onClick={() =>
                      setViewConsultationModal({
                        isOpen: false,
                        appointment: null,
                        consultation: null,
                        loading: false,
                        error: null,
                      })
                    }
                    className="px-4 py-1.5 rounded-lg bg-gray-100 hover:bg-gray-200 text-gray-700 font-semibold transition"
                  >
                    Đóng
                  </button>
                </div>
              </div>
            ) : null}
          </div>
        </div>
      )}

      {/* Modal 2: Confirm Complete Consultation */}
      {showCompleteConfirmModal && (
        <div className="fixed inset-0 z-50 overflow-y-auto bg-black/50 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="bg-white rounded-2xl max-w-md w-full p-6 shadow-2xl space-y-4 border border-gray-100 text-center">
            <div className="w-12 h-12 rounded-full bg-emerald-100 text-emerald-600 mx-auto flex items-center justify-center">
              <Check className="w-6 h-6" />
            </div>

            <h3 className="text-lg font-bold text-gray-900">
              Xác Nhận Hoàn Tất Ca Khám?
            </h3>

            <p className="text-xs text-gray-500 leading-relaxed">
              Ca khám sẽ được chuyển sang trạng thái <strong>Completed (Đã hoàn tất)</strong>. Toàn bộ thông tin triệu chứng và chẩn đoán sẽ được lưu vết kiểm toán và <strong>khóa vĩnh viễn</strong> chống chỉnh sửa theo quy định y tế.
            </p>

            <div className="pt-3 border-t border-gray-100 flex items-center justify-end space-x-3">
              <button
                type="button"
                onClick={() => setShowCompleteConfirmModal(false)}
                className="px-4 py-2 rounded-lg text-xs font-semibold text-gray-700 bg-gray-100 hover:bg-gray-200 transition"
              >
                Xem Lại
              </button>
              <button
                type="button"
                onClick={handleCompleteConsultation}
                disabled={completingConsultation}
                className="px-5 py-2 rounded-lg text-xs font-bold text-white bg-emerald-600 hover:bg-emerald-700 shadow-md transition disabled:opacity-50 flex items-center"
              >
                {completingConsultation && <RotateCw className="w-3.5 h-3.5 animate-spin mr-1.5" />}
                Xác Nhận & Khóa Bệnh Án
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default DoctorDashboardPage;
