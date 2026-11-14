import React from 'react';
import { Link, useLocation } from 'react-router-dom';
import { Calendar, Search, Stethoscope, User, ShieldCheck } from 'lucide-react';

export const Navbar: React.FC = () => {
  const location = useLocation();

  const isActive = (path: string) => location.pathname === path;

  return (
    <header className="sticky top-0 z-50 bg-white border-b border-gray-200 shadow-sm">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex justify-between items-center h-16">
          {/* Logo & Brand */}
          <Link to="/" className="flex items-center space-x-3 group">
            <div className="w-10 h-10 rounded-xl bg-teal-600 flex items-center justify-center text-white shadow-md group-hover:bg-teal-700 transition">
              <Stethoscope className="w-6 h-6" />
            </div>
            <div>
              <span className="text-xl font-bold text-gray-900 tracking-tight block">
                Phòng Khám Đa Khoa
              </span>
              <span className="text-xs text-teal-600 font-medium tracking-wide uppercase">
                Clinic Care & Booking
              </span>
            </div>
          </Link>

          {/* Navigation Links */}
          <nav className="hidden md:flex items-center space-x-1 lg:space-x-3">
            <Link
              to="/"
              className={`px-3 py-2 rounded-lg text-sm font-medium transition ${
                isActive('/')
                  ? 'bg-teal-50 text-teal-700 font-semibold'
                  : 'text-gray-600 hover:text-gray-900 hover:bg-gray-100'
              }`}
            >
              Trang Chủ
            </Link>

            <Link
              to="/doctors"
              className={`px-3 py-2 rounded-lg text-sm font-medium transition ${
                isActive('/doctors')
                  ? 'bg-teal-50 text-teal-700 font-semibold'
                  : 'text-gray-600 hover:text-gray-900 hover:bg-gray-100'
              }`}
            >
              Đội Ngũ Bác Sĩ
            </Link>

            <Link
              to="/lookup"
              className={`flex items-center space-x-1 px-3 py-2 rounded-lg text-sm font-medium transition ${
                isActive('/lookup')
                  ? 'bg-teal-50 text-teal-700 font-semibold'
                  : 'text-gray-600 hover:text-gray-900 hover:bg-gray-100'
              }`}
            >
              <Search className="w-4 h-4 mr-1 text-gray-500" />
              <span>Tra Cứu Lịch Hẹn</span>
            </Link>

            <Link
              to="/receptionist"
              className={`flex items-center space-x-1 px-3 py-2 rounded-lg text-sm font-medium transition ${
                isActive('/receptionist')
                  ? 'bg-teal-50 text-teal-700 font-semibold'
                  : 'text-gray-600 hover:text-gray-900 hover:bg-gray-100'
              }`}
            >
              <ShieldCheck className="w-4 h-4 mr-1 text-teal-600" />
              <span>Kênh Lễ Tân</span>
            </Link>

            <Link
              to="/doctor"
              className={`flex items-center space-x-1 px-3 py-2 rounded-lg text-sm font-medium transition ${
                isActive('/doctor')
                  ? 'bg-blue-50 text-blue-700 font-semibold'
                  : 'text-gray-600 hover:text-gray-900 hover:bg-gray-100'
              }`}
            >
              <Stethoscope className="w-4 h-4 mr-1 text-blue-600" />
              <span>Kênh Bác Sĩ</span>
            </Link>
          </nav>

          {/* Action CTAs */}
          <div className="flex items-center space-x-3">
            <Link
              to="/booking"
              className="inline-flex items-center justify-center px-4 py-2 border border-transparent rounded-lg shadow-sm text-sm font-medium text-white bg-teal-600 hover:bg-teal-700 transition"
            >
              <Calendar className="w-4 h-4 mr-2" />
              Đặt Lịch Khám Ngay
            </Link>

            <button
              title="Đăng nhập dành cho nhân viên / bệnh nhân có tài khoản"
              className="p-2 text-gray-500 hover:text-gray-700 rounded-lg hover:bg-gray-100 transition hidden sm:inline-flex"
            >
              <User className="w-5 h-5" />
            </button>
          </div>
        </div>
      </div>
    </header>
  );
};
