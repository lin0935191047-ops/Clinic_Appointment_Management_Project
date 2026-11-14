import React from 'react';
import { Link, useLocation } from 'react-router-dom';
import { Calendar, Search, Stethoscope, User, ShieldCheck, Settings } from 'lucide-react';

export const Navbar: React.FC = () => {
  const location = useLocation();

  const isActive = (path: string) => location.pathname === path;

  return (
    <header className="sticky top-0 z-50 bg-white border-b border-gray-200 shadow-sm">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex justify-between items-center h-16 gap-2 lg:gap-4">
          {/* Logo & Brand */}
          <Link to="/" className="flex items-center space-x-2.5 sm:space-x-3 group shrink-0">
            <div className="w-9 h-9 sm:w-10 sm:h-10 rounded-xl bg-teal-600 flex items-center justify-center text-white shadow-md group-hover:bg-teal-700 transition shrink-0">
              <Stethoscope className="w-5 h-5 sm:w-6 sm:h-6" />
            </div>
            <div className="whitespace-nowrap">
              <span className="text-base sm:text-lg lg:text-xl font-bold text-gray-900 tracking-tight block leading-tight">
                Phòng Khám Đa Khoa
              </span>
              <span className="text-[11px] sm:text-xs text-teal-600 font-medium tracking-wide uppercase block">
                Clinic Care & Booking
              </span>
            </div>
          </Link>

          {/* Navigation Links */}
          <nav className="hidden md:flex items-center gap-1 lg:gap-1.5 xl:gap-2">
            <Link
              to="/"
              className={`inline-flex items-center justify-center px-2.5 py-1.5 lg:px-3 lg:py-2 rounded-lg text-xs lg:text-sm font-medium whitespace-nowrap transition shrink-0 ${
                isActive('/')
                  ? 'bg-teal-50 text-teal-700 font-semibold'
                  : 'text-gray-600 hover:text-gray-900 hover:bg-gray-100'
              }`}
            >
              <span>Trang Chủ</span>
            </Link>

            <Link
              to="/doctors"
              className={`inline-flex items-center justify-center px-2.5 py-1.5 lg:px-3 lg:py-2 rounded-lg text-xs lg:text-sm font-medium whitespace-nowrap transition shrink-0 ${
                isActive('/doctors')
                  ? 'bg-teal-50 text-teal-700 font-semibold'
                  : 'text-gray-600 hover:text-gray-900 hover:bg-gray-100'
              }`}
            >
              <span>Đội Ngũ Bác Sĩ</span>
            </Link>

            <Link
              to="/lookup"
              className={`inline-flex items-center justify-center gap-1.5 px-2.5 py-1.5 lg:px-3 lg:py-2 rounded-lg text-xs lg:text-sm font-medium whitespace-nowrap transition shrink-0 ${
                isActive('/lookup')
                  ? 'bg-teal-50 text-teal-700 font-semibold'
                  : 'text-gray-600 hover:text-gray-900 hover:bg-gray-100'
              }`}
            >
              <Search className="w-4 h-4 text-gray-500 shrink-0" />
              <span>Tra Cứu Lịch Hẹn</span>
            </Link>

            <Link
              to="/receptionist"
              className={`inline-flex items-center justify-center gap-1.5 px-2.5 py-1.5 lg:px-3 lg:py-2 rounded-lg text-xs lg:text-sm font-medium whitespace-nowrap transition shrink-0 ${
                isActive('/receptionist')
                  ? 'bg-teal-50 text-teal-700 font-semibold'
                  : 'text-gray-600 hover:text-gray-900 hover:bg-gray-100'
              }`}
            >
              <ShieldCheck className="w-4 h-4 text-teal-600 shrink-0" />
              <span>Kênh Lễ Tân</span>
            </Link>

            <Link
              to="/doctor"
              className={`inline-flex items-center justify-center gap-1.5 px-2.5 py-1.5 lg:px-3 lg:py-2 rounded-lg text-xs lg:text-sm font-medium whitespace-nowrap transition shrink-0 ${
                isActive('/doctor')
                  ? 'bg-blue-50 text-blue-700 font-semibold'
                  : 'text-gray-600 hover:text-gray-900 hover:bg-gray-100'
              }`}
            >
              <Stethoscope className="w-4 h-4 text-blue-600 shrink-0" />
              <span>Kênh Bác Sĩ</span>
            </Link>

            <Link
              to="/admin"
              className={`inline-flex items-center justify-center gap-1.5 px-2.5 py-1.5 lg:px-3 lg:py-2 rounded-lg text-xs lg:text-sm font-medium whitespace-nowrap transition shrink-0 ${
                isActive('/admin')
                  ? 'bg-purple-50 text-purple-700 font-semibold'
                  : 'text-gray-600 hover:text-gray-900 hover:bg-gray-100'
              }`}
            >
              <Settings className="w-4 h-4 text-purple-600 shrink-0" />
              <span>Quản Trị</span>
            </Link>
          </nav>

          {/* Action CTAs */}
          <div className="flex items-center space-x-2 lg:space-x-3 shrink-0">
            <Link
              to="/booking"
              className="inline-flex items-center justify-center gap-1.5 px-3 py-2 sm:px-4 sm:py-2 border border-transparent rounded-lg shadow-sm text-xs sm:text-sm font-medium text-white bg-teal-600 hover:bg-teal-700 transition whitespace-nowrap shrink-0"
            >
              <Calendar className="w-4 h-4 shrink-0" />
              <span>Đặt Lịch Khám Ngay</span>
            </Link>

            <button
              title="Đăng nhập dành cho nhân viên / bệnh nhân có tài khoản"
              className="p-2 text-gray-500 hover:text-gray-700 rounded-lg hover:bg-gray-100 transition hidden sm:inline-flex shrink-0 items-center justify-center"
            >
              <User className="w-5 h-5" />
            </button>
          </div>
        </div>
      </div>
    </header>
  );
};
