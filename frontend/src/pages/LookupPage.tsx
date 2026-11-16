import React, { useState, useEffect } from 'react';
import { useSearchParams } from 'react-router-dom';
import { appointmentApi, catalogApi, paymentApi } from '../api/client';
import { AppointmentDetail, AvailableSlot, RefundPreview, CreatePaymentLinkResponse } from '../types';
import {
  Search,
  KeyRound,
  AlertCircle,
  CheckCircle,
  RefreshCw,
  XCircle,
  AlertTriangle,
  CreditCard,
  ExternalLink,
} from 'lucide-react';

export const LookupPage: React.FC = () => {
  const [searchParams] = useSearchParams();

  // Search form
  const [bookingCode, setBookingCode] = useState<string>(searchParams.get('code') || '');
  const [phone, setPhone] = useState<string>('');
  const [challengeId, setChallengeId] = useState<string>('');
  const [otpCode, setOtpCode] = useState<string>('');

  // States: 'input' -> 'otp_sent' -> 'verified'
  const [authStep, setAuthStep] = useState<'input' | 'otp_sent' | 'verified'>('input');
  const [loading, setLoading] = useState<boolean>(false);
  const [errorMessage, setErrorMessage] = useState<string>('');
  const [successMessage, setSuccessMessage] = useState<string>('');
  const [cooldownSeconds, setCooldownSeconds] = useState<number>(0);

  // Appointment Data
  const [appointment, setAppointment] = useState<AppointmentDetail | null>(null);

  // Action Modals: Reschedule & Cancel
  const [showRescheduleModal, setShowRescheduleModal] = useState<boolean>(false);
  const [showCancelModal, setShowCancelModal] = useState<boolean>(false);

  // Reschedule state
  const [newDate, setNewDate] = useState<string>(
    new Date(Date.now() + 86400000 * 2).toISOString().split('T')[0]
  );
  const [availableSlots, setAvailableSlots] = useState<AvailableSlot[]>([]);
  const [selectedNewSlot, setSelectedNewSlot] = useState<string>('');
  const [rescheduleReason, setRescheduleReason] = useState<string>('');

  // Cancellation state
  const [refundPreview, setRefundPreview] = useState<RefundPreview | null>(null);
  const [cancelReason, setCancelReason] = useState<string>('');
  const [cancelledSuccess, setCancelledSuccess] = useState<boolean>(false);

  // PayOS Payment Modal state
  const [showPaymentModal, setShowPaymentModal] = useState<boolean>(false);
  const [paymentLink, setPaymentLink] = useState<CreatePaymentLinkResponse | null>(null);
  const [loadingPayment, setLoadingPayment] = useState<boolean>(false);
  const [checkingPayment, setCheckingPayment] = useState<boolean>(false);

  const handleOpenPayment = async (appointmentId: string) => {
    setShowPaymentModal(true);
    if (!paymentLink) {
      try {
        setLoadingPayment(true);
        const res = await paymentApi.createPayOSLink(
          appointmentId,
          appointment?.servicePrice,
          appointment?.bookingCode
        );
        setPaymentLink(res);
      } catch (err: any) {
        alert(err.message || 'Lỗi tạo liên kết thanh toán');
      } finally {
        setLoadingPayment(false);
      }
    }
  };

  const handleCheckPaymentStatus = async (appointmentId: string) => {
    try {
      setCheckingPayment(true);
      const status = await paymentApi.getPaymentStatus(appointmentId);
      if (status.isPaid) {
        alert('Giao dịch đã được ghi nhận thành công!');
        setAppointment((prev) => prev ? { ...prev, paidAmount: status.paidAmount, status: 'Confirmed' } : null);
        setShowPaymentModal(false);
      } else {
        alert('Hệ thống chưa ghi nhận tiền từ giao dịch chuyển khoản. Vui lòng thử lại sau vài giây.');
      }
    } catch (err: any) {
      console.error(err);
    } finally {
      setCheckingPayment(false);
    }
  };

  const handleMockConfirm = async (appointmentId: string) => {
    try {
      setCheckingPayment(true);
      const status = await paymentApi.mockConfirmPayment(appointmentId);
      setAppointment((prev) => prev ? { ...prev, paidAmount: status.paidAmount, status: 'Confirmed' } : null);
      setShowPaymentModal(false);
      alert('Mô phỏng thanh toán VietQR thành công!');
    } catch (err: any) {
      alert(err.message);
    } finally {
      setCheckingPayment(false);
    }
  };

  // Cooldown timer
  useEffect(() => {
    if (cooldownSeconds > 0) {
      const timer = setTimeout(() => setCooldownSeconds(cooldownSeconds - 1), 1000);
      return () => clearTimeout(timer);
    }
  }, [cooldownSeconds]);

  // Request OTP
  const handleRequestOtp = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMessage('');
    if (!bookingCode.trim() || !phone.trim()) {
      setErrorMessage('Vui lòng nhập Mã đặt lịch và Số điện thoại đã đăng ký.');
      return;
    }

    setLoading(true);
    try {
      const result = await appointmentApi.requestOtp(
        bookingCode.trim().toUpperCase(),
        phone.trim(),
        'LOOKUP'
      );
      setChallengeId(result.challengeId);
      setSuccessMessage(result.message);
      setAuthStep('otp_sent');
      setCooldownSeconds(60);
    } catch (err: any) {
      setErrorMessage(err.message || 'Không thể gửi mã OTP.');
    } finally {
      setLoading(false);
    }
  };

  // Verify OTP
  const handleVerifyOtp = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMessage('');
    if (!otpCode.trim()) {
      setErrorMessage('Vui lòng nhập mã OTP gồm 6 chữ số.');
      return;
    }

    setLoading(true);
    try {
      const detail = await appointmentApi.verifyOtpAndGetDetails(
        bookingCode.trim().toUpperCase(),
        otpCode.trim(),
        challengeId
      );
      setAppointment(detail);
      setAuthStep('verified');
      setSuccessMessage('Xác thực danh tính thành công!');
    } catch (err: any) {
      setErrorMessage(err.message || 'Mã OTP không chính xác hoặc đã hết hạn.');
    } finally {
      setLoading(false);
    }
  };

  // Open Reschedule Modal
  const handleOpenReschedule = async () => {
    setShowRescheduleModal(true);
    const slots = await catalogApi.getAvailableSlots('doc-1', newDate);
    setAvailableSlots(slots);
    const firstFree = slots.find((s) => s.isAvailable);
    if (firstFree) setSelectedNewSlot(firstFree.startTime);
  };

  // Submit Reschedule Request
  const handleSubmitReschedule = (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedNewSlot) {
      alert('Vui lòng chọn khung giờ mới.');
      return;
    }
    // Business invariant: Reschedule is a request waiting for Receptionist approval
    setTimeout(() => {
      alert(
        'Yêu cầu đổi lịch của bạn đã được gửi tới bộ phận Lễ tân (Trạng thái: Chờ duyệt). Lịch hẹn hiện tại của bạn vẫn được giữ nguyên cho tới khi yêu cầu được duyệt thành công.'
      );
      setShowRescheduleModal(false);
    }, 1000);
  };

  // Open Cancellation Modal with 60-Minute Refund Calculation
  const handleOpenCancel = () => {
    if (!appointment) return;
    const [y, m, d] = appointment.appointmentDate.split('-').map(Number);
    const [h, min] = appointment.startTime.split(':').map(Number);
    const aptTime = new Date(Date.UTC(y, m - 1, d, h - 7, min)); // convert to UTC assuming ICT+7

    const nowUtc = new Date();
    const preview = appointmentApi.calculateRefundPreview(aptTime, nowUtc, appointment.paidAmount);
    setRefundPreview(preview);
    setShowCancelModal(true);
  };

  // Confirm Cancellation
  const handleConfirmCancel = () => {
    if (!appointment) return;
    // Patient can cancel directly before appointment start without receptionist approval
    setAppointment({
      ...appointment,
      status: 'Cancelled',
    });
    setCancelledSuccess(true);
    setShowCancelModal(false);
  };

  return (
    <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-10 space-y-8">
      {/* Title */}
      <div>
        <h1 className="text-3xl font-extrabold text-gray-900">Tra Cứu & Quản Lý Lịch Hẹn</h1>
        <p className="text-gray-600 mt-2 text-sm">
          Nhập mã đặt lịch (Booking Code) và xác thực số điện thoại qua mã OTP để xem thông tin, yêu cầu đổi lịch hoặc hủy lịch.
        </p>
      </div>

      {/* Messages */}
      {errorMessage && (
        <div className="p-4 bg-red-50 border-l-4 border-red-500 rounded-xl flex items-start space-x-3 text-red-800 text-sm">
          <AlertCircle className="w-5 h-5 flex-shrink-0 text-red-500 mt-0.5" />
          <div>{errorMessage}</div>
        </div>
      )}
      {successMessage && (
        <div className="p-4 bg-emerald-50 border-l-4 border-emerald-500 rounded-xl flex items-start space-x-3 text-emerald-800 text-sm">
          <CheckCircle className="w-5 h-5 flex-shrink-0 text-emerald-500 mt-0.5" />
          <div>{successMessage}</div>
        </div>
      )}

      {/* AUTH STEP 1: INPUT BOOKING CODE & PHONE */}
      {authStep === 'input' && (
        <div className="bg-white p-6 sm:p-8 rounded-3xl border border-gray-200 shadow-sm max-w-lg mx-auto space-y-6">
          <div className="flex items-center space-x-3 text-teal-700">
            <Search className="w-6 h-6" />
            <h2 className="text-lg font-bold text-gray-900">Tra Cứu Lịch Hẹn Bằng OTP</h2>
          </div>

          <form onSubmit={handleRequestOtp} className="space-y-4">
            <div>
              <label className="block text-xs font-bold text-gray-700 mb-1">
                Mã Đặt Lịch (Booking Code) <span className="text-red-500">*</span>
              </label>
              <input
                type="text"
                required
                placeholder="Ví dụ: BK123456"
                value={bookingCode}
                onChange={(e) => setBookingCode(e.target.value.toUpperCase())}
                className="w-full px-4 py-2.5 border border-gray-300 rounded-xl text-sm font-mono tracking-wider focus:outline-none focus:ring-2 focus:ring-teal-500 uppercase"
              />
            </div>

            <div>
              <label className="block text-xs font-bold text-gray-700 mb-1">
                Số Điện Thoại Khi Đặt Lịch <span className="text-red-500">*</span>
              </label>
              <input
                type="tel"
                required
                placeholder="Ví dụ: 0912345678"
                value={phone}
                onChange={(e) => setPhone(e.target.value)}
                className="w-full px-4 py-2.5 border border-gray-300 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-teal-500"
              />
            </div>

            <div className="pt-2">
              <button
                type="submit"
                disabled={loading}
                className="w-full inline-flex items-center justify-center px-4 py-3 bg-teal-600 hover:bg-teal-700 text-white rounded-xl text-sm font-bold shadow-sm transition disabled:bg-gray-400"
              >
                {loading ? 'Đang gửi mã...' : 'Gửi Mã Xác Thực OTP'}
              </button>
            </div>
          </form>

          <p className="text-xs text-gray-500 text-center">
            Mã OTP 6 số sẽ được gửi tới số điện thoại bạn đã đăng ký để bảo vệ quyền riêng tư hồ sơ y tế.
          </p>
        </div>
      )}

      {/* AUTH STEP 2: ENTER OTP CHALLENGE */}
      {authStep === 'otp_sent' && (
        <div className="bg-white p-6 sm:p-8 rounded-3xl border border-gray-200 shadow-sm max-w-lg mx-auto space-y-6">
          <div className="flex items-center space-x-3 text-teal-700">
            <KeyRound className="w-6 h-6" />
            <h2 className="text-lg font-bold text-gray-900">Nhập Mã OTP Xác Minh</h2>
          </div>

          <p className="text-xs text-gray-600">
            Mã xác thực đã được gửi tới số điện thoại gắn với lịch hẹn <strong>{bookingCode}</strong>.
            Mã có hiệu lực trong vòng 5 phút (tối đa 5 lần thử).
          </p>

          <form onSubmit={handleVerifyOtp} className="space-y-4">
            <div>
              <label className="block text-xs font-bold text-gray-700 mb-1">
                Mã OTP (6 chữ số) <span className="text-red-500">*</span>
              </label>
              <input
                type="text"
                required
                maxLength={6}
                placeholder="Nhập 6 số OTP (Demo: 123456)"
                value={otpCode}
                onChange={(e) => setOtpCode(e.target.value)}
                className="w-full px-4 py-3 text-center border border-gray-300 rounded-xl text-lg font-mono tracking-widest focus:outline-none focus:ring-2 focus:ring-teal-500"
              />
            </div>

            <button
              type="submit"
              disabled={loading}
              className="w-full inline-flex items-center justify-center px-4 py-3 bg-teal-600 hover:bg-teal-700 text-white rounded-xl text-sm font-bold shadow-sm transition disabled:bg-gray-400"
            >
              {loading ? 'Đang kiểm tra...' : 'Xác Nhận & Xem Lịch Hẹn'}
            </button>
          </form>

          <div className="flex justify-between items-center text-xs pt-2 border-t border-gray-100">
            <button
              type="button"
              onClick={() => setAuthStep('input')}
              className="text-gray-500 hover:text-gray-700 underline"
            >
              Quay lại nhập lại số
            </button>

            <button
              type="button"
              disabled={cooldownSeconds > 0}
              onClick={handleRequestOtp}
              className="text-teal-600 hover:text-teal-800 disabled:text-gray-400 font-semibold"
            >
              {cooldownSeconds > 0 ? `Gửi lại sau ${cooldownSeconds}s` : 'Gửi lại mã OTP'}
            </button>
          </div>
        </div>
      )}

      {/* STEP 3: VERIFIED APPOINTMENT DETAILS */}
      {authStep === 'verified' && appointment && (
        <div className="space-y-6">
          {cancelledSuccess && (
            <div className="p-4 bg-amber-50 border-l-4 border-amber-500 rounded-xl text-sm text-amber-800">
              Lịch hẹn đã được hủy thành công theo đúng chính sách hoàn tiền.
            </div>
          )}

          {/* Appointment Card */}
          <div className="bg-white rounded-3xl border border-gray-200 shadow-sm overflow-hidden">
            <div className="bg-teal-800 text-white p-6 sm:p-8 flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
              <div>
                <span className="text-xs uppercase font-bold text-teal-300 tracking-wider">
                  Chi Tiết Lịch Khám Bệnh
                </span>
                <h2 className="text-2xl font-black mt-1">Mã Lịch: {appointment.bookingCode}</h2>
                <p className="text-xs text-teal-100 mt-1">
                  Bệnh nhân: <strong>{appointment.patientFullName}</strong> (SĐT:{' '}
                  {appointment.contactPhoneMasked})
                </p>
              </div>

              <div>
                <span
                  className={`px-3 py-1.5 rounded-full text-xs font-bold inline-block ${appointment.status === 'Confirmed'
                      ? 'bg-emerald-500 text-white'
                      : appointment.status === 'Cancelled'
                        ? 'bg-rose-500 text-white'
                        : 'bg-amber-400 text-amber-950'
                    }`}
                >
                  {appointment.status === 'Confirmed' && 'Đã Xác Nhận (Confirmed)'}
                  {appointment.status === 'PendingConfirmation' && 'Chờ Xác Nhận (PendingConfirmation)'}
                  {appointment.status === 'Cancelled' && 'Đã Hủy (Cancelled)'}
                  {appointment.status === 'CheckedIn' && 'Đã Tiếp Nhận (CheckedIn)'}
                  {appointment.status === 'Completed' && 'Đã Hoàn Thành (Completed)'}
                </span>
              </div>
            </div>

            <div className="p-6 sm:p-8 grid grid-cols-1 md:grid-cols-2 gap-6 text-sm">
              <div className="space-y-4">
                <h3 className="font-bold text-gray-900 text-base border-b pb-2">Thông Tin Chuyên Khoa</h3>
                <div className="space-y-2 text-xs">
                  <div className="flex justify-between py-1">
                    <span className="text-gray-500">Chuyên khoa:</span>
                    <span className="font-bold text-gray-800">{appointment.specialtyName}</span>
                  </div>
                  <div className="flex justify-between py-1">
                    <span className="text-gray-500">Bác sĩ phụ trách:</span>
                    <span className="font-bold text-gray-800">{appointment.doctorName}</span>
                  </div>
                  <div className="flex justify-between py-1">
                    <span className="text-gray-500">Dịch vụ khám:</span>
                    <span className="font-bold text-gray-800">{appointment.serviceName}</span>
                  </div>
                  <div className="flex justify-between py-1">
                    <span className="text-gray-500">Chi phí dịch vụ:</span>
                    <span className="font-extrabold text-teal-700">
                      {new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(
                        appointment.paidAmount
                      )}
                    </span>
                  </div>
                </div>
              </div>

              <div className="space-y-4">
                <h3 className="font-bold text-gray-900 text-base border-b pb-2">Thời Gian & Ghi Chú</h3>
                <div className="space-y-2 text-xs">
                  <div className="flex justify-between py-1">
                    <span className="text-gray-500">Ngày hẹn khám:</span>
                    <span className="font-bold text-teal-700">{appointment.appointmentDate}</span>
                  </div>
                  <div className="flex justify-between py-1">
                    <span className="text-gray-500">Khung giờ:</span>
                    <span className="font-bold text-teal-700">
                      {appointment.startTime} - {appointment.endTime}
                    </span>
                  </div>
                  <div className="flex justify-between py-1">
                    <span className="text-gray-500">Lý do/Triệu chứng:</span>
                    <span className="text-gray-700">{appointment.notes || 'Không ghi chú'}</span>
                  </div>
                </div>
              </div>
            </div>

            {/* Action Bar */}
            {appointment.status !== 'Cancelled' && appointment.status !== 'Completed' && (
              <div className="p-6 bg-gray-50 border-t border-gray-200 flex flex-wrap gap-4 justify-end">
                {appointment.paidAmount === 0 && (
                  <button
                    type="button"
                    onClick={() => handleOpenPayment(appointment.id)}
                    className="inline-flex items-center px-4 py-2.5 bg-emerald-600 hover:bg-emerald-700 text-white rounded-xl text-xs font-bold transition shadow-sm"
                  >
                    <CreditCard className="w-4 h-4 mr-2" />
                    Thanh Toán VietQR Ngay
                  </button>
                )}

                <button
                  type="button"
                  onClick={handleOpenReschedule}
                  className="inline-flex items-center px-4 py-2.5 bg-blue-600 hover:bg-blue-700 text-white rounded-xl text-xs font-bold transition shadow-sm"
                >
                  <RefreshCw className="w-4 h-4 mr-2" />
                  Yêu Cầu Đổi Lịch Khám (Cần Lễ Tân Duyệt)
                </button>

                <button
                  type="button"
                  onClick={handleOpenCancel}
                  className="inline-flex items-center px-4 py-2.5 bg-rose-600 hover:bg-rose-700 text-white rounded-xl text-xs font-bold transition shadow-sm"
                >
                  <XCircle className="w-4 h-4 mr-2" />
                  Hủy Lịch Khám & Hoàn Phí
                </button>
              </div>
            )}
          </div>
        </div>
      )}

      {/* MODAL: RESCHEDULE REQUEST */}
      {showRescheduleModal && (
        <div className="fixed inset-0 z-50 bg-black/60 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="bg-white rounded-3xl max-w-lg w-full p-6 sm:p-8 space-y-6 shadow-2xl">
            <div>
              <h3 className="text-xl font-bold text-gray-900">Yêu Cầu Đổi Lịch Khám</h3>
              <p className="text-xs text-gray-500 mt-1">
                Lịch cũ vẫn được giữ nguyên và bảo lưu chỗ khám cho tới khi nhân viên Lễ tân phê duyệt thành công.
              </p>
            </div>

            <form onSubmit={handleSubmitReschedule} className="space-y-4">
              <div>
                <label className="block text-xs font-bold text-gray-700 mb-1">Chọn ngày mới</label>
                <input
                  type="date"
                  value={newDate}
                  min={new Date().toISOString().split('T')[0]}
                  onChange={(e) => setNewDate(e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-xl text-sm"
                />
              </div>

              <div>
                <label className="block text-xs font-bold text-gray-700 mb-1">
                  Chọn khung giờ trống
                </label>
                <div className="grid grid-cols-3 gap-2">
                  {availableSlots.map((slot) => (
                    <button
                      key={slot.startTime}
                      type="button"
                      disabled={!slot.isAvailable}
                      onClick={() => setSelectedNewSlot(slot.startTime)}
                      className={`py-2 px-2 rounded-xl text-xs font-semibold border ${!slot.isAvailable
                          ? 'bg-gray-100 text-gray-400 line-through'
                          : selectedNewSlot === slot.startTime
                            ? 'bg-blue-600 text-white border-blue-600'
                            : 'bg-white text-gray-700 border-gray-300'
                        }`}
                    >
                      {slot.startTime}
                    </button>
                  ))}
                </div>
              </div>

              <div>
                <label className="block text-xs font-bold text-gray-700 mb-1">Lý do xin đổi lịch</label>
                <textarea
                  rows={2}
                  placeholder="Ví dụ: Bận đột xuất..."
                  value={rescheduleReason}
                  onChange={(e) => setRescheduleReason(e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-xl text-xs"
                />
              </div>

              <div className="flex justify-end space-x-3 pt-3">
                <button
                  type="button"
                  onClick={() => setShowRescheduleModal(false)}
                  className="px-4 py-2 border rounded-xl text-xs font-semibold text-gray-700 hover:bg-gray-100"
                >
                  Đóng
                </button>
                <button
                  type="submit"
                  className="px-5 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-xl text-xs font-bold"
                >
                  Gửi Yêu Cầu Đổi Lịch
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* MODAL: CANCELLATION & 60-MINUTE REFUND PREVIEW */}
      {showCancelModal && refundPreview && (
        <div className="fixed inset-0 z-50 bg-black/60 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="bg-white rounded-3xl max-w-lg w-full p-6 sm:p-8 space-y-6 shadow-2xl">
            <div className="flex items-start space-x-3 text-rose-600">
              <AlertTriangle className="w-6 h-6 flex-shrink-0 mt-0.5" />
              <div>
                <h3 className="text-xl font-bold text-gray-900">Xác Nhận Hủy Lịch Khám</h3>
                <p className="text-xs text-gray-500 mt-1">
                  Bệnh nhân có quyền chủ động hủy lịch trước giờ hẹn không cần Lễ tân phê duyệt.
                </p>
              </div>
            </div>

            {/* Refund calculation result */}
            <div className="bg-gray-50 p-4 rounded-2xl border border-gray-200 space-y-3 text-xs">
              <h4 className="font-bold text-gray-800 uppercase tracking-wider text-[11px]">
                Chi Tiết Tính Hoàn Phí Theo Quy Định
              </h4>
              <div className="flex justify-between py-1 border-b">
                <span>Số tiền đã thanh toán:</span>
                <span className="font-bold">
                  {new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(
                    refundPreview.paidAmount
                  )}
                </span>
              </div>
              <div className="flex justify-between py-1 border-b">
                <span>Thời gian còn lại đến giờ hẹn:</span>
                <span className="font-bold text-teal-700">{refundPreview.minutesUntilAppointment} phút</span>
              </div>
              <div className="flex justify-between py-1 border-b">
                <span>Tỷ lệ hoàn tiền:</span>
                <span className="font-bold text-emerald-600">{refundPreview.refundPercentage}%</span>
              </div>
              <div className="flex justify-between py-1 border-b">
                <span>Số tiền thực tế được hoàn:</span>
                <span className="font-extrabold text-base text-emerald-600">
                  {new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(
                    refundPreview.refundAmount
                  )}
                </span>
              </div>
              <div className="flex justify-between py-1">
                <span>Phí hủy:</span>
                <span className="font-bold text-rose-600">
                  {new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(
                    refundPreview.cancellationFee
                  )}
                </span>
              </div>
              <p className="text-[11px] text-gray-500 italic pt-1">{refundPreview.policyNote}</p>
            </div>

            <div>
              <label className="block text-xs font-bold text-gray-700 mb-1">
                Lý do hủy lịch (Tùy chọn)
              </label>
              <textarea
                rows={2}
                placeholder="Ví dụ: Đã khỏi bệnh, có việc đột xuất..."
                value={cancelReason}
                onChange={(e) => setCancelReason(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-xl text-xs focus:ring-2 focus:ring-rose-500"
              />
            </div>

            <div className="flex justify-end space-x-3 pt-3">
              <button
                type="button"
                onClick={() => setShowCancelModal(false)}
                className="px-4 py-2 border rounded-xl text-xs font-semibold text-gray-700 hover:bg-gray-100"
              >
                Không, giữ lịch lại
              </button>
              <button
                type="button"
                onClick={handleConfirmCancel}
                className="px-5 py-2 bg-rose-600 hover:bg-rose-700 text-white rounded-xl text-xs font-bold"
              >
                Đồng Ý Hủy Lịch Khám
              </button>
            </div>
          </div>
        </div>
      )}

      {/* MODAL: PAYOS VIETQR PAYMENT */}
      {showPaymentModal && (
        <div className="fixed inset-0 z-50 bg-black/60 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="bg-white rounded-3xl max-w-md w-full p-6 sm:p-8 space-y-5 shadow-2xl">
            <div className="flex justify-between items-center border-b pb-3">
              <div className="flex items-center space-x-2 text-teal-700">
                <CreditCard className="w-5 h-5" />
                <h3 className="text-lg font-bold text-gray-900">Thanh Toán VietQR (PayOS)</h3>
              </div>
              <button
                type="button"
                onClick={() => setShowPaymentModal(false)}
                className="text-gray-400 hover:text-gray-600 font-bold text-lg"
              >
                ✕
              </button>
            </div>

            {loadingPayment ? (
              <div className="py-12 text-center text-xs text-gray-500 space-y-2">
                <RefreshCw className="w-8 h-8 animate-spin mx-auto text-teal-600" />
                <p>Đang khởi tạo mã VietQR từ cổng PayOS...</p>
              </div>
            ) : paymentLink ? (
              <div className="space-y-4 text-center">
                <div className="flex justify-center">
                  <img
                    src={paymentLink.qrCode?.startsWith('data:') || paymentLink.qrCode?.startsWith('http') || paymentLink.qrCode?.startsWith('/')
                      ? paymentLink.qrCode
                      : `https://api.qrserver.com/v1/create-qr-code/?size=220x220&data=${encodeURIComponent(paymentLink.qrCode || '')}`}
                    alt="VietQR Techcombank"
                    className="w-56 h-auto max-w-full rounded-xl shadow border p-1 bg-white mx-auto object-contain"
                  />
                </div>

                <div className="text-xs text-gray-600 space-y-1 bg-gray-50 p-3 rounded-xl border border-gray-100 text-left">
                  <p>Ngân hàng: <strong className="text-gray-900">{paymentLink.bankName || 'Techcombank'}</strong></p>
                  <p>Chủ tài khoản: <strong className="text-gray-900">{paymentLink.accountName || 'LE HUYNH PHUONG ANH'}</strong></p>
                  <p>Số tài khoản: <strong className="font-mono text-teal-700 font-bold">{paymentLink.accountNumber || '1903 8779 7590 14'}</strong></p>
                  <p>Số tiền: <strong className="text-teal-700 font-bold">{new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(appointment?.servicePrice || 150000)}</strong></p>
                  <p>Cuộc hẹn: <strong className="font-mono text-gray-900">{appointment?.bookingCode}</strong></p>
                  <p>Nội dung CK: <strong className="font-mono text-gray-900">TT {appointment?.bookingCode}</strong></p>
                  <p>Trạng thái: <span className="text-amber-700 font-bold">Chờ chuyển khoản</span></p>
                </div>

                <div className="flex flex-col sm:flex-row gap-2 justify-center pt-2">
                  {paymentLink.checkoutUrl && !paymentLink.checkoutUrl.startsWith('/') && (
                    <a
                      href={paymentLink.checkoutUrl}
                      target="_blank"
                      rel="noreferrer"
                      className="inline-flex items-center justify-center px-4 py-2 bg-teal-600 hover:bg-teal-700 text-white rounded-xl text-xs font-bold transition"
                    >
                      Mở Cổng PayOS <ExternalLink className="w-3.5 h-3.5 ml-1" />
                    </a>
                  )}
                  <button
                    type="button"
                    disabled={checkingPayment}
                    onClick={() => appointment && handleCheckPaymentStatus(appointment.id)}
                    className="inline-flex items-center justify-center px-4 py-2 border border-teal-600 text-teal-700 hover:bg-teal-50 rounded-xl text-xs font-bold transition"
                  >
                    <RefreshCw className={`w-3.5 h-3.5 mr-1 ${checkingPayment ? 'animate-spin' : ''}`} />
                    Kiểm tra kết quả
                  </button>
                </div>

                <div className="pt-2 border-t border-gray-100">
                  <button
                    type="button"
                    onClick={() => appointment && handleMockConfirm(appointment.id)}
                    className="text-[11px] text-teal-600 hover:underline"
                  >
                    [Chế độ Demo: Bấm vào đây để mô phỏng quét mã thành công]
                  </button>
                </div>
              </div>
            ) : null}
          </div>
        </div>
      )}
    </div>
  );
};
