import React, { useState, useEffect, useCallback } from 'react';
import {
  ShieldCheck,
  TrendingUp,
  DollarSign,
  Calendar,
  Layers,
  RotateCw,
  LogOut,
  Plus,
  CheckCircle2,
  AlertCircle,
  X,
  Stethoscope,
  Briefcase,
  ToggleLeft,
  ToggleRight,
  Trash2,
  UserCheck,
  Search,
  ArrowRight,
  Clock,
} from 'lucide-react';
import { Link } from 'react-router-dom';
import { adminApi, authApi, receptionApi, ReceptionAppointmentItem } from '../api/client';
import { getDoctorAvatar, getDoctorLocalFallback } from '../utils/doctorAvatars';
import {
  AdminDashboardReport,
  AdminSpecialtyItem,
  AdminDoctorItem,
  AdminServiceItem,
  AdminDoctorScheduleItem,
} from '../types';

export const AdminDashboardPage: React.FC = () => {
  // Auth state
  const [currentUser, setCurrentUser] = useState<any>(authApi.getStoredUser());
  const [token, setToken] = useState<string | null>(authApi.getStoredToken());
  const [loginEmail, setLoginEmail] = useState('');
  const [loginPassword, setLoginPassword] = useState('');
  const [loginLoading, setLoginLoading] = useState(false);
  const [loginError, setLoginError] = useState('');

  // Main navigation tabs
  const [activeTab, setActiveTab] = useState<
    'appointments' | 'reports' | 'specialties' | 'doctors' | 'services' | 'schedules'
  >('appointments');

  // Loading & notification states
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  // TAB 0: APPOINTMENTS STATE
  const [appointmentsList, setAppointmentsList] = useState<ReceptionAppointmentItem[]>([]);
  const [aptFilterDate, setAptFilterDate] = useState<string>('');
  const [aptFilterStatus, setAptFilterStatus] = useState<string>('');
  const [aptSearch, setAptSearch] = useState<string>('');

  // TAB 1: REPORTS & DASHBOARD STATE
  const todayStr = new Date().toISOString().split('T')[0];
  const thirtyDaysAgoStr = new Date(Date.now() - 30 * 24 * 60 * 60 * 1000)
    .toISOString()
    .split('T')[0];

  const [reportFromDate, setReportFromDate] = useState<string>(thirtyDaysAgoStr);
  const [reportToDate, setReportToDate] = useState<string>(todayStr);
  const [reportDoctorId, setReportDoctorId] = useState<string>('');
  const [reportSpecialtyId, setReportSpecialtyId] = useState<string>('');
  const [reportStatus, setReportStatus] = useState<string>('');
  const [dashboardReport, setDashboardReport] = useState<AdminDashboardReport | null>(null);

  // TAB 2: SPECIALTIES STATE
  const [specialtiesList, setSpecialtiesList] = useState<AdminSpecialtyItem[]>([]);
  const [showSpecialtyModal, setShowSpecialtyModal] = useState(false);
  const [newSpecialty, setNewSpecialty] = useState({
    code: '',
    name: '',
    description: '',
  });

  // TAB 3: DOCTORS STATE
  const [doctorsList, setDoctorsList] = useState<AdminDoctorItem[]>([]);
  const [showDoctorModal, setShowDoctorModal] = useState(false);
  const [newDoctor, setNewDoctor] = useState({
    email: '',
    password: '',
    doctorCode: '',
    fullName: '',
    title: 'BS. CKI',
    specialtyId: '',
    roomNumber: 'P.101',
    bio: '',
  });

  // TAB 4: SERVICES STATE
  const [servicesList, setServicesList] = useState<AdminServiceItem[]>([]);
  const [showServiceModal, setShowServiceModal] = useState(false);
  const [newService, setNewService] = useState({
    specialtyId: '',
    code: '',
    name: '',
    durationMinutes: 30,
    price: 200000,
    description: '',
  });

  // TAB 5: DOCTOR SCHEDULES STATE
  const [selectedDoctorIdForSchedule, setSelectedDoctorIdForSchedule] = useState<string>('');
  const [schedulesList, setSchedulesList] = useState<AdminDoctorScheduleItem[]>([]);
  const [showScheduleModal, setShowScheduleModal] = useState(false);
  const [newSchedule, setNewSchedule] = useState({
    dayOfWeek: 1, // 1 = Monday
    startTime: '08:00',
    endTime: '12:00',
    effectiveFrom: todayStr,
    effectiveTo: '',
    slotDurationMinutes: 30,
  });

  // Role check
  const isAuthorizedAdmin =
    currentUser &&
    currentUser.roles?.includes('ADMIN');

  // DATA FETCHING FUNCTIONS
  const fetchAppointments = useCallback(async () => {
    if (!token || !isAuthorizedAdmin) return;
    setLoading(true);
    try {
      const list = await receptionApi.getQueue({
        date: aptFilterDate || undefined,
        status: aptFilterStatus || undefined,
        search: aptSearch || undefined,
      });
      setAppointmentsList(list);
    } catch (err: any) {
      setError(err.message || 'Không thể tải danh sách ca khám.');
    } finally {
      setLoading(false);
    }
  }, [token, isAuthorizedAdmin, aptFilterDate, aptFilterStatus, aptSearch]);

  const fetchReports = useCallback(async () => {
    if (!token || !isAuthorizedAdmin) return;
    setLoading(true);
    setError(null);
    try {
      const data = await adminApi.getDashboardReport({
        fromDate: reportFromDate || undefined,
        toDate: reportToDate || undefined,
        doctorId: reportDoctorId || undefined,
        specialtyId: reportSpecialtyId || undefined,
        status: reportStatus || undefined,
      });
      setDashboardReport(data);
    } catch (err: any) {
      setError(err.message || 'Không thể tải báo cáo quản trị.');
    } finally {
      setLoading(false);
    }
  }, [
    token,
    isAuthorizedAdmin,
    reportFromDate,
    reportToDate,
    reportDoctorId,
    reportSpecialtyId,
    reportStatus,
  ]);

  const fetchSpecialties = useCallback(async () => {
    if (!token || !isAuthorizedAdmin) return;
    setLoading(true);
    try {
      const list = await adminApi.getSpecialties();
      setSpecialtiesList(list);
    } catch (err: any) {
      setError(err.message || 'Không thể tải danh sách chuyên khoa.');
    } finally {
      setLoading(false);
    }
  }, [token, isAuthorizedAdmin]);

  const fetchDoctors = useCallback(async () => {
    if (!token || !isAuthorizedAdmin) return;
    setLoading(true);
    try {
      const list = await adminApi.getDoctors();
      setDoctorsList(list);
      if (list.length > 0 && !selectedDoctorIdForSchedule) {
        setSelectedDoctorIdForSchedule(list[0].id);
      }
    } catch (err: any) {
      setError(err.message || 'Không thể tải danh sách bác sĩ.');
    } finally {
      setLoading(false);
    }
  }, [token, isAuthorizedAdmin, selectedDoctorIdForSchedule]);

  const fetchServices = useCallback(async () => {
    if (!token || !isAuthorizedAdmin) return;
    setLoading(true);
    try {
      const list = await adminApi.getServices();
      setServicesList(list);
    } catch (err: any) {
      setError(err.message || 'Không thể tải danh sách dịch vụ.');
    } finally {
      setLoading(false);
    }
  }, [token, isAuthorizedAdmin]);

  const fetchSchedules = useCallback(async () => {
    if (!token || !isAuthorizedAdmin || !selectedDoctorIdForSchedule) return;
    setLoading(true);
    try {
      const list = await adminApi.getDoctorSchedules(selectedDoctorIdForSchedule);
      setSchedulesList(list);
    } catch (err: any) {
      setError(err.message || 'Không thể tải danh sách lịch làm việc của bác sĩ.');
    } finally {
      setLoading(false);
    }
  }, [token, isAuthorizedAdmin, selectedDoctorIdForSchedule]);

  // Initial load
  useEffect(() => {
    if (token && isAuthorizedAdmin) {
      fetchAppointments();
      fetchReports();
      fetchSpecialties();
      fetchDoctors();
      fetchServices();
    }
  }, [token, isAuthorizedAdmin, fetchAppointments, fetchReports, fetchSpecialties, fetchDoctors, fetchServices]);

  // Tab change trigger
  useEffect(() => {
    if (token && isAuthorizedAdmin) {
      if (activeTab === 'appointments') fetchAppointments();
      if (activeTab === 'reports') fetchReports();
      if (activeTab === 'specialties') fetchSpecialties();
      if (activeTab === 'doctors') fetchDoctors();
      if (activeTab === 'services') fetchServices();
      if (activeTab === 'schedules' && selectedDoctorIdForSchedule) fetchSchedules();
    }
  }, [
    activeTab,
    token,
    isAuthorizedAdmin,
    fetchAppointments,
    fetchReports,
    fetchSpecialties,
    fetchDoctors,
    fetchServices,
    fetchSchedules,
    selectedDoctorIdForSchedule,
  ]);

  // ==========================================
  // AUTH HANDLERS
  // ==========================================
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
      const isAdmin = user?.roles?.includes('ADMIN');

      if (!isAdmin) {
        setLoginError('Tài khoản của bạn không có vai trò Quản Trị Viên (ADMIN).');
        authApi.logout();
        return;
      }

      setToken(res.token);
      setCurrentUser(user);
      setSuccessMessage('Đăng nhập Quản Trị Viên thành công!');
      setTimeout(() => setSuccessMessage(null), 4000);
    } catch (err: any) {
      setLoginError(err.message || 'Đăng nhập thất bại.');
    } finally {
      setLoginLoading(false);
    }
  };

  const handleLogout = () => {
    authApi.logout();
    setToken(null);
    setCurrentUser(null);
    setDashboardReport(null);
  };

  // ==========================================
  // CRUD ACTIONS
  // ==========================================
  // 1. Specialty
  const handleCreateSpecialty = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newSpecialty.name.trim() || !newSpecialty.code.trim()) {
      setError('Vui lòng nhập tên và mã chuyên khoa.');
      return;
    }
    try {
      await adminApi.createSpecialty(newSpecialty);
      setShowSpecialtyModal(false);
      setNewSpecialty({ code: '', name: '', description: '' });
      setSuccessMessage('Thêm chuyên khoa mới thành công!');
      setTimeout(() => setSuccessMessage(null), 4000);
      fetchSpecialties();
    } catch (err: any) {
      setError(err.message || 'Không thể tạo chuyên khoa.');
    }
  };

  const handleToggleSpecialtyStatus = async (id: string, currentStatus: boolean) => {
    try {
      await adminApi.setSpecialtyStatus(id, !currentStatus);
      setSuccessMessage(`Đã ${!currentStatus ? 'kích hoạt' : 'vô hiệu hóa'} chuyên khoa.`);
      setTimeout(() => setSuccessMessage(null), 3000);
      fetchSpecialties();
    } catch (err: any) {
      setError(err.message || 'Không thể cập nhật trạng thái chuyên khoa.');
    }
  };

  // 2. Doctor
  const handleCreateDoctor = async (e: React.FormEvent) => {
    e.preventDefault();
    if (
      !newDoctor.fullName.trim() ||
      !newDoctor.email.trim() ||
      !newDoctor.doctorCode.trim() ||
      !newDoctor.specialtyId
    ) {
      setError('Vui lòng điền đầy đủ Họ tên, Email, Mã bác sĩ và Chuyên khoa.');
      return;
    }
    try {
      await adminApi.createDoctor({
        fullName: newDoctor.fullName,
        email: newDoctor.email,
        password: newDoctor.password || 'Doctor@2026',
        doctorCode: newDoctor.doctorCode,
        specialtyId: newDoctor.specialtyId,
        title: newDoctor.title,
        roomNumber: newDoctor.roomNumber,
        bio: newDoctor.bio,
      });
      setShowDoctorModal(false);
      setNewDoctor({
        email: '',
        password: '',
        doctorCode: '',
        fullName: '',
        title: 'BS. CKI',
        specialtyId: '',
        roomNumber: 'P.101',
        bio: '',
      });
      setSuccessMessage('Tạo hồ sơ và tài khoản Bác sĩ thành công!');
      setTimeout(() => setSuccessMessage(null), 4000);
      fetchDoctors();
    } catch (err: any) {
      setError(err.message || 'Không thể tạo bác sĩ.');
    }
  };

  const handleToggleDoctorStatus = async (id: string, currentStatus: boolean) => {
    try {
      await adminApi.setDoctorStatus(id, !currentStatus);
      setSuccessMessage(`Đã ${!currentStatus ? 'kích hoạt' : 'vô hiệu hóa'} bác sĩ.`);
      setTimeout(() => setSuccessMessage(null), 3000);
      fetchDoctors();
    } catch (err: any) {
      setError(err.message || 'Không thể cập nhật trạng thái bác sĩ.');
    }
  };

  // 3. Service
  const handleCreateService = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newService.name.trim() || !newService.code.trim() || !newService.specialtyId) {
      setError('Vui lòng nhập tên dịch vụ, mã code và chuyên khoa.');
      return;
    }
    try {
      await adminApi.createService({
        name: newService.name,
        code: newService.code,
        specialtyId: newService.specialtyId,
        durationMinutes: Number(newService.durationMinutes) || 30,
        price: Number(newService.price) || 200000,
        description: newService.description,
      });
      setShowServiceModal(false);
      setNewService({
        specialtyId: '',
        code: '',
        name: '',
        durationMinutes: 30,
        price: 200000,
        description: '',
      });
      setSuccessMessage('Tạo dịch vụ khám mới thành công!');
      setTimeout(() => setSuccessMessage(null), 4000);
      fetchServices();
    } catch (err: any) {
      setError(err.message || 'Không thể tạo dịch vụ.');
    }
  };

  const handleToggleServiceStatus = async (id: string, currentStatus: boolean) => {
    try {
      await adminApi.setServiceStatus(id, !currentStatus);
      setSuccessMessage(`Đã ${!currentStatus ? 'kích hoạt' : 'vô hiệu hóa'} dịch vụ khám.`);
      setTimeout(() => setSuccessMessage(null), 3000);
      fetchServices();
    } catch (err: any) {
      setError(err.message || 'Không thể cập nhật trạng thái dịch vụ.');
    }
  };

  // 4. Schedule
  const handleCreateSchedule = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedDoctorIdForSchedule) {
      setError('Vui lòng chọn bác sĩ trước khi tạo lịch.');
      return;
    }
    try {
      await adminApi.createDoctorSchedule({
        doctorId: selectedDoctorIdForSchedule,
        dayOfWeek: Number(newSchedule.dayOfWeek),
        startTime: newSchedule.startTime,
        endTime: newSchedule.endTime,
        effectiveFrom: newSchedule.effectiveFrom,
        effectiveTo: newSchedule.effectiveTo || null,
        slotDurationMinutes: Number(newSchedule.slotDurationMinutes) || 30,
      });
      setShowScheduleModal(false);
      setSuccessMessage('Thiết lập ca làm việc định kỳ thành công!');
      setTimeout(() => setSuccessMessage(null), 4000);
      fetchSchedules();
    } catch (err: any) {
      setError(err.message || 'Không thể tạo ca làm việc.');
    }
  };

  const handleToggleScheduleStatus = async (id: string, currentStatus: boolean) => {
    try {
      await adminApi.setDoctorScheduleStatus(id, !currentStatus);
      setSuccessMessage(`Đã ${!currentStatus ? 'kích hoạt' : 'vô hiệu hóa'} ca làm việc.`);
      setTimeout(() => setSuccessMessage(null), 3000);
      fetchSchedules();
    } catch (err: any) {
      setError(err.message || 'Không thể cập nhật ca làm việc.');
    }
  };

  const handleDeleteSchedule = async (id: string) => {
    if (!window.confirm('Bạn có chắc chắn muốn xóa ca làm việc định kỳ này?')) return;
    try {
      await adminApi.deleteDoctorSchedule(id);
      setSuccessMessage('Đã xóa ca làm việc thành công.');
      setTimeout(() => setSuccessMessage(null), 3000);
      fetchSchedules();
    } catch (err: any) {
      setError(err.message || 'Không thể xóa ca làm việc.');
    }
  };

  const formatDateTimeVn = (utcString?: string) => {
    if (!utcString) return '--:--';
    const date = new Date(utcString);
    const vn = new Date(date.getTime() + 7 * 3600000);
    return `${vn.toISOString().substring(11, 16)} (${vn.getDate().toString().padStart(2, '0')}/${(vn.getMonth() + 1).toString().padStart(2, '0')}/${vn.getFullYear()})`;
  };

  const handleConfirmAppointment = async (appointmentId: string) => {
    try {
      setLoading(true);
      await receptionApi.confirmAppointment(appointmentId);
      setSuccessMessage('Đã xác nhận lịch hẹn thành công!');
      fetchAppointments();
    } catch (err: any) {
      setError(err.message || 'Không thể xác nhận lịch hẹn.');
    } finally {
      setLoading(false);
    }
  };

  const handleCheckIn = async (appointmentId: string) => {
    try {
      setLoading(true);
      await receptionApi.checkInAppointment(appointmentId);
      setSuccessMessage('Đã tiếp đón (check-in) bệnh nhân thành công!');
      fetchAppointments();
    } catch (err: any) {
      setError(err.message || 'Không thể check-in bệnh nhân.');
    } finally {
      setLoading(false);
    }
  };

  const formatVnd = (amount?: number) => {
    if (amount === undefined || amount === null) return '0 ₫';
    return `${amount.toLocaleString('vi-VN')} ₫`;
  };

  // ==========================================
  // VIEW 1: LOGIN FORM FOR ADMIN
  // ==========================================
  if (!token || !isAuthorizedAdmin) {
    return (
      <div className="min-h-[80vh] flex items-center justify-center py-12 px-4 sm:px-6 lg:px-8 bg-gray-50">
        <div className="max-w-md w-full space-y-8 bg-white p-8 rounded-2xl shadow-xl border border-gray-100">
          <div className="text-center">
            <div className="mx-auto h-16 w-16 bg-purple-600 text-white rounded-2xl flex items-center justify-center shadow-lg shadow-purple-200">
              <ShieldCheck className="h-8 w-8" />
            </div>
            <h2 className="mt-4 text-2xl font-black text-gray-900 tracking-tight">
              Trung Tâm Quản Trị Hệ Thống
            </h2>
            <p className="mt-2 text-sm text-gray-500">
              Đăng nhập tài khoản Quản trị viên (ADMIN) để xem báo cáo thống kê, tài chính và cấu hình danh mục.
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
                Email Quản Trị Viên
              </label>
              <input
                type="email"
                required
                value={loginEmail}
                onChange={(e) => setLoginEmail(e.target.value)}
                placeholder="admin@clinic.vn"
                className="w-full px-3.5 py-2.5 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-purple-500 focus:border-purple-500 transition"
              />
            </div>
            <div>
              <label className="block text-xs font-semibold uppercase text-gray-700 mb-1">
                Mật Khẩu Quản Trị
              </label>
              <input
                type="password"
                required
                value={loginPassword}
                onChange={(e) => setLoginPassword(e.target.value)}
                placeholder="••••••••"
                className="w-full px-3.5 py-2.5 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-purple-500 focus:border-purple-500 transition"
              />
            </div>

            <button
              type="submit"
              disabled={loginLoading}
              className="w-full flex justify-center items-center py-2.5 px-4 border border-transparent rounded-lg shadow-md text-sm font-bold text-white bg-purple-600 hover:bg-purple-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-purple-500 disabled:opacity-50 transition"
            >
              {loginLoading ? (
                <RotateCw className="w-5 h-5 animate-spin mr-2" />
              ) : (
                <ShieldCheck className="w-5 h-5 mr-2" />
              )}
              Đăng Nhập Quản Trị Viên
            </button>
          </form>

          {/* Quick Demo Login */}
          <div className="mt-6 pt-6 border-t border-gray-100">
            <p className="text-xs font-semibold text-gray-400 uppercase tracking-wider text-center mb-3">
              Tài Khoản Mẫu Quản Trị Viên
            </p>
            <button
              type="button"
              onClick={() => handleLogin(undefined, 'admin@clinic.vn', 'Admin@Clinic2026!')}
              disabled={loginLoading}
              className="w-full text-left px-4 py-3 text-xs font-medium bg-purple-50 hover:bg-purple-100 text-purple-900 rounded-xl border border-purple-200 transition flex items-center justify-between shadow-sm"
            >
              <div>
                🛡️ <strong>Hệ Thống Quản Trị Toàn Quyền</strong>
                <div className="text-[11px] text-purple-700 font-mono mt-0.5">admin@clinic.vn</div>
              </div>
              <span className="text-xs font-bold text-purple-600 bg-white px-2.5 py-1 rounded-lg border border-purple-200">
                Click Login
              </span>
            </button>
          </div>
        </div>
      </div>
    );
  }

  // ==========================================
  // VIEW 2: ADMIN PORTAL MAIN INTERFACE
  // ==========================================
  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 space-y-6">
      {/* Top Header Bar */}
      <div className="bg-white p-6 rounded-2xl shadow-sm border border-gray-200 flex flex-col md:flex-row md:items-center md:justify-between gap-4">
        <div className="flex items-center space-x-4">
          <div className="w-14 h-14 rounded-2xl bg-purple-600 text-white flex items-center justify-center shadow-md shadow-purple-100">
            <ShieldCheck className="w-8 h-8" />
          </div>
          <div>
            <div className="flex items-center space-x-2">
              <h1 className="text-2xl font-bold text-gray-900">Trung Tâm Quản Trị Hệ Thống</h1>
              <span className="px-2.5 py-0.5 rounded-full text-xs font-bold bg-purple-100 text-purple-800 uppercase tracking-wide">
                Admin Portal
              </span>
            </div>
            <p className="text-sm text-gray-500 mt-0.5">
              Đăng nhập: <strong>{currentUser?.email || 'admin@clinic.vn'}</strong> • Quyền hạn cao nhất
            </p>
          </div>
        </div>

        <div className="flex items-center space-x-3 self-end md:self-auto">
          <button
            onClick={() => {
              if (activeTab === 'appointments') fetchAppointments();
              if (activeTab === 'reports') fetchReports();
              if (activeTab === 'specialties') fetchSpecialties();
              if (activeTab === 'doctors') fetchDoctors();
              if (activeTab === 'services') fetchServices();
              if (activeTab === 'schedules') fetchSchedules();
            }}
            disabled={loading}
            className="inline-flex items-center px-3.5 py-2 border border-gray-300 rounded-lg text-sm font-medium text-gray-700 bg-white hover:bg-gray-50 shadow-sm transition"
            title="Làm mới dữ liệu"
          >
            <RotateCw className={`w-4 h-4 mr-1.5 ${loading ? 'animate-spin text-purple-600' : ''}`} />
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

      {/* Navigation Tabs */}
      <div className="flex space-x-2 border-b border-gray-200 overflow-x-auto pb-2">
        {[
          { id: 'appointments', label: 'Quản Lý Lịch Hẹn', icon: Calendar },
          { id: 'reports', label: 'Báo Cáo & Doanh Thu', icon: TrendingUp },
          { id: 'specialties', label: 'Chuyên Khoa', icon: Layers },
          { id: 'doctors', label: 'Bác Sĩ', icon: Stethoscope },
          { id: 'services', label: 'Dịch Vụ Khám', icon: Briefcase },
          { id: 'schedules', label: 'Lịch Làm Việc Định Kỳ', icon: Clock },
        ].map((t) => {
          const Icon = t.icon;
          const isActive = activeTab === t.id;
          return (
            <button
              key={t.id}
              onClick={() => setActiveTab(t.id as any)}
              className={`flex items-center space-x-2 px-4 py-2.5 rounded-xl text-sm font-bold whitespace-nowrap transition ${isActive
                  ? 'bg-purple-600 text-white shadow-md shadow-purple-100'
                  : 'text-gray-600 hover:bg-gray-100 hover:text-gray-900'
                }`}
            >
              <Icon className="w-4 h-4" />
              <span>{t.label}</span>
            </button>
          );
        })}
      </div>

      {/* ========================================== */}
      {/* TAB 0: APPOINTMENTS MANAGEMENT             */}
      {/* ========================================== */}
      {activeTab === 'appointments' && (
        <div className="bg-white rounded-2xl border border-gray-200 shadow-sm overflow-hidden space-y-4 p-6">
          <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 pb-4 border-b border-gray-200">
            <div>
              <div className="flex items-center space-x-2">
                <span className="px-2.5 py-0.5 rounded-full text-[11px] font-bold bg-teal-100 text-teal-800">
                  Thời Gian Thực
                </span>
                <h2 className="text-lg font-bold text-gray-900">Danh Sách Ca Khám Toàn Hệ Thống</h2>
              </div>
              <p className="text-xs text-gray-500 mt-0.5">
                Theo dõi tất cả lịch hẹn của bệnh nhân từ link đăng ký trực tuyến và tiếp đón tại phòng khám.
              </p>
            </div>
            <div className="flex items-center gap-2">
              <Link
                to="/receptionist"
                className="inline-flex items-center px-3.5 py-2 bg-teal-50 border border-teal-200 text-teal-700 hover:bg-teal-100 rounded-xl text-xs font-bold transition shadow-sm"
              >
                <span>Mở Bàn Lễ Tân Check-in</span>
                <ArrowRight className="w-3.5 h-3.5 ml-1.5" />
              </Link>
            </div>
          </div>

          {/* Filters Bar */}
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-3 bg-gray-50 p-4 rounded-xl border border-gray-200">
            <div>
              <div className="flex items-center justify-between mb-1">
                <label className="text-[11px] font-bold text-gray-500">Ngày khám</label>
                <div className="flex gap-1 text-[10px]">
                  <button
                    type="button"
                    onClick={() => {
                      const today = new Date(Date.now() + 7 * 3600000).toISOString().split('T')[0];
                      setAptFilterDate(today);
                    }}
                    className={`px-1.5 py-0.5 rounded font-semibold transition ${
                      aptFilterDate === new Date(Date.now() + 7 * 3600000).toISOString().split('T')[0]
                        ? 'bg-purple-600 text-white'
                        : 'bg-gray-200 text-gray-600 hover:bg-gray-300'
                    }`}
                  >
                    Hôm nay
                  </button>
                  <button
                    type="button"
                    onClick={() => {
                      const tomorrow = new Date(Date.now() + 7 * 3600000 + 86400000).toISOString().split('T')[0];
                      setAptFilterDate(tomorrow);
                    }}
                    className={`px-1.5 py-0.5 rounded font-semibold transition ${
                      aptFilterDate === new Date(Date.now() + 7 * 3600000 + 86400000).toISOString().split('T')[0]
                        ? 'bg-purple-600 text-white'
                        : 'bg-gray-200 text-gray-600 hover:bg-gray-300'
                    }`}
                  >
                    Ngày mai
                  </button>
                  <button
                    type="button"
                    onClick={() => setAptFilterDate('')}
                    className={`px-1.5 py-0.5 rounded font-semibold transition ${
                      !aptFilterDate ? 'bg-purple-600 text-white' : 'bg-gray-200 text-gray-600 hover:bg-gray-300'
                    }`}
                  >
                    Tất cả
                  </button>
                </div>
              </div>
              <input
                type="date"
                value={aptFilterDate}
                onChange={(e) => setAptFilterDate(e.target.value)}
                className="w-full px-3 py-1.5 rounded-xl border border-gray-300 text-xs focus:ring-2 focus:ring-purple-500 bg-white"
              />
            </div>

            <div>
              <label className="block text-[11px] font-bold text-gray-500 mb-1">Trạng thái</label>
              <select
                value={aptFilterStatus}
                onChange={(e) => setAptFilterStatus(e.target.value)}
                className="w-full px-3 py-1.5 rounded-xl border border-gray-300 text-xs focus:ring-2 focus:ring-purple-500 bg-white"
              >
                <option value="">Tất cả trạng thái</option>
                <option value="PendingConfirmation">Chờ Xác Nhận</option>
                <option value="Confirmed">Đã Xác Nhận</option>
                <option value="CheckedIn">Đã Tiếp Đón</option>
                <option value="InProgress">Đang Khám</option>
                <option value="Completed">Hoàn Tất</option>
                <option value="Cancelled">Đã Hủy</option>
              </select>
            </div>

            <div>
              <label className="block text-[11px] font-bold text-gray-500 mb-1">Tìm kiếm</label>
              <div className="relative">
                <input
                  type="text"
                  value={aptSearch}
                  onChange={(e) => setAptSearch(e.target.value)}
                  onKeyDown={(e) => e.key === 'Enter' && fetchAppointments()}
                  placeholder="Tên, số điện thoại, mã booking..."
                  className="w-full pl-8 pr-3 py-1.5 rounded-xl border border-gray-300 text-xs focus:ring-2 focus:ring-purple-500 bg-white"
                />
                <Search className="w-3.5 h-3.5 text-gray-400 absolute left-2.5 top-2.5" />
              </div>
            </div>
          </div>

          {/* Table */}
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs border-collapse">
              <thead>
                <tr className="border-b border-gray-200 text-gray-500 font-semibold uppercase tracking-wider bg-gray-50/50">
                  <th className="py-3 px-4">STT</th>
                  <th className="py-3 px-4">Mã / Bệnh Nhân</th>
                  <th className="py-3 px-4">Liên Hệ</th>
                  <th className="py-3 px-4">Bác Sĩ / Phòng</th>
                  <th className="py-3 px-4">Dịch Vụ Khám</th>
                  <th className="py-3 px-4">Giờ Khám (VN)</th>
                  <th className="py-3 px-4">Trạng Thái</th>
                  <th className="py-3 px-4 text-right">Thao Tác</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {appointmentsList.length === 0 ? (
                  <tr>
                    <td colSpan={8} className="py-8 text-center text-xs text-gray-400 italic">
                      Không có ca khám nào phù hợp với bộ lọc hiện tại.
                    </td>
                  </tr>
                ) : (
                  appointmentsList.map((apt, idx) => (
                    <tr key={apt.id} className="hover:bg-purple-50/20 transition">
                      <td className="py-3.5 px-4 font-mono font-bold text-gray-400">
                        #{apt.queueNumber || idx + 1}
                      </td>

                      <td className="py-3.5 px-4">
                        <span className="font-mono font-bold text-teal-700 block">{apt.bookingCode}</span>
                        <span className="font-bold text-gray-900">{apt.patientName}</span>
                        {apt.isGuest && (
                          <span className="text-[10px] bg-gray-100 text-gray-600 px-1 py-0.5 rounded ml-1">
                            Guest
                          </span>
                        )}
                        {apt.reasonForVisit && (
                          <p className="text-[11px] text-gray-500 italic mt-0.5">
                            "{apt.reasonForVisit}"
                          </p>
                        )}
                      </td>

                      <td className="py-3.5 px-4 text-gray-600">
                        <div>{apt.maskedPhoneNumber || '--'}</div>
                        <div className="text-[11px] text-gray-400">
                          {apt.dateOfBirth || (apt.birthYear ? `Năm sinh: ${apt.birthYear}` : '')}
                        </div>
                      </td>

                      <td className="py-3.5 px-4 text-gray-600">
                        <div className="font-medium text-gray-900">{apt.doctorName}</div>
                        <div className="text-gray-400">{apt.roomNumber || 'Phòng khám'}</div>
                      </td>

                      <td className="py-3.5 px-4 text-gray-600">
                        <div className="font-medium">{apt.serviceName}</div>
                        <div className="text-purple-700 font-bold">{formatVnd(apt.servicePrice)}</div>
                      </td>

                      <td className="py-3.5 px-4 font-semibold text-gray-800">
                        {formatDateTimeVn(apt.startAt)}
                      </td>

                      <td className="py-3.5 px-4">
                        <span
                          className={`px-2.5 py-1 rounded-full text-[11px] font-bold inline-block ${
                            apt.status === 'Confirmed'
                              ? 'bg-blue-100 text-blue-800'
                              : apt.status === 'CheckedIn'
                                ? 'bg-emerald-100 text-emerald-800'
                                : apt.status === 'Completed'
                                  ? 'bg-purple-100 text-purple-800'
                                  : apt.status === 'Cancelled'
                                    ? 'bg-rose-100 text-rose-800'
                                    : apt.status === 'InProgress'
                                      ? 'bg-indigo-100 text-indigo-800'
                                      : 'bg-amber-100 text-amber-800'
                          }`}
                        >
                          {apt.status === 'PendingConfirmation' && 'Chờ Xác Nhận'}
                          {apt.status === 'Confirmed' && 'Đã Xác Nhận'}
                          {apt.status === 'CheckedIn' && 'Đã Tiếp Đón'}
                          {apt.status === 'InProgress' && 'Đang Khám'}
                          {apt.status === 'Completed' && 'Đã Hoàn Tất'}
                          {apt.status === 'Cancelled' && 'Đã Hủy'}
                          {apt.status === 'NoShow' && 'Vắng Mặt'}
                        </span>
                      </td>

                      <td className="py-3.5 px-4 text-right space-x-2">
                        {apt.status === 'PendingConfirmation' && (
                          <button
                            onClick={() => handleConfirmAppointment(apt.id)}
                            disabled={loading}
                            className="px-3 py-1.5 bg-teal-600 hover:bg-teal-700 text-white rounded-lg font-bold text-xs transition shadow-sm disabled:opacity-50"
                          >
                            Xác Nhận
                          </button>
                        )}

                        {apt.status === 'Confirmed' && (
                          <button
                            onClick={() => handleCheckIn(apt.id)}
                            disabled={loading}
                            className="inline-flex items-center px-3 py-1.5 bg-emerald-600 hover:bg-emerald-700 text-white rounded-lg font-bold text-xs transition shadow-sm disabled:opacity-50"
                          >
                            <UserCheck className="w-3.5 h-3.5 mr-1" /> Check-in
                          </button>
                        )}

                        {apt.status === 'CheckedIn' && (
                          <span className="text-xs text-emerald-600 font-semibold flex items-center justify-end">
                            <CheckCircle2 className="w-3.5 h-3.5 mr-1" /> Chờ khám
                          </span>
                        )}

                        {apt.status === 'Completed' && (
                          <span className="text-xs text-purple-600 font-semibold flex items-center justify-end">
                            <CheckCircle2 className="w-3.5 h-3.5 mr-1" /> Hoàn tất
                          </span>
                        )}
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}

            {/* ========================================== */}
      {/* TAB 1: REPORTS & FINANCIAL DASHBOARD      */}
      {/* ========================================== */}
      {activeTab === 'reports' && (
        <div className="space-y-6">
          {/* Filters Bar */}
          <div className="bg-white p-5 rounded-2xl border border-gray-200 shadow-sm space-y-4">
            <div className="flex items-center justify-between">
              <h2 className="text-base font-bold text-gray-900 flex items-center">
                <TrendingUp className="w-5 h-5 mr-2 text-purple-600" />
                Bộ Lọc Thống Kê & Báo Cáo
              </h2>
              <button
                onClick={fetchReports}
                className="px-4 py-1.5 rounded-lg text-xs font-bold text-white bg-purple-600 hover:bg-purple-700 shadow-sm transition"
              >
                Áp Dụng Lọc
              </button>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-3">
              <div>
                <label className="block text-xs font-semibold text-gray-600 mb-1">Từ ngày</label>
                <input
                  type="date"
                  value={reportFromDate}
                  onChange={(e) => setReportFromDate(e.target.value)}
                  className="w-full px-3 py-1.5 text-xs border border-gray-300 rounded-lg focus:ring-1 focus:ring-purple-500"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-gray-600 mb-1">Đến ngày</label>
                <input
                  type="date"
                  value={reportToDate}
                  onChange={(e) => setReportToDate(e.target.value)}
                  className="w-full px-3 py-1.5 text-xs border border-gray-300 rounded-lg focus:ring-1 focus:ring-purple-500"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-gray-600 mb-1">Bác sĩ</label>
                <select
                  value={reportDoctorId}
                  onChange={(e) => setReportDoctorId(e.target.value)}
                  className="w-full px-3 py-1.5 text-xs border border-gray-300 rounded-lg focus:ring-1 focus:ring-purple-500"
                >
                  <option value="">Tất cả bác sĩ</option>
                  {doctorsList.map((d) => (
                    <option key={d.id} value={d.id}>
                      {d.fullName} ({d.specialtyName})
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block text-xs font-semibold text-gray-600 mb-1">Chuyên khoa</label>
                <select
                  value={reportSpecialtyId}
                  onChange={(e) => setReportSpecialtyId(e.target.value)}
                  className="w-full px-3 py-1.5 text-xs border border-gray-300 rounded-lg focus:ring-1 focus:ring-purple-500"
                >
                  <option value="">Tất cả chuyên khoa</option>
                  {specialtiesList.map((s) => (
                    <option key={s.id} value={s.id}>
                      {s.name}
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block text-xs font-semibold text-gray-600 mb-1">Trạng thái</label>
                <select
                  value={reportStatus}
                  onChange={(e) => setReportStatus(e.target.value)}
                  className="w-full px-3 py-1.5 text-xs border border-gray-300 rounded-lg focus:ring-1 focus:ring-purple-500"
                >
                  <option value="">Tất cả trạng thái</option>
                  <option value="Completed">Đã Hoàn Tất</option>
                  <option value="CheckedIn">Chờ Khám</option>
                  <option value="InProgress">Đang Khám</option>
                  <option value="Confirmed">Đã Xác Nhận</option>
                  <option value="Cancelled">Đã Hủy</option>
                </select>
              </div>
            </div>
          </div>

          {/* Financial Metrics Cards */}
          {dashboardReport && (
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
              <div className="bg-white p-5 rounded-2xl border border-gray-200 shadow-sm">
                <span className="text-xs font-bold text-gray-500 uppercase">Tổng Doanh Thu Ca Đặt</span>
                <div className="mt-2 flex items-baseline justify-between">
                  <span className="text-2xl font-black text-gray-900">
                    {formatVnd(dashboardReport.grossRevenue)}
                  </span>
                  <DollarSign className="w-5 h-5 text-gray-400" />
                </div>
                <span className="text-xs text-gray-400 mt-1 block">
                  Tổng ca: <strong>{dashboardReport.totalAppointments}</strong> cuộc hẹn
                </span>
              </div>

              <div className="bg-white p-5 rounded-2xl border border-emerald-200 bg-emerald-50/20 shadow-sm">
                <span className="text-xs font-bold text-emerald-700 uppercase">Doanh Thu Thực Nhận</span>
                <div className="mt-2 flex items-baseline justify-between">
                  <span className="text-2xl font-black text-emerald-600">
                    {formatVnd(dashboardReport.netRevenue)}
                  </span>
                  <TrendingUp className="w-5 h-5 text-emerald-500" />
                </div>
                <span className="text-xs text-emerald-700 mt-1 block">
                  Bảo toàn sau đối soát hủy lịch
                </span>
              </div>

              <div className="bg-white p-5 rounded-2xl border border-blue-200 bg-blue-50/20 shadow-sm">
                <span className="text-xs font-bold text-blue-700 uppercase">Đã Thanh Toán</span>
                <div className="mt-2 flex items-baseline justify-between">
                  <span className="text-2xl font-black text-blue-600">
                    {formatVnd(dashboardReport.paidAmount)}
                  </span>
                  <CheckCircle2 className="w-5 h-5 text-blue-500" />
                </div>
                <span className="text-xs text-blue-700 mt-1 block">
                  Tiền bệnh nhân thanh toán thực tế
                </span>
              </div>

              <div className="bg-white p-5 rounded-2xl border border-amber-200 bg-amber-50/20 shadow-sm">
                <span className="text-xs font-bold text-amber-700 uppercase">Phí Hủy Đã Thu</span>
                <div className="mt-2 flex items-baseline justify-between">
                  <span className="text-2xl font-black text-amber-600">
                    {formatVnd(dashboardReport.cancellationFeeCollected)}
                  </span>
                  <AlertCircle className="w-5 h-5 text-amber-500" />
                </div>
                <span className="text-xs text-amber-700 mt-1 block">
                  Áp dụng mốc 60 phút
                </span>
              </div>
            </div>
          )}

          {/* Status Breakdown Pills */}
          {dashboardReport?.statusBreakdown && (
            <div className="bg-white p-5 rounded-2xl border border-gray-200 shadow-sm">
              <h3 className="text-sm font-bold text-gray-900 mb-3">
                Phân Rã Trạng Thái Ca Khám (Status Breakdown)
              </h3>
              <div className="grid grid-cols-2 sm:grid-cols-4 lg:grid-cols-7 gap-3 text-center">
                <div className="p-3 rounded-xl bg-gray-50 border border-gray-200">
                  <span className="text-[11px] font-semibold text-gray-500 uppercase block">Tổng Ca</span>
                  <span className="text-xl font-bold text-gray-900 mt-1 block">
                    {dashboardReport.statusBreakdown.total}
                  </span>
                </div>
                <div className="p-3 rounded-xl bg-emerald-50 border border-emerald-200">
                  <span className="text-[11px] font-semibold text-emerald-700 uppercase block">Hoàn Tất</span>
                  <span className="text-xl font-bold text-emerald-700 mt-1 block">
                    {dashboardReport.statusBreakdown.completed}
                  </span>
                </div>
                <div className="p-3 rounded-xl bg-blue-50 border border-blue-200">
                  <span className="text-[11px] font-semibold text-blue-700 uppercase block">Đang Khám</span>
                  <span className="text-xl font-bold text-blue-700 mt-1 block">
                    {dashboardReport.statusBreakdown.inProgress}
                  </span>
                </div>
                <div className="p-3 rounded-xl bg-amber-50 border border-amber-200">
                  <span className="text-[11px] font-semibold text-amber-700 uppercase block">Chờ Khám</span>
                  <span className="text-xl font-bold text-amber-700 mt-1 block">
                    {dashboardReport.statusBreakdown.checkedIn}
                  </span>
                </div>
                <div className="p-3 rounded-xl bg-teal-50 border border-teal-200">
                  <span className="text-[11px] font-semibold text-teal-700 uppercase block">Đã Xác Nhận</span>
                  <span className="text-xl font-bold text-teal-700 mt-1 block">
                    {dashboardReport.statusBreakdown.confirmed}
                  </span>
                </div>
                <div className="p-3 rounded-xl bg-yellow-50 border border-yellow-200">
                  <span className="text-[11px] font-semibold text-yellow-700 uppercase block">Chờ Duyệt</span>
                  <span className="text-xl font-bold text-yellow-700 mt-1 block">
                    {dashboardReport.statusBreakdown.pendingConfirmation}
                  </span>
                </div>
                <div className="p-3 rounded-xl bg-red-50 border border-red-200">
                  <span className="text-[11px] font-semibold text-red-700 uppercase block">Đã Hủy</span>
                  <span className="text-xl font-bold text-red-700 mt-1 block">
                    {dashboardReport.statusBreakdown.cancelled}
                  </span>
                </div>
              </div>
            </div>
          )}

          {/* Specialty & Doctor Performance Tables */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            {/* Specialty Report */}
            <div className="bg-white rounded-2xl border border-gray-200 shadow-sm overflow-hidden">
              <div className="p-4 border-b border-gray-200 bg-gray-50 flex items-center justify-between">
                <h3 className="text-sm font-bold text-gray-900 flex items-center">
                  <Layers className="w-4 h-4 mr-1.5 text-purple-600" />
                  Hiệu Suất Theo Chuyên Khoa
                </h3>
                <span className="text-xs text-gray-500 font-semibold">
                  {dashboardReport?.specialtyReports.length || 0} chuyên khoa
                </span>
              </div>
              <div className="overflow-x-auto">
                <table className="min-w-full text-left text-xs divide-y divide-gray-200">
                  <thead className="bg-gray-50/50 text-gray-500 uppercase font-semibold">
                    <tr>
                      <th className="px-4 py-2.5">Chuyên Khoa</th>
                      <th className="px-4 py-2.5 text-center">Tổng Ca</th>
                      <th className="px-4 py-2.5 text-center">Tỷ Lệ</th>
                      <th className="px-4 py-2.5 text-right">Doanh Thu</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-100">
                    {dashboardReport?.specialtyReports && dashboardReport.specialtyReports.length > 0 ? (
                      dashboardReport.specialtyReports.map((s) => (
                        <tr key={s.specialtyId} className="hover:bg-gray-50/80">
                          <td className="px-4 py-3 font-semibold text-gray-900">{s.specialtyName}</td>
                          <td className="px-4 py-3 text-center text-gray-600">{s.totalAppointments}</td>
                          <td className="px-4 py-3 text-center">
                            <span className="inline-block px-2 py-0.5 rounded-full text-[10px] font-bold bg-teal-100 text-teal-800">
                              {s.completionRate}%
                            </span>
                          </td>
                          <td className="px-4 py-3 text-right font-bold text-gray-900">
                            {formatVnd(s.grossRevenue)}
                          </td>
                        </tr>
                      ))
                    ) : (
                      <tr>
                        <td colSpan={4} className="px-4 py-6 text-center text-gray-400">
                          Chưa có số liệu chuyên khoa.
                        </td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>
            </div>

            {/* Doctor Report */}
            <div className="bg-white rounded-2xl border border-gray-200 shadow-sm overflow-hidden">
              <div className="p-4 border-b border-gray-200 bg-gray-50 flex items-center justify-between">
                <h3 className="text-sm font-bold text-gray-900 flex items-center">
                  <Stethoscope className="w-4 h-4 mr-1.5 text-purple-600" />
                  Hiệu Suất Theo Bác Sĩ
                </h3>
                <span className="text-xs text-gray-500 font-semibold">
                  {dashboardReport?.doctorReports.length || 0} bác sĩ
                </span>
              </div>
              <div className="overflow-x-auto">
                <table className="min-w-full text-left text-xs divide-y divide-gray-200">
                  <thead className="bg-gray-50/50 text-gray-500 uppercase font-semibold">
                    <tr>
                      <th className="px-4 py-2.5">Bác Sĩ</th>
                      <th className="px-4 py-2.5">Chuyên Khoa</th>
                      <th className="px-4 py-2.5 text-center">Tổng Ca</th>
                      <th className="px-4 py-2.5 text-center">Tỷ Lệ</th>
                      <th className="px-4 py-2.5 text-right">Doanh Thu</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-100">
                    {dashboardReport?.doctorReports && dashboardReport.doctorReports.length > 0 ? (
                      dashboardReport.doctorReports.map((d) => (
                        <tr key={d.doctorId} className="hover:bg-gray-50/80">
                          <td className="px-4 py-3 font-semibold text-gray-900">{d.doctorName}</td>
                          <td className="px-4 py-3 text-gray-500">{d.specialtyName}</td>
                          <td className="px-4 py-3 text-center text-gray-600">{d.totalAppointments}</td>
                          <td className="px-4 py-3 text-center">
                            <span className="inline-block px-2 py-0.5 rounded-full text-[10px] font-bold bg-blue-100 text-blue-800">
                              {d.completionRate}%
                            </span>
                          </td>
                          <td className="px-4 py-3 text-right font-bold text-gray-900">
                            {formatVnd(d.grossRevenue)}
                          </td>
                        </tr>
                      ))
                    ) : (
                      <tr>
                        <td colSpan={5} className="px-4 py-6 text-center text-gray-400">
                          Chưa có số liệu bác sĩ.
                        </td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* ========================================== */}
      {/* TAB 2: SPECIALTIES CRUD                  */}
      {/* ========================================== */}
      {activeTab === 'specialties' && (
        <div className="bg-white rounded-2xl border border-gray-200 shadow-sm overflow-hidden space-y-4 p-6">
          <div className="flex items-center justify-between pb-4 border-b border-gray-200">
            <div>
              <h2 className="text-lg font-bold text-gray-900">Danh Mục Chuyên Khoa Khám Bệnh</h2>
              <p className="text-xs text-gray-500 mt-0.5">
                Quản lý chuyên khoa khám, mã định danh và trạng thái kích hoạt
              </p>
            </div>
            <button
              onClick={() => setShowSpecialtyModal(true)}
              className="inline-flex items-center px-4 py-2 rounded-xl text-xs font-bold text-white bg-purple-600 hover:bg-purple-700 shadow-md transition"
            >
              <Plus className="w-4 h-4 mr-1.5" /> Thêm Chuyên Khoa Mới
            </button>
          </div>

          <div className="overflow-x-auto">
            <table className="min-w-full text-left text-sm divide-y divide-gray-200">
              <thead className="bg-gray-50 text-gray-500 text-xs uppercase font-semibold">
                <tr>
                  <th className="px-4 py-3">Mã Code</th>
                  <th className="px-4 py-3">Tên Chuyên Khoa</th>
                  <th className="px-4 py-3">Slug</th>
                  <th className="px-4 py-3">Mô Tả</th>
                  <th className="px-4 py-3 text-center">Số Dịch Vụ</th>
                  <th className="px-4 py-3 text-center">Trạng Thái</th>
                  <th className="px-4 py-3 text-right">Thao Tác</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {specialtiesList.map((s) => (
                  <tr key={s.id} className="hover:bg-gray-50/80 transition">
                    <td className="px-4 py-3 font-mono text-xs font-bold text-purple-700">{s.code}</td>
                    <td className="px-4 py-3 font-bold text-gray-900">{s.name}</td>
                    <td className="px-4 py-3 font-mono text-xs text-gray-400">{s.slug}</td>
                    <td className="px-4 py-3 text-xs text-gray-600 max-w-xs truncate">{s.description || '-'}</td>
                    <td className="px-4 py-3 text-center font-bold text-gray-700">{s.serviceCount}</td>
                    <td className="px-4 py-3 text-center">
                      <span
                        className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold ${s.isActive
                            ? 'bg-emerald-100 text-emerald-800'
                            : 'bg-gray-100 text-gray-600'
                          }`}
                      >
                        {s.isActive ? 'Đang hoạt động' : 'Tạm tắt'}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-right">
                      <button
                        onClick={() => handleToggleSpecialtyStatus(s.id, s.isActive)}
                        className={`inline-flex items-center px-2.5 py-1 rounded-lg text-xs font-semibold transition ${s.isActive
                            ? 'text-red-700 bg-red-50 hover:bg-red-100'
                            : 'text-emerald-700 bg-emerald-50 hover:bg-emerald-100'
                          }`}
                      >
                        {s.isActive ? 'Vô hiệu hóa' : 'Kích hoạt'}
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* ========================================== */}
      {/* TAB 3: DOCTORS CRUD                      */}
      {/* ========================================== */}
      {activeTab === 'doctors' && (
        <div className="bg-white rounded-2xl border border-gray-200 shadow-sm overflow-hidden space-y-4 p-6">
          <div className="flex items-center justify-between pb-4 border-b border-gray-200">
            <div>
              <h2 className="text-lg font-bold text-gray-900">Danh Sách Đội Ngũ Bác Sĩ</h2>
              <p className="text-xs text-gray-500 mt-0.5">
                Quản lý hồ sơ bác sĩ, phân công chuyên khoa và tài khoản đăng nhập hệ thống
              </p>
            </div>
            <button
              onClick={() => setShowDoctorModal(true)}
              className="inline-flex items-center px-4 py-2 rounded-xl text-xs font-bold text-white bg-purple-600 hover:bg-purple-700 shadow-md transition"
            >
              <Plus className="w-4 h-4 mr-1.5" /> Thêm Bác Sĩ Mới
            </button>
          </div>

          <div className="overflow-x-auto">
            <table className="min-w-full text-left text-sm divide-y divide-gray-200">
              <thead className="bg-gray-50 text-gray-500 text-xs uppercase font-semibold">
                <tr>
                  <th className="px-4 py-3">Mã BS</th>
                  <th className="px-4 py-3">Họ Tên & Học Vị</th>
                  <th className="px-4 py-3">Chuyên Khoa</th>
                  <th className="px-4 py-3">Phòng Khám</th>
                  <th className="px-4 py-3">Email Đăng Nhập</th>
                  <th className="px-4 py-3 text-center">Trạng Thái</th>
                  <th className="px-4 py-3 text-right">Thao Tác</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {doctorsList.map((d) => (
                  <tr key={d.id} className="hover:bg-gray-50/80 transition">
                    <td className="px-4 py-3 font-mono text-xs font-bold text-purple-700">{d.doctorCode}</td>
                    <td className="px-4 py-3">
                      <div className="flex items-center space-x-3">
                        <div className="w-10 h-10 rounded-xl overflow-hidden bg-purple-50 border border-purple-200 flex-shrink-0 shadow-sm">
                          <img
                            src={d.avatarUrl || getDoctorAvatar(d)}
                            alt={d.fullName}
                            className="w-full h-full object-cover"
                            loading="lazy"
                            onError={(e) => {
                              const target = e.currentTarget;
                              target.onerror = null;
                              const fallback = getDoctorLocalFallback(d);
                              target.src = fallback;
                            }}
                          />
                        </div>
                        <div>
                          <div className="font-bold text-gray-900">{d.fullName}</div>
                          <div className="text-xs text-gray-400">{d.title || 'Bác Sĩ'}</div>
                        </div>
                      </div>
                    </td>
                    <td className="px-4 py-3 font-semibold text-gray-700">{d.specialtyName}</td>
                    <td className="px-4 py-3 text-xs font-mono text-gray-600">{d.roomNumber || '-'}</td>
                    <td className="px-4 py-3 text-xs text-gray-500 font-mono">{d.email}</td>
                    <td className="px-4 py-3 text-center">
                      <span
                        className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold ${d.isActive
                            ? 'bg-emerald-100 text-emerald-800'
                            : 'bg-gray-100 text-gray-600'
                          }`}
                      >
                        {d.isActive ? 'Đang hoạt động' : 'Tạm tắt'}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-right">
                      <button
                        onClick={() => handleToggleDoctorStatus(d.id, d.isActive)}
                        className={`inline-flex items-center px-2.5 py-1 rounded-lg text-xs font-semibold transition ${d.isActive
                            ? 'text-red-700 bg-red-50 hover:bg-red-100'
                            : 'text-emerald-700 bg-emerald-50 hover:bg-emerald-100'
                          }`}
                      >
                        {d.isActive ? 'Vô hiệu hóa' : 'Kích hoạt'}
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* ========================================== */}
      {/* TAB 4: SERVICES CRUD                     */}
      {/* ========================================== */}
      {activeTab === 'services' && (
        <div className="bg-white rounded-2xl border border-gray-200 shadow-sm overflow-hidden space-y-4 p-6">
          <div className="flex items-center justify-between pb-4 border-b border-gray-200">
            <div>
              <h2 className="text-lg font-bold text-gray-900">Danh Mục Dịch Vụ Khám Bệnh</h2>
              <p className="text-xs text-gray-500 mt-0.5">
                Quản lý danh sách dịch vụ, thời lượng khám (phút), giá niêm yết (VND) và chuyên khoa
              </p>
            </div>
            <button
              onClick={() => setShowServiceModal(true)}
              className="inline-flex items-center px-4 py-2 rounded-xl text-xs font-bold text-white bg-purple-600 hover:bg-purple-700 shadow-md transition"
            >
              <Plus className="w-4 h-4 mr-1.5" /> Thêm Dịch Vụ Mới
            </button>
          </div>

          <div className="overflow-x-auto">
            <table className="min-w-full text-left text-sm divide-y divide-gray-200">
              <thead className="bg-gray-50 text-gray-500 text-xs uppercase font-semibold">
                <tr>
                  <th className="px-4 py-3">Mã Code</th>
                  <th className="px-4 py-3">Tên Dịch Vụ</th>
                  <th className="px-4 py-3">Chuyên Khoa</th>
                  <th className="px-4 py-3 text-center">Thời Lượng</th>
                  <th className="px-4 py-3 text-right">Đơn Giá Niêm Yết</th>
                  <th className="px-4 py-3 text-center">Trạng Thái</th>
                  <th className="px-4 py-3 text-right">Thao Tác</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {servicesList.map((srv) => (
                  <tr key={srv.id} className="hover:bg-gray-50/80 transition">
                    <td className="px-4 py-3 font-mono text-xs font-bold text-purple-700">{srv.code}</td>
                    <td className="px-4 py-3 font-bold text-gray-900">{srv.name}</td>
                    <td className="px-4 py-3 text-xs font-semibold text-gray-600">{srv.specialtyName}</td>
                    <td className="px-4 py-3 text-center font-semibold text-gray-700">
                      {srv.durationMinutes} phút
                    </td>
                    <td className="px-4 py-3 text-right font-bold text-purple-700">
                      {formatVnd(srv.price)}
                    </td>
                    <td className="px-4 py-3 text-center">
                      <span
                        className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold ${srv.isActive
                            ? 'bg-emerald-100 text-emerald-800'
                            : 'bg-gray-100 text-gray-600'
                          }`}
                      >
                        {srv.isActive ? 'Đang hoạt động' : 'Tạm tắt'}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-right">
                      <button
                        onClick={() => handleToggleServiceStatus(srv.id, srv.isActive)}
                        className={`inline-flex items-center px-2.5 py-1 rounded-lg text-xs font-semibold transition ${srv.isActive
                            ? 'text-red-700 bg-red-50 hover:bg-red-100'
                            : 'text-emerald-700 bg-emerald-50 hover:bg-emerald-100'
                          }`}
                      >
                        {srv.isActive ? 'Vô hiệu hóa' : 'Kích hoạt'}
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* ========================================== */}
      {/* TAB 5: DOCTOR SCHEDULES CRUD             */}
      {/* ========================================== */}
      {activeTab === 'schedules' && (
        <div className="bg-white rounded-2xl border border-gray-200 shadow-sm overflow-hidden space-y-4 p-6">
          <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 pb-4 border-b border-gray-200">
            <div>
              <h2 className="text-lg font-bold text-gray-900">Lịch Làm Việc Định Kỳ Của Bác Sĩ</h2>
              <p className="text-xs text-gray-500 mt-0.5">
                Cấu hình ca làm việc trong tuần (Thứ 2 - Chủ Nhật), khung giờ và ngày hiệu lực
              </p>
            </div>

            <div className="flex items-center space-x-3">
              <select
                value={selectedDoctorIdForSchedule}
                onChange={(e) => setSelectedDoctorIdForSchedule(e.target.value)}
                className="px-3.5 py-2 border border-gray-300 rounded-xl text-xs font-bold text-gray-800 focus:ring-2 focus:ring-purple-500"
              >
                {doctorsList.map((d) => (
                  <option key={d.id} value={d.id}>
                    👨‍⚕️ {d.fullName} ({d.specialtyName})
                  </option>
                ))}
              </select>

              <button
                onClick={() => setShowScheduleModal(true)}
                className="inline-flex items-center px-4 py-2 rounded-xl text-xs font-bold text-white bg-purple-600 hover:bg-purple-700 shadow-md transition whitespace-nowrap"
              >
                <Plus className="w-4 h-4 mr-1.5" /> Thêm Ca Mới
              </button>
            </div>
          </div>

          <div className="overflow-x-auto">
            <table className="min-w-full text-left text-sm divide-y divide-gray-200">
              <thead className="bg-gray-50 text-gray-500 text-xs uppercase font-semibold">
                <tr>
                  <th className="px-4 py-3">Thứ Trong Tuần</th>
                  <th className="px-4 py-3">Giờ Bắt Đầu</th>
                  <th className="px-4 py-3">Giờ Kết Thúc</th>
                  <th className="px-4 py-3">Thời Lượng Slot</th>
                  <th className="px-4 py-3">Ngày Bắt Đầu Hiệu Lực</th>
                  <th className="px-4 py-3">Ngày Hết Hiệu Lực</th>
                  <th className="px-4 py-3 text-center">Trạng Thái</th>
                  <th className="px-4 py-3 text-right">Thao Tác</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {schedulesList.length > 0 ? (
                  schedulesList.map((sc) => (
                    <tr key={sc.id} className="hover:bg-gray-50/80 transition">
                      <td className="px-4 py-3 font-bold text-purple-900">
                        {sc.dayOfWeekName || `Thứ ${sc.dayOfWeek === 7 ? 'Chủ Nhật' : sc.dayOfWeek + 1}`}
                      </td>
                      <td className="px-4 py-3 font-semibold text-gray-900">{sc.startTime}</td>
                      <td className="px-4 py-3 font-semibold text-gray-900">{sc.endTime}</td>
                      <td className="px-4 py-3 text-xs text-gray-600">{sc.slotDurationMinutes} phút</td>
                      <td className="px-4 py-3 text-xs text-gray-600">{sc.effectiveFrom}</td>
                      <td className="px-4 py-3 text-xs text-gray-400">{sc.effectiveTo || 'Vô thời hạn'}</td>
                      <td className="px-4 py-3 text-center">
                        <span
                          className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold ${sc.isActive
                              ? 'bg-emerald-100 text-emerald-800'
                              : 'bg-gray-100 text-gray-600'
                            }`}
                        >
                          {sc.isActive ? 'Đang hoạt động' : 'Tạm tắt'}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-right space-x-2">
                        <button
                          onClick={() => handleToggleScheduleStatus(sc.id, sc.isActive)}
                          className="text-xs font-semibold text-gray-600 hover:text-purple-600"
                        >
                          {sc.isActive ? (
                            <ToggleRight className="w-5 h-5 text-emerald-600 inline" />
                          ) : (
                            <ToggleLeft className="w-5 h-5 text-gray-400 inline" />
                          )}
                        </button>
                        <button
                          onClick={() => handleDeleteSchedule(sc.id)}
                          className="p-1 text-gray-400 hover:text-red-600 rounded transition"
                          title="Xóa ca làm việc"
                        >
                          <Trash2 className="w-4 h-4 inline" />
                        </button>
                      </td>
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td colSpan={8} className="px-4 py-8 text-center text-xs text-gray-400">
                      Bác sĩ này hiện chưa có ca làm việc định kỳ nào.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* ========================================== */}
      {/* MODAL 1: ADD SPECIALTY                   */}
      {/* ========================================== */}
      {showSpecialtyModal && (
        <div className="fixed inset-0 z-50 bg-black/50 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="bg-white rounded-2xl max-w-md w-full p-6 shadow-2xl space-y-4">
            <div className="flex items-center justify-between pb-3 border-b border-gray-100">
              <h3 className="text-base font-bold text-gray-900">Thêm Chuyên Khoa Mới</h3>
              <button onClick={() => setShowSpecialtyModal(false)} className="text-gray-400 hover:text-gray-600">
                <X className="w-5 h-5" />
              </button>
            </div>
            <form onSubmit={handleCreateSpecialty} className="space-y-3">
              <div>
                <label className="block text-xs font-bold text-gray-700 uppercase mb-1">Tên Chuyên Khoa *</label>
                <input
                  type="text"
                  required
                  value={newSpecialty.name}
                  onChange={(e) => setNewSpecialty({ ...newSpecialty, name: e.target.value })}
                  placeholder="VD: Răng Hàm Mặt"
                  className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-purple-500"
                />
              </div>
              <div>
                <label className="block text-xs font-bold text-gray-700 uppercase mb-1">Mã Code *</label>
                <input
                  type="text"
                  required
                  value={newSpecialty.code}
                  onChange={(e) => setNewSpecialty({ ...newSpecialty, code: e.target.value.toUpperCase() })}
                  placeholder="VD: DENTISTRY"
                  className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-purple-500"
                />
              </div>
              <div>
                <label className="block text-xs font-bold text-gray-700 uppercase mb-1">Mô Tả</label>
                <textarea
                  rows={2}
                  value={newSpecialty.description}
                  onChange={(e) => setNewSpecialty({ ...newSpecialty, description: e.target.value })}
                  placeholder="Mô tả dịch vụ và phạm vi chuyên khoa..."
                  className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-purple-500"
                />
              </div>
              <div className="pt-3 border-t border-gray-100 flex justify-end space-x-2">
                <button
                  type="button"
                  onClick={() => setShowSpecialtyModal(false)}
                  className="px-4 py-2 text-xs font-semibold text-gray-600 hover:bg-gray-100 rounded-lg"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  className="px-5 py-2 text-xs font-bold text-white bg-purple-600 hover:bg-purple-700 rounded-lg shadow-md"
                >
                  Lưu Chuyên Khoa
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* ========================================== */}
      {/* MODAL 2: ADD DOCTOR                      */}
      {/* ========================================== */}
      {showDoctorModal && (
        <div className="fixed inset-0 z-50 bg-black/50 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="bg-white rounded-2xl max-w-lg w-full p-6 shadow-2xl space-y-4 max-h-[90vh] overflow-y-auto">
            <div className="flex items-center justify-between pb-3 border-b border-gray-100">
              <h3 className="text-base font-bold text-gray-900">Tạo Hồ Sơ & Tài Khoản Bác Sĩ Mới</h3>
              <button onClick={() => setShowDoctorModal(false)} className="text-gray-400 hover:text-gray-600">
                <X className="w-5 h-5" />
              </button>
            </div>
            <form onSubmit={handleCreateDoctor} className="space-y-3">
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-bold text-gray-700 uppercase mb-1">Họ Tên Đầy Đủ *</label>
                  <input
                    type="text"
                    required
                    value={newDoctor.fullName}
                    onChange={(e) => setNewDoctor({ ...newDoctor, fullName: e.target.value })}
                    placeholder="VD: BS. Phạm Minh Tuấn"
                    className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-purple-500"
                  />
                </div>
                <div>
                  <label className="block text-xs font-bold text-gray-700 uppercase mb-1">Mã Bác Sĩ *</label>
                  <input
                    type="text"
                    required
                    value={newDoctor.doctorCode}
                    onChange={(e) => setNewDoctor({ ...newDoctor, doctorCode: e.target.value.toUpperCase() })}
                    placeholder="VD: DOC-006"
                    className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-purple-500"
                  />
                </div>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-bold text-gray-700 uppercase mb-1">Email Đăng Nhập *</label>
                  <input
                    type="email"
                    required
                    value={newDoctor.email}
                    onChange={(e) => setNewDoctor({ ...newDoctor, email: e.target.value })}
                    placeholder="dr.tuan@clinic.vn"
                    className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-purple-500"
                  />
                </div>
                <div>
                  <label className="block text-xs font-bold text-gray-700 uppercase mb-1">Mật Khẩu Ban Đầu</label>
                  <input
                    type="password"
                    value={newDoctor.password}
                    onChange={(e) => setNewDoctor({ ...newDoctor, password: e.target.value })}
                    placeholder="Mặc định: Doctor@2026"
                    className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-purple-500"
                  />
                </div>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-bold text-gray-700 uppercase mb-1">Chuyên Khoa Phụ Trách *</label>
                  <select
                    required
                    value={newDoctor.specialtyId}
                    onChange={(e) => setNewDoctor({ ...newDoctor, specialtyId: e.target.value })}
                    className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-purple-500"
                  >
                    <option value="">Chọn chuyên khoa</option>
                    {specialtiesList.map((s) => (
                      <option key={s.id} value={s.id}>
                        {s.name}
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="block text-xs font-bold text-gray-700 uppercase mb-1">Phòng Khám</label>
                  <input
                    type="text"
                    value={newDoctor.roomNumber}
                    onChange={(e) => setNewDoctor({ ...newDoctor, roomNumber: e.target.value })}
                    placeholder="VD: P.201"
                    className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-purple-500"
                  />
                </div>
              </div>

              <div className="pt-3 border-t border-gray-100 flex justify-end space-x-2">
                <button
                  type="button"
                  onClick={() => setShowDoctorModal(false)}
                  className="px-4 py-2 text-xs font-semibold text-gray-600 hover:bg-gray-100 rounded-lg"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  className="px-5 py-2 text-xs font-bold text-white bg-purple-600 hover:bg-purple-700 rounded-lg shadow-md"
                >
                  Tạo Hồ Sơ Bác Sĩ
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* ========================================== */}
      {/* MODAL 3: ADD SERVICE                     */}
      {/* ========================================== */}
      {showServiceModal && (
        <div className="fixed inset-0 z-50 bg-black/50 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="bg-white rounded-2xl max-w-md w-full p-6 shadow-2xl space-y-4">
            <div className="flex items-center justify-between pb-3 border-b border-gray-100">
              <h3 className="text-base font-bold text-gray-900">Thêm Dịch Vụ Khám Mới</h3>
              <button onClick={() => setShowServiceModal(false)} className="text-gray-400 hover:text-gray-600">
                <X className="w-5 h-5" />
              </button>
            </div>
            <form onSubmit={handleCreateService} className="space-y-3">
              <div>
                <label className="block text-xs font-bold text-gray-700 uppercase mb-1">Chuyên Khoa *</label>
                <select
                  required
                  value={newService.specialtyId}
                  onChange={(e) => setNewService({ ...newService, specialtyId: e.target.value })}
                  className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-purple-500"
                >
                  <option value="">Chọn chuyên khoa</option>
                  {specialtiesList.map((s) => (
                    <option key={s.id} value={s.id}>
                      {s.name}
                    </option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-xs font-bold text-gray-700 uppercase mb-1">Tên Dịch Vụ *</label>
                <input
                  type="text"
                  required
                  value={newService.name}
                  onChange={(e) => setNewService({ ...newService, name: e.target.value })}
                  placeholder="VD: Siêu âm Doppler tim màu"
                  className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-purple-500"
                />
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-bold text-gray-700 uppercase mb-1">Mã Code *</label>
                  <input
                    type="text"
                    required
                    value={newService.code}
                    onChange={(e) => setNewService({ ...newService, code: e.target.value.toUpperCase() })}
                    placeholder="VD: SRV-CARDIO-02"
                    className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-purple-500"
                  />
                </div>
                <div>
                  <label className="block text-xs font-bold text-gray-700 uppercase mb-1">Thời Lượng (Phút) *</label>
                  <input
                    type="number"
                    min="15"
                    step="15"
                    required
                    value={newService.durationMinutes}
                    onChange={(e) => setNewService({ ...newService, durationMinutes: Number(e.target.value) })}
                    className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-purple-500"
                  />
                </div>
              </div>
              <div>
                <label className="block text-xs font-bold text-gray-700 uppercase mb-1">Đơn Giá Niêm Yết (VND) *</label>
                <input
                  type="number"
                  min="0"
                  step="10000"
                  required
                  value={newService.price}
                  onChange={(e) => setNewService({ ...newService, price: Number(e.target.value) })}
                  className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-purple-500"
                />
              </div>
              <div className="pt-3 border-t border-gray-100 flex justify-end space-x-2">
                <button
                  type="button"
                  onClick={() => setShowServiceModal(false)}
                  className="px-4 py-2 text-xs font-semibold text-gray-600 hover:bg-gray-100 rounded-lg"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  className="px-5 py-2 text-xs font-bold text-white bg-purple-600 hover:bg-purple-700 rounded-lg shadow-md"
                >
                  Lưu Dịch Vụ
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* ========================================== */}
      {/* MODAL 4: ADD SCHEDULE                    */}
      {/* ========================================== */}
      {showScheduleModal && (
        <div className="fixed inset-0 z-50 bg-black/50 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="bg-white rounded-2xl max-w-md w-full p-6 shadow-2xl space-y-4">
            <div className="flex items-center justify-between pb-3 border-b border-gray-100">
              <h3 className="text-base font-bold text-gray-900">Thiết Lập Ca Làm Việc Định Kỳ</h3>
              <button onClick={() => setShowScheduleModal(false)} className="text-gray-400 hover:text-gray-600">
                <X className="w-5 h-5" />
              </button>
            </div>
            <form onSubmit={handleCreateSchedule} className="space-y-3">
              <div>
                <label className="block text-xs font-bold text-gray-700 uppercase mb-1">Thứ Trong Tuần *</label>
                <select
                  value={newSchedule.dayOfWeek}
                  onChange={(e) => setNewSchedule({ ...newSchedule, dayOfWeek: Number(e.target.value) })}
                  className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-purple-500"
                >
                  <option value={1}>Thứ Hai (Monday)</option>
                  <option value={2}>Thứ Ba (Tuesday)</option>
                  <option value={3}>Thứ Tư (Wednesday)</option>
                  <option value={4}>Thứ Năm (Thursday)</option>
                  <option value={5}>Thứ Sáu (Friday)</option>
                  <option value={6}>Thứ Bảy (Saturday)</option>
                  <option value={7}>Chủ Nhật (Sunday)</option>
                </select>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-bold text-gray-700 uppercase mb-1">Giờ Bắt Đầu *</label>
                  <input
                    type="time"
                    required
                    value={newSchedule.startTime}
                    onChange={(e) => setNewSchedule({ ...newSchedule, startTime: e.target.value })}
                    className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-purple-500"
                  />
                </div>
                <div>
                  <label className="block text-xs font-bold text-gray-700 uppercase mb-1">Giờ Kết Thúc *</label>
                  <input
                    type="time"
                    required
                    value={newSchedule.endTime}
                    onChange={(e) => setNewSchedule({ ...newSchedule, endTime: e.target.value })}
                    className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-purple-500"
                  />
                </div>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-bold text-gray-700 uppercase mb-1">Ngày Bắt Đầu Hiệu Lực *</label>
                  <input
                    type="date"
                    required
                    value={newSchedule.effectiveFrom}
                    onChange={(e) => setNewSchedule({ ...newSchedule, effectiveFrom: e.target.value })}
                    className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-purple-500"
                  />
                </div>
                <div>
                  <label className="block text-xs font-bold text-gray-700 uppercase mb-1">Ngày Hết Hiệu Lực</label>
                  <input
                    type="date"
                    value={newSchedule.effectiveTo}
                    onChange={(e) => setNewSchedule({ ...newSchedule, effectiveTo: e.target.value })}
                    className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-purple-500"
                  />
                </div>
              </div>

              <div className="pt-3 border-t border-gray-100 flex justify-end space-x-2">
                <button
                  type="button"
                  onClick={() => setShowScheduleModal(false)}
                  className="px-4 py-2 text-xs font-semibold text-gray-600 hover:bg-gray-100 rounded-lg"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  className="px-5 py-2 text-xs font-bold text-white bg-purple-600 hover:bg-purple-700 rounded-lg shadow-md"
                >
                  Lưu Ca Làm Việc
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export default AdminDashboardPage;
