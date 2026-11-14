import React from 'react';
import { Phone, Clock, MapPin, Shield } from 'lucide-react';
import { Link } from 'react-router-dom';

export const Footer: React.FC = () => {
  return (
    <footer className="bg-gray-900 text-gray-300 mt-20 border-t border-gray-800">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
        <div className="grid grid-cols-1 md:grid-cols-4 gap-8">
          {/* Info */}
          <div className="space-y-4">
            <h3 className="text-white text-lg font-bold">Phòng Khám Đa Khoa</h3>
            <p className="text-sm text-gray-400 leading-relaxed">
              Dịch vụ chăm sóc sức khỏe chất lượng cao, phục vụ tận tâm. Đặt lịch khám nhanh chóng, không cần tạo tài khoản.
            </p>
          </div>

          {/* Quick links */}
          <div>
            <h4 className="text-white text-sm font-semibold tracking-wider uppercase mb-4">Liên Kết Nhanh</h4>
            <ul className="space-y-2 text-sm">
              <li>
                <Link to="/booking" className="hover:text-white transition">
                  Đặt lịch khám bệnh
                </Link>
              </li>
              <li>
                <Link to="/lookup" className="hover:text-white transition">
                  Tra cứu & Đổi / Hủy lịch hẹn
                </Link>
              </li>
              <li>
                <Link to="/doctors" className="hover:text-white transition">
                  Danh sách Bác sĩ chuyên khoa
                </Link>
              </li>
              <li>
                <Link to="/receptionist" className="hover:text-white transition">
                  Cổng tiếp nhận Lễ tân
                </Link>
              </li>
            </ul>
          </div>

          {/* Policy Highlights */}
          <div>
            <h4 className="text-white text-sm font-semibold tracking-wider uppercase mb-4">Chính Sách Lịch Hẹn</h4>
            <ul className="space-y-2 text-xs text-gray-400">
              <li className="flex items-start space-x-2">
                <Shield className="w-4 h-4 text-teal-400 mt-0.5 flex-shrink-0" />
                <span>Hoàn 100% nếu hủy trước giờ hẹn từ 60 phút trở lên.</span>
              </li>
              <li className="flex items-start space-x-2">
                <Shield className="w-4 h-4 text-amber-400 mt-0.5 flex-shrink-0" />
                <span>Hoàn 50% nếu hủy dưới 60 phút trước giờ khám.</span>
              </li>
              <li className="flex items-start space-x-2">
                <Shield className="w-4 h-4 text-blue-400 mt-0.5 flex-shrink-0" />
                <span>Đổi lịch cần Lễ tân phê duyệt; lịch cũ bảo lưu slot đến khi duyệt.</span>
              </li>
            </ul>
          </div>

          {/* Contact */}
          <div className="space-y-3 text-sm">
            <h4 className="text-white text-sm font-semibold tracking-wider uppercase mb-4">Thông Tin Phòng Khám</h4>
            <div className="flex items-start space-x-3 text-gray-400">
              <MapPin className="w-4 h-4 text-teal-400 mt-1 flex-shrink-0" />
              <span>Số 123 Đường Y Tế, Phường Sức Khỏe, TP. Đà Nẵng</span>
            </div>
            <div className="flex items-center space-x-3 text-gray-400">
              <Phone className="w-4 h-4 text-teal-400 flex-shrink-0" />
              <span>Hotline: 1900 6868 (07:00 - 20:00)</span>
            </div>
            <div className="flex items-center space-x-3 text-gray-400">
              <Clock className="w-4 h-4 text-teal-400 flex-shrink-0" />
              <span>Thứ Hai - Chủ Nhật: 07:30 - 17:30</span>
            </div>
          </div>
        </div>

        <div className="mt-8 pt-8 border-t border-gray-800 text-center text-xs text-gray-500">
          <p>© 2026 Clinic Appointment Management System. Phiên bản 1.2.0. Tất cả quyền được bảo lưu.</p>
        </div>
      </div>
    </footer>
  );
};
