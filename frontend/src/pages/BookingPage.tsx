import React, { useState, useEffect } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import {
  catalogApi,
  appointmentApi,
  mockSpecialties,
  mockDoctors,
  mockServices,
} from '../api/client';
import {
  Specialty,
  Doctor,
  ClinicService,
  AvailableSlot,
  GuestBookingRequest,
  BookingConfirmation,
} from '../types';
import { getDoctorAvatar, getDoctorLocalFallback } from '../utils/doctorAvatars';
import {
  CheckCircle,
  AlertCircle,
  ArrowRight,
  ArrowLeft,
  Info,
  ShieldCheck,
  Copy,
} from 'lucide-react';

export const BookingPage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();

  // Wizard Step (1: Specialty & Service, 2: Doctor & Slot, 3: Patient Info, 4: Success)
  const [currentStep, setCurrentStep] = useState<number>(1);

  // Catalogs
  const [specialties, setSpecialties] = useState<Specialty[]>(mockSpecialties);
  const [doctors, setDoctors] = useState<Doctor[]>(mockDoctors);
  const [services, setServices] = useState<ClinicService[]>(mockServices);
  const [availableSlots, setAvailableSlots] = useState<AvailableSlot[]>([]);

  // Selection states
  const [selectedSpecialtyId, setSelectedSpecialtyId] = useState<string>(
    searchParams.get('specialtyId') || ''
  );
  const [selectedServiceId, setSelectedServiceId] = useState<string>('');
  const [selectedDoctorId, setSelectedDoctorId] = useState<string>(
    searchParams.get('doctorId') || ''
  );
  const [appointmentDate, setAppointmentDate] = useState<string>(
    new Date(Date.now() + 86400000).toISOString().split('T')[0] // default tomorrow
  );
  const [selectedSlot, setSelectedSlot] = useState<string>('');

  // Patient info (FullName + DOB or BirthYear + Phone)
  const [fullName, setFullName] = useState<string>('');
  const [dobMode, setDobMode] = useState<'full' | 'year'>('full');
  const [dateOfBirth, setDateOfBirth] = useState<string>('');
  const [birthYear, setBirthYear] = useState<string>('');
  const [gender, setGender] = useState<string>('Nam');
  const [contactPhone, setContactPhone] = useState<string>('');
  const [citizenId, setCitizenId] = useState<string>('');
  const [healthInsuranceNumber, setHealthInsuranceNumber] = useState<string>('');
  const [notes, setNotes] = useState<string>('');

  // UI state
  const [submitting, setSubmitting] = useState<boolean>(false);
  const [errorMessage, setErrorMessage] = useState<string>('');
  const [confirmation, setConfirmation] = useState<BookingConfirmation | null>(null);
  const [copiedCode, setCopiedCode] = useState<boolean>(false);

  // Load catalogs
  useEffect(() => {
    const loadData = async () => {
      try {
        const [specs, docs, srvs] = await Promise.all([
          catalogApi.getSpecialties(),
          catalogApi.getDoctors(),
          catalogApi.getServices(),
        ]);
        setSpecialties(specs);
        setDoctors(docs);
        setServices(srvs);

        if (!selectedSpecialtyId && specs.length > 0) {
          setSelectedSpecialtyId(specs[0].id);
        }
      } catch (err) {
        console.error('Error fetching data:', err);
      }
    };
    loadData();
  }, []);

  // Update filtered doctors & services when specialty changes
  const filteredDoctors = doctors.filter(
    (d) => !selectedSpecialtyId || d.specialtyId === selectedSpecialtyId
  );
  const filteredServices = services.filter(
    (s) => !selectedSpecialtyId || s.specialtyId === selectedSpecialtyId
  );

  // Auto-select first service/doctor if unselected
  useEffect(() => {
    if (filteredServices.length > 0 && !selectedServiceId) {
      setSelectedServiceId(filteredServices[0].id);
    }
    if (filteredDoctors.length > 0 && !selectedDoctorId) {
      setSelectedDoctorId(filteredDoctors[0].id);
    }
  }, [selectedSpecialtyId, filteredServices, filteredDoctors]);

  // Load available slots
  useEffect(() => {
    if (selectedDoctorId && appointmentDate) {
      catalogApi.getAvailableSlots(selectedDoctorId, appointmentDate).then((slots) => {
        setAvailableSlots(slots);
        const firstAvailable = slots.find((s) => s.isAvailable);
        if (firstAvailable) {
          setSelectedSlot(firstAvailable.startTime);
        }
      });
    }
  }, [selectedDoctorId, appointmentDate]);

  // Validation before proceeding to step 3
  const handleProceedToStep2 = () => {
    if (!selectedSpecialtyId) {
      setErrorMessage('Vui lòng chọn một chuyên khoa.');
      return;
    }
    if (!selectedServiceId) {
      setErrorMessage('Vui lòng chọn một dịch vụ khám.');
      return;
    }
    setErrorMessage('');
    setCurrentStep(2);
  };

  const handleProceedToStep3 = () => {
    if (!selectedDoctorId) {
      setErrorMessage('Vui lòng chọn bác sĩ khám.');
      return;
    }
    if (!selectedSlot) {
      setErrorMessage('Vui lòng chọn một khung giờ còn trống.');
      return;
    }
    setErrorMessage('');
    setCurrentStep(3);
  };

  // Final Submit
  const handleBookingSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMessage('');

    // Validate Required invariants: FullName + (DOB or BirthYear) + Phone
    if (!fullName.trim()) {
      setErrorMessage('Họ và tên bệnh nhân là bắt buộc.');
      return;
    }
    if (dobMode === 'full' && !dateOfBirth) {
      setErrorMessage('Vui lòng nhập Ngày tháng năm sinh đầy đủ, hoặc chuyển sang chế độ Chỉ biết năm sinh.');
      return;
    }
    if (dobMode === 'year' && (!birthYear || isNaN(Number(birthYear)) || Number(birthYear) < 1900 || Number(birthYear) > new Date().getFullYear())) {
      setErrorMessage('Vui lòng nhập Năm sinh hợp lệ (ví dụ: 1985).');
      return;
    }
    if (!contactPhone.trim() || !/^[0-9]{9,11}$/.test(contactPhone.replace(/[\s-]/g, ''))) {
      setErrorMessage('Số điện thoại liên hệ không hợp lệ (cần từ 9 đến 11 số để nhận OTP).');
      return;
    }

    const payload: GuestBookingRequest = {
      patientFullName: fullName.trim(),
      dateOfBirth: dobMode === 'full' ? dateOfBirth : undefined,
      birthYear: dobMode === 'year' ? Number(birthYear) : undefined,
      gender,
      contactPhone: contactPhone.trim(),
      citizenId: citizenId.trim() || undefined,
      healthInsuranceNumber: healthInsuranceNumber.trim() || undefined,
      specialtyId: selectedSpecialtyId,
      doctorId: selectedDoctorId,
      serviceId: selectedServiceId,
      appointmentDate,
      startTime: selectedSlot,
      notes: notes.trim() || undefined,
    };

    setSubmitting(true);
    try {
      const result = await appointmentApi.bookGuest(payload);
      setConfirmation(result);
      setCurrentStep(4);
    } catch (err: any) {
      setErrorMessage(err.message || 'Đặt lịch không thành công. Vui lòng kiểm tra lại thông tin.');
    } finally {
      setSubmitting(false);
    }
  };

  const copyBookingCode = () => {
    if (confirmation) {
      navigator.clipboard.writeText(confirmation.bookingCode);
      setCopiedCode(true);
      setTimeout(() => setCopiedCode(false), 2500);
    }
  };

  const selectedSpecialtyObj = specialties.find((s) => s.id === selectedSpecialtyId);
  const selectedDoctorObj = doctors.find((d) => d.id === selectedDoctorId);
  const selectedServiceObj = services.find((s) => s.id === selectedServiceId);

  return (
    <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-10 space-y-8">
      {/* Step Indicator Header */}
      <div>
        <div className="flex items-center justify-between pb-6 border-b border-gray-200">
          <div>
            <h1 className="text-2xl sm:text-3xl font-extrabold text-gray-900">
              Đặt Lịch Khám Trực Tuyến
            </h1>
            <p className="text-sm text-teal-700 font-medium mt-1 flex items-center">
              <ShieldCheck className="w-4 h-4 mr-1 text-teal-600" />
              Chế độ Guest Booking: Hoàn toàn không yêu cầu đăng nhập hoặc tạo tài khoản
            </p>
          </div>
          <div className="text-right hidden sm:block">
            <span className="text-xs font-semibold uppercase tracking-wider text-gray-400">
              Bước {currentStep} / 4
            </span>
            <p className="text-sm font-bold text-teal-600">
              {currentStep === 1 && 'Chuyên khoa & Dịch vụ'}
              {currentStep === 2 && 'Bác sĩ & Khung giờ'}
              {currentStep === 3 && 'Thông tin bệnh nhân'}
              {currentStep === 4 && 'Hoàn tất đặt lịch'}
            </p>
          </div>
        </div>

        {/* Wizard progress bar */}
        <div className="mt-4 grid grid-cols-4 gap-2">
          {[1, 2, 3, 4].map((step) => (
            <div
              key={step}
              className={`h-2 rounded-full transition-all duration-300 ${currentStep >= step ? 'bg-teal-600' : 'bg-gray-200'
                }`}
            />
          ))}
        </div>
      </div>

      {/* Error alert */}
      {errorMessage && (
        <div className="p-4 bg-red-50 border-l-4 border-red-500 rounded-xl flex items-start space-x-3 text-red-800 text-sm">
          <AlertCircle className="w-5 h-5 flex-shrink-0 text-red-500 mt-0.5" />
          <div>{errorMessage}</div>
        </div>
      )}

      {/* STEP 1: CHUYÊN KHOA & DỊCH VỤ */}
      {currentStep === 1 && (
        <div className="space-y-8 bg-white p-6 sm:p-8 rounded-3xl border border-gray-200 shadow-sm">
          <div>
            <h2 className="text-lg font-bold text-gray-900 mb-4 flex items-center">
              <span className="w-7 h-7 rounded-lg bg-teal-100 text-teal-700 inline-flex items-center justify-center text-xs font-bold mr-2">
                1
              </span>
              Lựa Chọn Chuyên Khoa Khám
            </h2>
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
              {specialties.map((spec) => (
                <div
                  key={spec.id}
                  onClick={() => {
                    setSelectedSpecialtyId(spec.id);
                    setSelectedServiceId('');
                    setSelectedDoctorId('');
                  }}
                  className={`p-4 rounded-2xl border-2 cursor-pointer transition text-left ${selectedSpecialtyId === spec.id
                    ? 'border-teal-600 bg-teal-50/50 shadow-sm'
                    : 'border-gray-200 hover:border-gray-300 bg-white'
                    }`}
                >
                  <p className="font-bold text-gray-900 text-sm">{spec.name}</p>
                  <p className="text-xs text-gray-500 mt-1 line-clamp-2">{spec.description}</p>
                </div>
              ))}
            </div>
          </div>

          <div>
            <h2 className="text-lg font-bold text-gray-900 mb-4 flex items-center">
              <span className="w-7 h-7 rounded-lg bg-teal-100 text-teal-700 inline-flex items-center justify-center text-xs font-bold mr-2">
                2
              </span>
              Lựa Chọn Dịch Vụ Khám
            </h2>
            {filteredServices.length === 0 ? (
              <p className="text-xs text-gray-500 italic">Chưa có dịch vụ nào cho chuyên khoa này.</p>
            ) : (
              <div className="space-y-3">
                {filteredServices.map((srv) => (
                  <div
                    key={srv.id}
                    onClick={() => setSelectedServiceId(srv.id)}
                    className={`p-4 rounded-2xl border-2 cursor-pointer transition flex items-center justify-between ${selectedServiceId === srv.id
                      ? 'border-teal-600 bg-teal-50/50 shadow-sm'
                      : 'border-gray-200 hover:border-gray-300 bg-white'
                      }`}
                  >
                    <div>
                      <p className="font-bold text-gray-900 text-sm">{srv.name}</p>
                      <p className="text-xs text-gray-500 mt-0.5">{srv.description}</p>
                      <span className="inline-block mt-2 text-xs text-gray-400">
                        Thời lượng dự kiến: {srv.durationMinutes} phút
                      </span>
                    </div>
                    <div className="text-right pl-4">
                      <span className="text-base font-extrabold text-teal-700 block">
                        {new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(
                          srv.basePrice
                        )}
                      </span>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>

          <div className="pt-4 flex justify-end">
            <button
              onClick={handleProceedToStep2}
              className="inline-flex items-center px-6 py-3 rounded-xl bg-teal-600 hover:bg-teal-700 text-white font-semibold text-sm shadow-sm transition"
            >
              Tiếp tục chọn Bác sĩ & Giờ khám <ArrowRight className="w-4 h-4 ml-2" />
            </button>
          </div>
        </div>
      )}

      {/* STEP 2: BÁC SĨ & KHUNG GIỜ KHÁM */}
      {currentStep === 2 && (
        <div className="space-y-8 bg-white p-6 sm:p-8 rounded-3xl border border-gray-200 shadow-sm">
          <div>
            <h2 className="text-lg font-bold text-gray-900 mb-4 flex items-center">
              <span className="w-7 h-7 rounded-lg bg-teal-100 text-teal-700 inline-flex items-center justify-center text-xs font-bold mr-2">
                1
              </span>
              Chọn Bác Sĩ Chuyên Khoa ({selectedSpecialtyObj?.name})
            </h2>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              {filteredDoctors.map((doc) => (
                <div
                  key={doc.id}
                  onClick={() => setSelectedDoctorId(doc.id)}
                  className={`p-4 rounded-2xl border-2 cursor-pointer transition flex items-start space-x-3 ${selectedDoctorId === doc.id
                    ? 'border-teal-600 bg-teal-50/50 shadow-sm'
                    : 'border-gray-200 hover:border-gray-300 bg-white'
                    }`}
                >
                  <div className="w-12 h-12 rounded-xl overflow-hidden bg-teal-50 border border-teal-200 flex items-center justify-center flex-shrink-0 shadow-sm">
                    <img
                      src={doc.avatarUrl || getDoctorAvatar(doc)}
                      alt={doc.fullName}
                      className="w-full h-full object-cover"
                      loading="lazy"
                      onError={(e) => {
                        const target = e.target as HTMLImageElement;
                        const fallback = getDoctorLocalFallback(doc);
                        if (!target.src.endsWith(fallback)) {
                          target.src = fallback;
                        }
                      }}
                    />
                  </div>
                  <div>
                    <p className="font-bold text-gray-900 text-sm">{doc.fullName}</p>
                    <p className="text-xs text-teal-700 font-medium">{doc.title}</p>
                    {doc.roomNumber && (
                      <p className="text-xs text-gray-400 mt-1">Phòng khám: {doc.roomNumber}</p>
                    )}
                  </div>
                </div>
              ))}
            </div>
          </div>

          <div>
            <h2 className="text-lg font-bold text-gray-900 mb-4 flex items-center">
              <span className="w-7 h-7 rounded-lg bg-teal-100 text-teal-700 inline-flex items-center justify-center text-xs font-bold mr-2">
                2
              </span>
              Chọn Ngày & Khung Giờ Khám
            </h2>

            <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-6">
              <div className="sm:col-span-1">
                <label className="block text-xs font-semibold text-gray-700 mb-1">Ngày khám</label>
                <input
                  type="date"
                  value={appointmentDate}
                  min={new Date().toISOString().split('T')[0]}
                  onChange={(e) => setAppointmentDate(e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-teal-500"
                />
              </div>

              <div className="sm:col-span-2">
                <label className="block text-xs font-semibold text-gray-700 mb-1">
                  Khung giờ khả dụng (Chống trùng lịch tự động)
                </label>
                <div className="grid grid-cols-3 sm:grid-cols-4 gap-2">
                  {availableSlots.map((slot) => (
                    <button
                      key={slot.startTime}
                      type="button"
                      disabled={!slot.isAvailable}
                      onClick={() => setSelectedSlot(slot.startTime)}
                      className={`py-2 px-3 rounded-xl text-xs font-semibold transition border ${!slot.isAvailable
                        ? 'bg-gray-100 text-gray-400 border-gray-200 cursor-not-allowed line-through'
                        : selectedSlot === slot.startTime
                          ? 'bg-teal-600 text-white border-teal-600 shadow-sm'
                          : 'bg-white text-gray-800 border-gray-300 hover:border-teal-500'
                        }`}
                    >
                      {slot.startTime} - {slot.endTime}
                    </button>
                  ))}
                </div>
              </div>
            </div>
          </div>

          <div className="pt-4 flex justify-between">
            <button
              onClick={() => setCurrentStep(1)}
              className="inline-flex items-center px-5 py-2.5 rounded-xl border border-gray-300 bg-white hover:bg-gray-50 text-gray-700 text-sm font-semibold transition"
            >
              <ArrowLeft className="w-4 h-4 mr-2" /> Quay lại
            </button>
            <button
              onClick={handleProceedToStep3}
              className="inline-flex items-center px-6 py-3 rounded-xl bg-teal-600 hover:bg-teal-700 text-white font-semibold text-sm shadow-sm transition"
            >
              Tiếp tục điền thông tin <ArrowRight className="w-4 h-4 ml-2" />
            </button>
          </div>
        </div>
      )}

      {/* STEP 3: THÔNG TIN BỆNH NHÂN (FULLNAME + DOB/BIRTHYEAR + PHONE) */}
      {currentStep === 3 && (
        <form onSubmit={handleBookingSubmit} className="space-y-8 bg-white p-6 sm:p-8 rounded-3xl border border-gray-200 shadow-sm">
          <div>
            <h2 className="text-lg font-bold text-gray-900 mb-2 flex items-center">
              <span className="w-7 h-7 rounded-lg bg-teal-100 text-teal-700 inline-flex items-center justify-center text-xs font-bold mr-2">
                3
              </span>
              Thông Tin Người Khám Bệnh
            </h2>
            <p className="text-xs text-gray-500">
              Vui lòng cung cấp chính xác họ tên và ngày sinh/năm sinh để tạo hồ sơ bệnh án chuẩn xác.
            </p>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            {/* Full Name */}
            <div className="sm:col-span-2">
              <label className="block text-xs font-bold text-gray-800 mb-1">
                Họ và Tên Bệnh Nhân <span className="text-red-500">*</span>
              </label>
              <input
                type="text"
                required
                placeholder="Ví dụ: NGUYỄN VĂN AN"
                value={fullName}
                onChange={(e) => setFullName(e.target.value.toUpperCase())}
                className="w-full px-4 py-2.5 border border-gray-300 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-teal-500"
              />
            </div>

            {/* DOB or BirthYear Selector */}
            <div className="sm:col-span-2 bg-gray-50 p-4 rounded-2xl border border-gray-200 space-y-3">
              <div className="flex items-center justify-between">
                <label className="text-xs font-bold text-gray-800">
                  Thông Tin Độ Tuổi / Năm Sinh <span className="text-red-500">*</span>
                </label>
                <div className="flex items-center space-x-2 text-xs">
                  <button
                    type="button"
                    onClick={() => setDobMode('full')}
                    className={`px-3 py-1 rounded-lg font-semibold transition ${dobMode === 'full' ? 'bg-teal-600 text-white' : 'bg-gray-200 text-gray-700'
                      }`}
                  >
                    Đầy đủ ngày tháng năm sinh
                  </button>
                  <button
                    type="button"
                    onClick={() => setDobMode('year')}
                    className={`px-3 py-1 rounded-lg font-semibold transition ${dobMode === 'year' ? 'bg-teal-600 text-white' : 'bg-gray-200 text-gray-700'
                      }`}
                  >
                    Chỉ nhớ năm sinh
                  </button>
                </div>
              </div>

              {dobMode === 'full' ? (
                <div>
                  <input
                    type="date"
                    required
                    value={dateOfBirth}
                    max={new Date().toISOString().split('T')[0]}
                    onChange={(e) => setDateOfBirth(e.target.value)}
                    className="w-full sm:w-1/2 px-4 py-2 border border-gray-300 rounded-xl text-sm bg-white focus:outline-none focus:ring-2 focus:ring-teal-500"
                  />
                  <p className="text-xs text-gray-500 mt-1">Định dạng: Ngày/Tháng/Năm</p>
                </div>
              ) : (
                <div>
                  <input
                    type="number"
                    required
                    placeholder="Ví dụ: 1980"
                    min="1900"
                    max={new Date().getFullYear()}
                    value={birthYear}
                    onChange={(e) => setBirthYear(e.target.value)}
                    className="w-full sm:w-1/2 px-4 py-2 border border-gray-300 rounded-xl text-sm bg-white focus:outline-none focus:ring-2 focus:ring-teal-500"
                  />
                  <p className="text-xs text-gray-500 mt-1">
                    Dành cho người cao tuổi hoặc trường hợp không nhớ chính xác ngày tháng sinh.
                  </p>
                </div>
              )}
            </div>

            {/* Gender */}
            <div>
              <label className="block text-xs font-bold text-gray-800 mb-1">Giới tính</label>
              <select
                value={gender}
                onChange={(e) => setGender(e.target.value)}
                className="w-full px-4 py-2.5 border border-gray-300 rounded-xl text-sm bg-white focus:outline-none focus:ring-2 focus:ring-teal-500"
              >
                <option value="Nam">Nam</option>
                <option value="Nữ">Nữ</option>
                <option value="Khác">Khác</option>
              </select>
            </div>

            {/* Contact Phone */}
            <div>
              <label className="block text-xs font-bold text-gray-800 mb-1">
                Số Điện Thoại Nhận OTP <span className="text-red-500">*</span>
              </label>
              <input
                type="tel"
                required
                placeholder="Ví dụ: 0912345678"
                value={contactPhone}
                onChange={(e) => setContactPhone(e.target.value)}
                className="w-full px-4 py-2.5 border border-gray-300 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-teal-500"
              />
              <p className="text-xs text-gray-500 mt-1">
                Số này dùng để nhận mã xác nhận và tra cứu / đổi / hủy lịch hẹn.
              </p>
            </div>

            {/* Optional Identifiers */}
            <div>
              <label className="block text-xs font-semibold text-gray-700 mb-1">
                Số CCCD / Định danh (Tùy chọn)
              </label>
              <input
                type="text"
                placeholder="12 chữ số căn cước"
                value={citizenId}
                onChange={(e) => setCitizenId(e.target.value)}
                className="w-full px-4 py-2.5 border border-gray-300 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-teal-500"
              />
            </div>

            <div>
              <label className="block text-xs font-semibold text-gray-700 mb-1">
                Mã Thẻ BHYT (Tùy chọn)
              </label>
              <input
                type="text"
                placeholder="10 hoặc 15 ký tự BHYT"
                value={healthInsuranceNumber}
                onChange={(e) => setHealthInsuranceNumber(e.target.value)}
                className="w-full px-4 py-2.5 border border-gray-300 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-teal-500"
              />
            </div>

            {/* Notes */}
            <div className="sm:col-span-2">
              <label className="block text-xs font-semibold text-gray-700 mb-1">
                Triệu chứng / Ghi chú lý do khám
              </label>
              <textarea
                rows={3}
                placeholder="Mô tả sơ lược triệu chứng, tiền sử dị ứng thuốc (nếu có)..."
                value={notes}
                onChange={(e) => setNotes(e.target.value)}
                className="w-full px-4 py-2.5 border border-gray-300 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-teal-500"
              />
            </div>
          </div>

          {/* Appointment Summary Box */}
          <div className="bg-teal-50 border border-teal-200 rounded-2xl p-4 sm:p-5 text-sm space-y-2">
            <h4 className="font-bold text-teal-950 flex items-center">
              <Info className="w-4 h-4 mr-1.5 text-teal-700" /> Tóm tắt lịch hẹn sắp đặt
            </h4>
            <div className="grid grid-cols-2 sm:grid-cols-4 gap-2 text-xs text-gray-700 pt-2 border-t border-teal-100">
              <div>
                <span className="text-gray-500 block">Chuyên khoa:</span>
                <strong>{selectedSpecialtyObj?.name}</strong>
              </div>
              <div>
                <span className="text-gray-500 block">Bác sĩ phụ trách:</span>
                <strong>{selectedDoctorObj?.fullName}</strong>
              </div>
              <div>
                <span className="text-gray-500 block">Thời gian:</span>
                <strong>{selectedSlot} ngày {appointmentDate}</strong>
              </div>
              <div>
                <span className="text-gray-500 block">Dịch vụ & Phí dự kiến:</span>
                <strong className="text-teal-700 font-extrabold">
                  {new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(
                    selectedServiceObj?.basePrice || 150000
                  )}
                </strong>
              </div>
            </div>
          </div>

          <div className="pt-4 flex justify-between">
            <button
              type="button"
              onClick={() => setCurrentStep(2)}
              className="inline-flex items-center px-5 py-2.5 rounded-xl border border-gray-300 bg-white hover:bg-gray-50 text-gray-700 text-sm font-semibold transition"
            >
              <ArrowLeft className="w-4 h-4 mr-2" /> Quay lại
            </button>

            <button
              type="submit"
              disabled={submitting}
              className="inline-flex items-center px-8 py-3 rounded-xl bg-teal-600 hover:bg-teal-700 disabled:bg-gray-400 text-white font-bold text-sm shadow-md transition"
            >
              {submitting ? 'Đang gửi đăng ký...' : 'Xác Nhận Đặt Lịch Khám'}
            </button>
          </div>
        </form>
      )}

      {/* STEP 4: HOÀN TẤT ĐẶT LỊCH (BOOKING CODE & HƯỚNG DẪN) */}
      {currentStep === 4 && confirmation && (
        <div className="bg-white p-6 sm:p-10 rounded-3xl border border-teal-200 shadow-xl space-y-8 text-center">
          <div className="w-16 h-16 bg-emerald-100 text-emerald-600 rounded-full flex items-center justify-center mx-auto">
            <CheckCircle className="w-10 h-10" />
          </div>

          <div className="space-y-2">
            <h2 className="text-2xl sm:text-3xl font-extrabold text-gray-900">
              Đặt Lịch Khám Thành Công!
            </h2>
            <p className="text-sm text-gray-600 max-w-lg mx-auto">
              Lịch khám của bạn đã được ghi nhận vào hệ thống. Lễ tân sẽ xác nhận và gửi thông báo cho bạn.
            </p>
          </div>

          {/* Booking Code Card */}
          <div className="max-w-md mx-auto bg-gray-50 border-2 border-dashed border-teal-500 rounded-3xl p-6 space-y-4">
            <span className="text-xs uppercase font-bold text-teal-700 tracking-wider">
              Mã Đặt Lịch Khám Của Bạn (Booking Code)
            </span>
            <div className="flex items-center justify-center space-x-3">
              <span className="font-mono text-3xl font-black text-gray-900 tracking-wider">
                {confirmation.bookingCode}
              </span>
              <button
                type="button"
                onClick={copyBookingCode}
                className="p-2 text-teal-600 hover:text-teal-800 hover:bg-teal-100 rounded-lg transition"
                title="Sao chép mã"
              >
                <Copy className="w-5 h-5" />
              </button>
            </div>
            {copiedCode && <p className="text-xs text-emerald-600 font-semibold">Đã sao chép mã vào clipboard!</p>}
            <p className="text-xs text-gray-500 leading-relaxed">
              Vui lòng lưu lại mã này. Bạn có thể dùng mã này kết hợp với số điện thoại nhận OTP để tra cứu, yêu cầu đổi lịch hoặc hủy lịch bất kỳ lúc nào.
            </p>
          </div>

          {/* Detailed Summary */}
          <div className="max-w-md mx-auto text-left text-xs bg-gray-50 rounded-2xl p-4 border border-gray-200 space-y-2 text-gray-700">
            <div className="flex justify-between py-1 border-b border-gray-200">
              <span className="text-gray-500">Người khám:</span>
              <span className="font-bold">{confirmation.patientFullName}</span>
            </div>
            <div className="flex justify-between py-1 border-b border-gray-200">
              <span className="text-gray-500">Bác sĩ:</span>
              <span className="font-bold">{confirmation.doctorName}</span>
            </div>
            <div className="flex justify-between py-1 border-b border-gray-200">
              <span className="text-gray-500">Chuyên khoa:</span>
              <span className="font-bold">{confirmation.specialtyName}</span>
            </div>
            <div className="flex justify-between py-1 border-b border-gray-200">
              <span className="text-gray-500">Khung giờ hẹn:</span>
              <span className="font-bold text-teal-700">
                {confirmation.startTime} - {confirmation.endTime} (Ngày {confirmation.appointmentDate})
              </span>
            </div>
            <div className="flex justify-between py-1 border-b border-gray-200">
              <span className="text-gray-500">Trạng thái:</span>
              <span className="px-2 py-0.5 rounded bg-amber-100 text-amber-800 font-bold">
                Chờ xác nhận (PendingConfirmation)
              </span>
            </div>
            <div className="flex justify-between py-1">
              <span className="text-gray-500">Phí khám dự kiến:</span>
              <span className="font-bold text-teal-700">
                {new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(
                  confirmation.fee
                )}
              </span>
            </div>
          </div>

          <div className="pt-4 flex flex-col sm:flex-row gap-4 justify-center">
            <button
              onClick={() => navigate(`/lookup?code=${confirmation.bookingCode}`)}
              className="px-6 py-3 rounded-xl bg-teal-600 hover:bg-teal-700 text-white font-semibold text-sm shadow-sm transition"
            >
              Tra Cứu / Quản Lý Lịch Hẹn Ngay
            </button>
            <button
              onClick={() => {
                setCurrentStep(1);
                setConfirmation(null);
                setFullName('');
                setDateOfBirth('');
                setBirthYear('');
              }}
              className="px-6 py-3 rounded-xl border border-gray-300 bg-white hover:bg-gray-50 text-gray-700 font-semibold text-sm transition"
            >
              Đặt Thêm Lịch Mới
            </button>
          </div>
        </div>
      )}
    </div>
  );
};
