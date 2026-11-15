import React, { useState, useEffect } from 'react';
import {
  ShieldCheck,
  CheckCircle,
  XCircle,
  Calendar,
  RefreshCw,
  AlertTriangle,
  UserCheck,
  Lock,
  LogOut,
  Search,
} from 'lucide-react';
import {
  authApi,
  receptionApi,
  AuthUser,
  ReceptionAppointmentItem,
  RescheduleRequestItem,
} from '../api/client';

export const ReceptionistDashboardPage: React.FC = () => {
  // 1. Quản lý phiên đăng nhập và phân quyền (Role checks: RECEPTIONIST hoặc ADMIN)
  const [currentUser, setCurrentUser] = useState<AuthUser | null>(authApi.getStoredUser());
  const [loginEmail, setLoginEmail] = useState<string>('admin@clinic.vn');
  const [loginPassword, setLoginPassword] = useState<string>('Admin@Clinic2026!');
  const [loginLoading, setLoginLoading] = useState<boolean>(false);
  const [loginError, setLoginError] = useState<string>('');

  // 2. Dữ liệu Lễ tân
  const [appointments, setAppointments] = useState<ReceptionAppointmentItem[]>([]);
  const [reschedules, setReschedules] = useState<RescheduleRequestItem[]>([]);
  const [loadingData, setLoadingData] = useState<boolean>(false);

  // 3. Bộ lọc hàng đợi
  const [filterDate, setFilterDate] = useState<string>(
    new Date(Date.now() + 7 * 3600000).toISOString().split('T')[0]
  );
  const [filterStatus, setFilterStatus] = useState<string>('');
  const [searchKeyword, setSearchKeyword] = useState<string>('');

  // 4. Trạng thái thông báo
  const [errorMessage, setErrorMessage] = useState<string>('');
  const [successMessage, setSuccessMessage] = useState<string>('');

  // 5. Modal từ chối đổi lịch
  const [rejectModalOpen, setRejectModalOpen] = useState<boolean>(false);
  const [selectedRejectRequestId, setSelectedRejectRequestId] = useState<string>('');
  const [rejectReason, setRejectReason] = useState<string>('');
  const [actionLoading, setActionLoading] = useState<boolean>(false);

  // Kiểm tra quyền hạn: Phải có vai trò RECEPTIONIST hoặc ADMIN
  const isAuthorized =
    currentUser &&
    (currentUser.roles.includes('RECEPTIONIST') ||
      currentUser.roles.includes('ADMIN') ||
      currentUser.roles.includes('Staff'));

  // Load dữ liệu khi đã xác thực
  const loadDashboardData = async () => {
    if (!isAuthorized) return;
    setLoadingData(true);
    setErrorMessage('');
    try {
      const [queueData, reschData] = await Promise.all([
        receptionApi.getQueue({
          date: filterDate || undefined,
          status: filterStatus || undefined,
          search: searchKeyword.trim() || undefined,
        }),
        receptionApi.getPendingReschedules(),
      ]);
      setAppointments(queueData);
      setReschedules(reschData);
    } catch (err: any) {
      if (err.response?.status === 401 || err.response?.status === 403) {
        setErrorMessage('Phiên đăng nhập đã hết hạn hoặc bạn không có quyền truy cập kênh Lễ tân.');
        authApi.logout();
        setCurrentUser(null);
      } else {
        setErrorMessage(err.message || 'Không thể tải dữ liệu bàn tiếp nhận.');
      }
    } finally {
      setLoadingData(false);
    }
  };

  useEffect(() => {
    if (isAuthorized) {
      loadDashboardData();
    }
  }, [isAuthorized, filterDate, filterStatus]);

  // Xử lý Đăng nhập
  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoginError('');
    setLoginLoading(true);
    try {
      const res = await authApi.login(loginEmail, loginPassword);
      setCurrentUser(res.user);
      setSuccessMessage('Đăng nhập thành công kênh Nghiệp vụ Lễ tân!');
    } catch (err: any) {
      setLoginError(err.message || 'Email hoặc mật khẩu không chính xác.');
    } finally {
      setLoginLoading(false);
    }
  };

  // Xử lý Đăng xuất
  const handleLogout = () => {
    authApi.logout();
    setCurrentUser(null);
    setAppointments([]);
    setReschedules([]);
    setSuccessMessage('');
    setErrorMessage('');
  };

  // Thao tác 1: Xác nhận lịch hẹn (PendingConfirmation -> Confirmed)
  const handleConfirmAppointment = async (id: string) => {
    setErrorMessage('');
    setSuccessMessage('');
    setActionLoading(true);
    try {
      await receptionApi.confirmAppointment(id, 'Lễ tân đã kiểm tra và xác nhận lịch hẹn.');
      setSuccessMessage('Đã xác nhận lịch hẹn thành công (Trạng thái: Confirmed)!');
      loadDashboardData();
    } catch (err: any) {
      setErrorMessage(err.message || 'Không thể xác nhận lịch hẹn.');
    } finally {
      setActionLoading(false);
    }
  };

  // Thao tác 2: Tiếp đón bệnh nhân Check-in tại quầy (Confirmed -> CheckedIn)
  const handleCheckIn = async (id: string) => {
    setErrorMessage('');
    setSuccessMessage('');
    setActionLoading(true);
    try {
      await receptionApi.checkInAppointment(id, 'Bệnh nhân đã có mặt tại quầy và hoàn tất thủ tục.');
      setSuccessMessage('Tiếp nhận bệnh nhân thành công! Ca khám đã chuyển sang phòng chờ (CheckedIn).');
      loadDashboardData();
    } catch (err: any) {
      setErrorMessage(err.message || 'Không thể làm thủ tục check-in.');
    } finally {
      setActionLoading(false);
    }
  };

  // Thao tác 3: Duyệt đổi lịch (Approve Reschedule)
  const handleApproveReschedule = async (requestId: string) => {
    setErrorMessage('');
    setSuccessMessage('');
    setActionLoading(true);
    try {
      await receptionApi.approveReschedule(requestId, 'Lễ tân phê duyệt đổi lịch theo yêu cầu.');
      setSuccessMessage('Phê duyệt đổi lịch thành công! Khung giờ mới đã được khóa và lịch cũ được chuyển.');
      loadDashboardData();
    } catch (err: any) {
      setErrorMessage(err.message || 'Không thể phê duyệt đổi lịch.');
    } finally {
      setActionLoading(false);
    }
  };

  // Thao tác 4: Từ chối đổi lịch (Reject Reschedule)
  const openRejectModal = (requestId: string) => {
    setSelectedRejectRequestId(requestId);
    setRejectReason('');
    setRejectModalOpen(true);
  };

  const handleConfirmReject = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!rejectReason.trim()) {
      setErrorMessage('Vui lòng nhập lý do từ chối yêu cầu đổi lịch.');
      return;
    }

    setErrorMessage('');
    setSuccessMessage('');
    setActionLoading(true);
    try {
      await receptionApi.rejectReschedule(selectedRejectRequestId, rejectReason.trim());
      setSuccessMessage('Đã từ chối yêu cầu đổi lịch. Lịch hẹn ban đầu của bệnh nhân vẫn được giữ nguyên 100%!');
      setRejectModalOpen(false);
      loadDashboardData();
    } catch (err: any) {
      setErrorMessage(err.message || 'Không thể từ chối yêu cầu đổi lịch.');
    } finally {
      setActionLoading(false);
    }
  };

  // Định dạng ngày giờ hiển thị
  const formatDateTimeVn = (utcString: string) => {
    if (!utcString) return '--:--';
    const date = new Date(utcString);
    const vn = new Date(date.getTime() + 7 * 3600000);
    return `${vn.toISOString().substring(11, 16)} (${vn.getDate().toString().padStart(2, '0')}/${(vn.getMonth() + 1).toString().padStart(2, '0')}/${vn.getFullYear()})`;
  };

  // MÀN HÌNH ĐĂNG NHẬP (Nếu chưa có quyền / chưa login)
  if (!isAuthorized) {
    return (
      <div className="max-w-md mx-auto my-16 px-4">
        <div className="bg-white rounded-3xl border border-gray-200 shadow-xl p-8 space-y-6">
          <div className="text-center space-y-2">
            <div className="w-14 h-14 bg-teal-100 text-teal-700 rounded-2xl flex items-center justify-center mx-auto shadow-inner">
              <Lock className="w-7 h-7" />
            </div>
            <h1 className="text-2xl font-black text-gray-900">Kênh Nghiệp Vụ Lễ Tân</h1>
            <p className="text-xs text-gray-500">
              Khu vực bảo mật dành cho Nhân viên Lễ tân và Quản trị viên phòng khám (Role-aware check).
            </p>
          </div>

          {loginError && (
            <div className="p-3 bg-rose-50 border border-rose-200 text-rose-700 rounded-xl text-xs flex items-center">
              <AlertTriangle className="w-4 h-4 mr-2 flex-shrink-0" />
              <span>{loginError}</span>
            </div>
          )}

          <form onSubmit={handleLogin} className="space-y-4">
            <div>
              <label className="block text-xs font-bold text-gray-700 mb-1">Tài khoản Email</label>
              <input
                type="email"
                value={loginEmail}
                onChange={(e) => setLoginEmail(e.target.value)}
                required
                className="w-full px-3.5 py-2.5 rounded-xl border border-gray-300 text-sm focus:ring-2 focus:ring-teal-500 focus:outline-none"
                placeholder="receptionist@clinic.vn hoặc admin@clinic.vn"
              />
            </div>

            <div>
              <label className="block text-xs font-bold text-gray-700 mb-1">Mật khẩu</label>
              <input
                type="password"
                value={loginPassword}
                onChange={(e) => setLoginPassword(e.target.value)}
                required
                className="w-full px-3.5 py-2.5 rounded-xl border border-gray-300 text-sm focus:ring-2 focus:ring-teal-500 focus:outline-none"
                placeholder="Nhập mật khẩu..."
              />
            </div>

            <button
              type="submit"
              disabled={loginLoading}
              className="w-full py-3 bg-teal-600 hover:bg-teal-700 text-white rounded-xl text-sm font-bold shadow-md hover:shadow-lg transition flex items-center justify-center disabled:opacity-50"
            >
              {loginLoading ? (
                <>
                  <RefreshCw className="w-4 h-4 mr-2 animate-spin" /> Đang xác thực...
                </>
              ) : (
                'Đăng Nhập Quầy Tiếp Đón'
              )}
            </button>
          </form>

          <div className="pt-4 border-t border-gray-100 text-[11px] text-gray-400 bg-gray-50 -mx-8 -mb-8 p-4 rounded-b-3xl text-center">
            Gợi ý tài khoản quản trị thử nghiệm: <br />
            <strong className="text-gray-700">admin@clinic.vn</strong> | Mật khẩu: <strong className="text-gray-700">Admin@Clinic2026!</strong>
          </div>
        </div>
      </div>
    );
  }

  // MÀN HÌNH BÀN TIẾP NHẬN LỄ TÂN (ĐÃ ĐĂNG NHẬP)
  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-10 space-y-8">
      {/* Header bar */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4 pb-6 border-b border-gray-200">
        <div>
          <span className="text-xs font-bold text-teal-600 uppercase tracking-wider flex items-center">
            <ShieldCheck className="w-4 h-4 mr-1 text-teal-600" /> Bàn Tiếp Nhận Lễ Tân
          </span>
          <h1 className="text-3xl font-extrabold text-gray-900 mt-1">Quản Lý Ca Khám & Đổi Lịch</h1>
          <p className="text-sm text-gray-500 mt-1">
            Xác nhận lịch đặt mới, phê duyệt yêu cầu đổi lịch và tiếp đón bệnh nhân (Check-in).
          </p>
        </div>

        <div className="flex items-center space-x-3">
          <div className="text-right text-xs">
            <div className="font-bold text-gray-900">{currentUser.email}</div>
            <div className="text-teal-600 font-semibold">{currentUser.roles.join(', ')}</div>
          </div>
          <button
            onClick={handleLogout}
            className="inline-flex items-center px-3 py-2 border border-gray-300 rounded-xl text-xs font-bold text-gray-700 bg-white hover:bg-gray-50 transition shadow-sm"
          >
            <LogOut className="w-3.5 h-3.5 mr-1 text-gray-500" /> Đăng Xuất
          </button>
        </div>
      </div>

      {/* Thông báo Alert */}
      {errorMessage && (
        <div className="p-4 bg-rose-50 border border-rose-200 text-rose-800 rounded-2xl text-xs flex items-center justify-between">
          <div className="flex items-center space-x-2">
            <AlertTriangle className="w-4 h-4 text-rose-600 flex-shrink-0" />
            <span>{errorMessage}</span>
          </div>
          <button onClick={() => setErrorMessage('')} className="text-rose-500 font-bold ml-2">✕</button>
        </div>
      )}

      {successMessage && (
        <div className="p-4 bg-emerald-50 border border-emerald-200 text-emerald-800 rounded-2xl text-xs flex items-center justify-between">
          <div className="flex items-center space-x-2">
            <CheckCircle className="w-4 h-4 text-emerald-600 flex-shrink-0" />
            <span>{successMessage}</span>
          </div>
          <button onClick={() => setSuccessMessage('')} className="text-emerald-500 font-bold ml-2">✕</button>
        </div>
      )}

      {/* PHẦN 1: YÊU CẦU ĐỔI LỊCH CHỜ DUYỆT (RESCHEDULE APPROVALS) */}
      <section className="bg-white rounded-3xl border border-gray-200 shadow-sm p-6 sm:p-8 space-y-6">
        <div className="flex items-center justify-between">
          <div className="flex items-center space-x-3">
            <div className="p-2.5 rounded-xl bg-blue-100 text-blue-700">
              <RefreshCw className="w-5 h-5" />
            </div>
            <div>
              <h2 className="text-lg font-bold text-gray-900">
                Yêu Cầu Đổi Lịch Chờ Duyệt (Reschedule Requests)
              </h2>
              <p className="text-xs text-gray-500">
                Lịch cũ được bảo lưu 100% đến khi duyệt; hệ thống tự động kiểm tra slot mới chống xung đột.
              </p>
            </div>
          </div>
          <span className="px-3 py-1 rounded-full bg-blue-100 text-blue-800 text-xs font-bold">
            {reschedules.length} yêu cầu
          </span>
        </div>

        {reschedules.length === 0 ? (
          <div className="text-center py-8 text-xs text-gray-400 italic bg-gray-50 rounded-2xl border border-dashed border-gray-200">
            Hiện không có yêu cầu đổi lịch nào đang chờ phê duyệt.
          </div>
        ) : (
          <div className="space-y-4">
            {reschedules.map((r) => (
              <div
                key={r.requestId}
                className="p-5 rounded-2xl border border-gray-200 bg-gray-50 hover:bg-blue-50/30 transition flex flex-col md:flex-row justify-between items-start md:items-center gap-4"
              >
                <div className="space-y-1.5 text-xs flex-1">
                  <div className="flex items-center space-x-2">
                    <span className="font-mono font-bold text-teal-700 bg-teal-50 px-2 py-0.5 rounded border border-teal-200">
                      {r.bookingCode}
                    </span>
                    <span className="font-bold text-gray-900 text-sm">{r.patientFullName}</span>
                    <span className="text-gray-400">({r.patientPhoneNumber})</span>
                  </div>
                  <p className="text-gray-600">
                    Dịch vụ: <strong>{r.serviceName}</strong> • Bác sĩ hiện tại: <strong>{r.currentDoctorFullName}</strong>
                  </p>
                  <p className="text-gray-600">
                    Lịch cũ: <span className="line-through text-gray-400 font-semibold">{formatDateTimeVn(r.currentStartAtUtc)}</span>{' '}
                    <span className="text-blue-700 font-bold ml-1">
                      ➔ Đề xuất mới: {formatDateTimeVn(r.proposedStartAtUtc)} (BS. {r.requestedDoctorFullName})
                    </span>
                  </p>
                  {r.reason && (
                    <p className="text-gray-500 italic bg-white p-2 rounded-lg border border-gray-200 inline-block">
                      Lý do bệnh nhân đề xuất: "{r.reason}"
                    </p>
                  )}
                </div>

                <div className="flex items-center space-x-2 flex-shrink-0">
                  <button
                    onClick={() => handleApproveReschedule(r.requestId)}
                    disabled={actionLoading}
                    className="inline-flex items-center px-4 py-2 bg-emerald-600 hover:bg-emerald-700 text-white rounded-xl text-xs font-bold transition shadow-sm disabled:opacity-50"
                  >
                    <CheckCircle className="w-4 h-4 mr-1.5" /> Duyệt Đổi Lịch
                  </button>
                  <button
                    onClick={() => openRejectModal(r.requestId)}
                    disabled={actionLoading}
                    className="inline-flex items-center px-4 py-2 bg-rose-600 hover:bg-rose-700 text-white rounded-xl text-xs font-bold transition shadow-sm disabled:opacity-50"
                  >
                    <XCircle className="w-4 h-4 mr-1.5" /> Từ Chối
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </section>

      {/* PHẦN 2: HÀNG ĐỢI LỊCH KHÁM & TIẾP ĐÓN CHECK-IN */}
      <section className="bg-white rounded-3xl border border-gray-200 shadow-sm p-6 sm:p-8 space-y-6">
        <div className="flex flex-col md:flex-row justify-between items-start md:items-center gap-4">
          <div className="flex items-center space-x-3">
            <div className="p-2.5 rounded-xl bg-teal-100 text-teal-700">
              <Calendar className="w-5 h-5" />
            </div>
            <div>
              <h2 className="text-lg font-bold text-gray-900">Danh Sách Ca Khám & Hàng Đợi Quầy</h2>
              <p className="text-xs text-gray-500">
                Xác nhận lịch đặt mới (Confirm) và tiếp đón bệnh nhân khi có mặt (Check-in).
              </p>
            </div>
          </div>

          <button
            onClick={loadDashboardData}
            disabled={loadingData}
            className="inline-flex items-center px-3.5 py-1.5 bg-gray-100 hover:bg-gray-200 text-gray-700 rounded-xl text-xs font-bold transition"
          >
            <RefreshCw className={`w-3.5 h-3.5 mr-1.5 ${loadingData ? 'animate-spin' : ''}`} /> Tải lại
          </button>
        </div>

        {/* Thanh tìm kiếm & Bộ lọc */}
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-3 bg-gray-50 p-4 rounded-2xl border border-gray-200">
          <div>
            <label className="block text-[11px] font-bold text-gray-500 mb-1">Ngày khám</label>
            <input
              type="date"
              value={filterDate}
              onChange={(e) => setFilterDate(e.target.value)}
              className="w-full px-3 py-1.5 rounded-xl border border-gray-300 text-xs focus:ring-2 focus:ring-teal-500"
            />
          </div>

          <div>
            <label className="block text-[11px] font-bold text-gray-500 mb-1">Trạng thái</label>
            <select
              value={filterStatus}
              onChange={(e) => setFilterStatus(e.target.value)}
              className="w-full px-3 py-1.5 rounded-xl border border-gray-300 text-xs focus:ring-2 focus:ring-teal-500"
            >
              <option value="">Tất cả trạng thái</option>
              <option value="PendingConfirmation">Chờ Xác Nhận</option>
              <option value="Confirmed">Đã Xác Nhận</option>
              <option value="CheckedIn">Đã Check-in</option>
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
                value={searchKeyword}
                onChange={(e) => setSearchKeyword(e.target.value)}
                onKeyDown={(e) => e.key === 'Enter' && loadDashboardData()}
                placeholder="Tên, số điện thoại, mã booking..."
                className="w-full pl-8 pr-3 py-1.5 rounded-xl border border-gray-300 text-xs focus:ring-2 focus:ring-teal-500"
              />
              <Search className="w-3.5 h-3.5 text-gray-400 absolute left-2.5 top-2.5" />
            </div>
          </div>
        </div>

        {/* Bảng danh sách ca khám */}
        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs border-collapse">
            <thead>
              <tr className="border-b border-gray-200 text-gray-500 font-semibold uppercase tracking-wider bg-gray-50/50">
                <th className="py-3 px-4">STT</th>
                <th className="py-3 px-4">Mã / Bệnh Nhân</th>
                <th className="py-3 px-4">Liên Hệ</th>
                <th className="py-3 px-4">Bác Sĩ / Phòng</th>
                <th className="py-3 px-4">Dịch Vụ / Giá</th>
                <th className="py-3 px-4">Giờ Khám (VN)</th>
                <th className="py-3 px-4">Trạng Thái</th>
                <th className="py-3 px-4 text-right">Thao Tác</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {appointments.length === 0 ? (
                <tr>
                  <td colSpan={8} className="py-8 text-center text-xs text-gray-400 italic">
                    Không tìm thấy ca khám nào phù hợp với bộ lọc.
                  </td>
                </tr>
              ) : (
                appointments.map((apt, idx) => (
                  <tr key={apt.id} className="hover:bg-gray-50 transition">
                    <td className="py-3.5 px-4 font-mono font-bold text-gray-400">
                      #{apt.queueNumber || idx + 1}
                    </td>

                    <td className="py-3.5 px-4">
                      <span className="font-mono font-bold text-teal-700 block">{apt.bookingCode}</span>
                      <span className="font-bold text-gray-900">{apt.patientName}</span>
                      {apt.isDuplicateCandidate && (
                        <span className="inline-flex items-center text-[10px] bg-amber-100 text-amber-800 px-1.5 py-0.5 rounded ml-1 font-medium">
                          <AlertTriangle className="w-3 h-3 mr-0.5" /> Nghi trùng hồ sơ
                        </span>
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
                      <div className="text-teal-700 font-bold">{apt.servicePrice?.toLocaleString('vi-VN')} VND</div>
                    </td>

                    <td className="py-3.5 px-4 font-semibold text-gray-800">
                      {formatDateTimeVn(apt.startAt)}
                    </td>

                    <td className="py-3.5 px-4">
                      <span
                        className={`px-2.5 py-1 rounded-full text-[11px] font-bold inline-block ${apt.status === 'Confirmed'
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
                          disabled={actionLoading}
                          className="px-3 py-1.5 bg-teal-600 hover:bg-teal-700 text-white rounded-lg font-bold text-xs transition shadow-sm disabled:opacity-50"
                        >
                          Xác Nhận Lịch
                        </button>
                      )}

                      {apt.status === 'Confirmed' && (
                        <button
                          onClick={() => handleCheckIn(apt.id)}
                          disabled={actionLoading}
                          className="inline-flex items-center px-3 py-1.5 bg-emerald-600 hover:bg-emerald-700 text-white rounded-lg font-bold text-xs transition shadow-sm disabled:opacity-50"
                        >
                          <UserCheck className="w-3.5 h-3.5 mr-1" /> Check-in
                        </button>
                      )}

                      {apt.status === 'CheckedIn' && (
                        <span className="text-xs text-emerald-600 font-semibold flex items-center justify-end">
                          <CheckCircle className="w-3.5 h-3.5 mr-1" /> Trong phòng chờ
                        </span>
                      )}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </section>

      {/* MODAL TỪ CHỐI ĐỔI LỊCH (REJECT MODAL) */}
      {rejectModalOpen && (
        <div className="fixed inset-0 z-50 bg-black/50 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="bg-white rounded-3xl max-w-md w-full p-6 space-y-4 shadow-2xl animate-in fade-in zoom-in-95">
            <div className="flex items-center space-x-3 text-rose-600">
              <div className="p-2 bg-rose-100 rounded-xl">
                <XCircle className="w-6 h-6" />
              </div>
              <h3 className="text-lg font-bold text-gray-900">Từ Chối Yêu Cầu Đổi Lịch</h3>
            </div>

            <p className="text-xs text-gray-500">
              Khi từ chối yêu cầu, lịch khám ban đầu của bệnh nhân sẽ tiếp tục được giữ nguyên 100%. Vui lòng nhập lý do từ chối để thông báo tới bệnh nhân:
            </p>

            <form onSubmit={handleConfirmReject} className="space-y-4">
              <div>
                <textarea
                  value={rejectReason}
                  onChange={(e) => setRejectReason(e.target.value)}
                  required
                  rows={3}
                  placeholder="Ví dụ: Bác sĩ bận ca phẫu thuật đột xuất, vui lòng chọn khung giờ khác..."
                  className="w-full px-3 py-2 rounded-xl border border-gray-300 text-xs focus:ring-2 focus:ring-rose-500 focus:outline-none"
                />
              </div>

              <div className="flex justify-end space-x-2">
                <button
                  type="button"
                  onClick={() => setRejectModalOpen(false)}
                  className="px-4 py-2 border border-gray-300 rounded-xl text-xs font-bold text-gray-700 hover:bg-gray-50"
                >
                  Hủy Bỏ
                </button>
                <button
                  type="submit"
                  disabled={actionLoading}
                  className="px-4 py-2 bg-rose-600 hover:bg-rose-700 text-white rounded-xl text-xs font-bold transition disabled:opacity-50"
                >
                  {actionLoading ? 'Đang xử lý...' : 'Xác Nhận Từ Chối'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};
