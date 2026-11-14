import React, { useState } from 'react';
import {
  ShieldCheck,
  CheckCircle,
  XCircle,
  Calendar,
  RefreshCw,
  AlertTriangle,
  UserCheck,
} from 'lucide-react';

interface ReceptionAppointment {
  id: string;
  bookingCode: string;
  patientFullName: string;
  dobOrYear: string;
  contactPhone: string;
  doctorName: string;
  specialtyName: string;
  date: string;
  timeSlot: string;
  status: 'PendingConfirmation' | 'Confirmed' | 'CheckedIn';
  isDuplicateCandidate?: boolean;
}

interface PendingReschedule {
  id: string;
  bookingCode: string;
  patientName: string;
  oldSchedule: string;
  proposedSchedule: string;
  doctorName: string;
  reason: string;
}

export const ReceptionistDashboardPage: React.FC = () => {
  // Mock appointments for receptionist workflow
  const [appointments, setAppointments] = useState<ReceptionAppointment[]>([
    {
      id: 'apt-101',
      bookingCode: 'BK782910',
      patientFullName: 'NGUYỄN VĂN AN',
      dobOrYear: '1985-04-12',
      contactPhone: '0912345678',
      doctorName: 'BS. CKI Nguyễn Văn An',
      specialtyName: 'Nội Tổng Quát',
      date: '2026-11-20',
      timeSlot: '08:30 - 09:00',
      status: 'PendingConfirmation',
      isDuplicateCandidate: true,
    },
    {
      id: 'apt-102',
      bookingCode: 'BK459201',
      patientFullName: 'LÊ THỊ BÍCH',
      dobOrYear: 'Năm sinh: 1965',
      contactPhone: '0987654321',
      doctorName: 'ThS. BS Trần Thị Mai',
      specialtyName: 'Nhi Khoa',
      date: '2026-11-20',
      timeSlot: '09:00 - 09:30',
      status: 'Confirmed',
    },
  ]);

  const [reschedules, setReschedules] = useState<PendingReschedule[]>([
    {
      id: 'resch-01',
      bookingCode: 'BK459201',
      patientName: 'LÊ THỊ BÍCH',
      oldSchedule: '20/11/2026 (09:00 - 09:30)',
      proposedSchedule: '21/11/2026 (14:00 - 14:30)',
      doctorName: 'ThS. BS Trần Thị Mai',
      reason: 'Bận đột xuất việc gia đình',
    },
  ]);

  const handleConfirmAppointment = (id: string) => {
    setAppointments((prev) =>
      prev.map((apt) => (apt.id === id ? { ...apt, status: 'Confirmed' } : apt))
    );
  };

  const handleCheckIn = (id: string) => {
    setAppointments((prev) =>
      prev.map((apt) => (apt.id === id ? { ...apt, status: 'CheckedIn' } : apt))
    );
  };

  const handleApproveReschedule = (reschId: string) => {
    alert('Đã duyệt đổi lịch thành công! Slot mới đã được khóa và lịch cũ được chuyển.');
    setReschedules((prev) => prev.filter((r) => r.id !== reschId));
  };

  const handleRejectReschedule = (reschId: string) => {
    const reason = prompt('Nhập lý do từ chối đổi lịch:');
    if (reason) {
      alert(`Đã từ chối yêu cầu đổi lịch (Lý do: ${reason}). Lịch cũ của bệnh nhân vẫn giữ nguyên!`);
      setReschedules((prev) => prev.filter((r) => r.id !== reschId));
    }
  };

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-10 space-y-10">
      {/* Header */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4 pb-6 border-b border-gray-200">
        <div>
          <span className="text-xs font-bold text-teal-600 uppercase tracking-wider flex items-center">
            <ShieldCheck className="w-4 h-4 mr-1 text-teal-600" /> Kênh Nghiệp Vụ Phòng Khám
          </span>
          <h1 className="text-3xl font-extrabold text-gray-900 mt-1">Bàn Tiếp Nhận Lễ Tân</h1>
          <p className="text-sm text-gray-500 mt-1">
            Xác nhận lịch đặt mới, phê duyệt yêu cầu đổi lịch và tiếp nhận bệnh nhân (Check-in).
          </p>
        </div>

        <div className="flex items-center space-x-3">
          <span className="px-3 py-1.5 rounded-xl bg-teal-50 border border-teal-200 text-teal-800 text-xs font-bold">
            Ca trực: Lễ tân 01 (Sáng)
          </span>
        </div>
      </div>

      {/* Reschedule Approvals Section */}
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
                Lịch cũ được giữ nguyên đến khi duyệt; cần kiểm tra lại tính sẵn sàng của slot mới.
              </p>
            </div>
          </div>
          <span className="px-3 py-1 rounded-full bg-blue-100 text-blue-800 text-xs font-bold">
            {reschedules.length} yêu cầu
          </span>
        </div>

        {reschedules.length === 0 ? (
          <div className="text-center py-6 text-xs text-gray-400 italic">
            Hiện không có yêu cầu đổi lịch nào đang chờ xử lý.
          </div>
        ) : (
          <div className="space-y-4">
            {reschedules.map((r) => (
              <div
                key={r.id}
                className="p-5 rounded-2xl border border-gray-200 bg-gray-50 flex flex-col md:flex-row justify-between items-start md:items-center gap-4"
              >
                <div className="space-y-1 text-xs">
                  <div className="flex items-center space-x-2">
                    <span className="font-mono font-bold text-teal-700">{r.bookingCode}</span>
                    <span className="text-gray-400">•</span>
                    <span className="font-bold text-gray-900">{r.patientName}</span>
                  </div>
                  <p className="text-gray-600">
                    Bác sĩ: <strong>{r.doctorName}</strong>
                  </p>
                  <p className="text-gray-600">
                    Lịch cũ: <strong className="line-through text-gray-400">{r.oldSchedule}</strong>{' '}
                    <span className="text-blue-600">➔ Đề xuất: <strong>{r.proposedSchedule}</strong></span>
                  </p>
                  <p className="text-gray-500 italic">Lý do: "{r.reason}"</p>
                </div>

                <div className="flex items-center space-x-2">
                  <button
                    onClick={() => handleApproveReschedule(r.id)}
                    className="inline-flex items-center px-4 py-2 bg-emerald-600 hover:bg-emerald-700 text-white rounded-xl text-xs font-bold transition shadow-sm"
                  >
                    <CheckCircle className="w-4 h-4 mr-1.5" /> Duyệt Đổi Lịch
                  </button>
                  <button
                    onClick={() => handleRejectReschedule(r.id)}
                    className="inline-flex items-center px-4 py-2 bg-rose-600 hover:bg-rose-700 text-white rounded-xl text-xs font-bold transition shadow-sm"
                  >
                    <XCircle className="w-4 h-4 mr-1.5" /> Từ Chối
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </section>

      {/* Reception Queue: Confirmations & Check-In */}
      <section className="bg-white rounded-3xl border border-gray-200 shadow-sm p-6 sm:p-8 space-y-6">
        <div className="flex items-center justify-between">
          <div className="flex items-center space-x-3">
            <div className="p-2.5 rounded-xl bg-teal-100 text-teal-700">
              <Calendar className="w-5 h-5" />
            </div>
            <div>
              <h2 className="text-lg font-bold text-gray-900">Danh Sách Lịch Khám Trong Ngày</h2>
              <p className="text-xs text-gray-500">
                Xác nhận lịch hẹn của khách (Guest) và thực hiện Check-in khi bệnh nhân có mặt.
              </p>
            </div>
          </div>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs border-collapse">
            <thead>
              <tr className="border-b border-gray-200 text-gray-500 font-semibold uppercase tracking-wider">
                <th className="py-3 px-4">Mã / Bệnh Nhân</th>
                <th className="py-3 px-4">Ngày Sinh / SĐT</th>
                <th className="py-3 px-4">Bác Sĩ / Khoa</th>
                <th className="py-3 px-4">Khung Giờ</th>
                <th className="py-3 px-4">Trạng Thái</th>
                <th className="py-3 px-4 text-right">Thao Tác</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {appointments.map((apt) => (
                <tr key={apt.id} className="hover:bg-gray-50 transition">
                  <td className="py-4 px-4">
                    <span className="font-mono font-bold text-teal-700 block">{apt.bookingCode}</span>
                    <span className="font-bold text-gray-900">{apt.patientFullName}</span>
                    {apt.isDuplicateCandidate && (
                      <span className="inline-flex items-center text-[10px] bg-amber-100 text-amber-800 px-1.5 py-0.5 rounded ml-1 font-medium">
                        <AlertTriangle className="w-3 h-3 mr-0.5" /> Nghi trùng hồ sơ
                      </span>
                    )}
                  </td>
                  <td className="py-4 px-4 text-gray-600">
                    <div>{apt.dobOrYear}</div>
                    <div className="text-gray-400">{apt.contactPhone}</div>
                  </td>
                  <td className="py-4 px-4 text-gray-600">
                    <div className="font-medium text-gray-900">{apt.doctorName}</div>
                    <div className="text-gray-400">{apt.specialtyName}</div>
                  </td>
                  <td className="py-4 px-4 font-semibold text-gray-800">
                    {apt.timeSlot} <br />
                    <span className="text-[11px] text-gray-400 font-normal">{apt.date}</span>
                  </td>
                  <td className="py-4 px-4">
                    <span
                      className={`px-2.5 py-1 rounded-full text-[11px] font-bold inline-block ${
                        apt.status === 'Confirmed'
                          ? 'bg-blue-100 text-blue-800'
                          : apt.status === 'CheckedIn'
                          ? 'bg-emerald-100 text-emerald-800'
                          : 'bg-amber-100 text-amber-800'
                      }`}
                    >
                      {apt.status === 'PendingConfirmation' && 'Chờ Xác Nhận'}
                      {apt.status === 'Confirmed' && 'Đã Xác Nhận'}
                      {apt.status === 'CheckedIn' && 'Đã Check-in'}
                    </span>
                  </td>
                  <td className="py-4 px-4 text-right space-x-2">
                    {apt.status === 'PendingConfirmation' && (
                      <button
                        onClick={() => handleConfirmAppointment(apt.id)}
                        className="px-3 py-1.5 bg-teal-600 hover:bg-teal-700 text-white rounded-lg font-bold text-xs transition"
                      >
                        Xác Nhận Lịch
                      </button>
                    )}
                    {apt.status === 'Confirmed' && (
                      <button
                        onClick={() => handleCheckIn(apt.id)}
                        className="inline-flex items-center px-3 py-1.5 bg-emerald-600 hover:bg-emerald-700 text-white rounded-lg font-bold text-xs transition"
                      >
                        <UserCheck className="w-3.5 h-3.5 mr-1" /> Check-in
                      </button>
                    )}
                    {apt.status === 'CheckedIn' && (
                      <span className="text-xs text-emerald-600 font-semibold">Đang trong phòng chờ</span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>
    </div>
  );
};
