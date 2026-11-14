import React from 'react';
import { Link } from 'react-router-dom';
import { Calendar, Search, ShieldCheck, Clock, Award, Users, ArrowRight, HeartPulse } from 'lucide-react';
import { mockSpecialties } from '../api/client';

export const HomePage: React.FC = () => {
  return (
    <div className="space-y-16 pb-12">
      {/* Hero Section */}
      <section className="relative overflow-hidden bg-gradient-to-br from-teal-900 via-teal-800 to-gray-900 text-white py-20 px-4 sm:px-6 lg:px-8 rounded-b-3xl shadow-xl">
        <div className="max-w-7xl mx-auto grid grid-cols-1 lg:grid-cols-12 gap-12 items-center">
          <div className="lg:col-span-7 space-y-6">
            <div className="inline-flex items-center space-x-2 px-3 py-1 bg-teal-700/50 rounded-full border border-teal-500/30 text-teal-200 text-xs font-medium">
              <ShieldCheck className="w-4 h-4 text-teal-300" />
              <span>Hệ thống Đặt Lịch Thông Minh v1.2</span>
            </div>

            <h1 className="text-4xl sm:text-5xl font-extrabold tracking-tight leading-tight">
              Chăm Sóc Sức Khỏe Toàn Diện <br />
              <span className="text-teal-300">Đặt Lịch Nhanh Chóng Không Cần Tạo Tài Khoản</span>
            </h1>

            <p className="text-lg text-gray-200 max-w-2xl leading-relaxed">
              Chủ động lựa chọn bác sĩ chuyên khoa, dịch vụ và khung giờ khám mong muốn. Guest Booking tiện lợi, bảo mật tuyệt đối qua OTP và chính sách hoàn phí minh bạch.
            </p>

            <div className="flex flex-col sm:flex-row gap-4 pt-2">
              <Link
                to="/booking"
                className="inline-flex items-center justify-center px-6 py-3.5 border border-transparent rounded-xl shadow-lg text-base font-semibold text-teal-950 bg-teal-400 hover:bg-teal-300 transition duration-150 transform hover:-translate-y-0.5"
              >
                <Calendar className="w-5 h-5 mr-2" />
                Đặt Lịch Khám Ngay
              </Link>

              <Link
                to="/lookup"
                className="inline-flex items-center justify-center px-6 py-3.5 border border-white/20 rounded-xl text-base font-semibold text-white bg-white/10 hover:bg-white/20 backdrop-blur transition duration-150"
              >
                <Search className="w-5 h-5 mr-2 text-teal-300" />
                Tra Cứu & Đổi/Hủy Lịch
              </Link>
            </div>

            {/* Invariant Highlights Badge */}
            <div className="pt-6 border-t border-teal-700/60 grid grid-cols-3 gap-4 text-xs text-teal-100">
              <div className="flex items-center space-x-2">
                <div className="w-2 h-2 rounded-full bg-emerald-400"></div>
                <span>Không cần đăng nhập</span>
              </div>
              <div className="flex items-center space-x-2">
                <div className="w-2 h-2 rounded-full bg-teal-300"></div>
                <span>Xác thực OTP chính chủ</span>
              </div>
              <div className="flex items-center space-x-2">
                <div className="w-2 h-2 rounded-full bg-blue-300"></div>
                <span>Hoàn 100% nếu hủy trong 60 phút</span>
              </div>
            </div>
          </div>

          <div className="lg:col-span-5 hidden lg:block">
            <div className="bg-white/10 backdrop-blur-md p-6 rounded-2xl border border-white/20 shadow-2xl space-y-4">
              <div className="flex items-center justify-between pb-4 border-b border-white/10">
                <div className="flex items-center space-x-3">
                  <div className="w-10 h-10 rounded-full bg-teal-400 text-teal-950 flex items-center justify-center font-bold">
                    BS
                  </div>
                  <div>
                    <p className="font-semibold text-sm">Bác Sĩ Trực Tuyến</p>
                    <p className="text-xs text-teal-200">5 chuyên khoa đang nhận lịch</p>
                  </div>
                </div>
                <span className="px-2.5 py-1 bg-emerald-500/20 text-emerald-300 text-xs font-medium rounded-full border border-emerald-400/30">
                  Sẵn sàng
                </span>
              </div>

              <div className="space-y-3 text-xs text-gray-200">
                <div className="p-3 bg-white/5 rounded-xl border border-white/5 flex justify-between items-center">
                  <span>Khung giờ khả dụng hôm nay</span>
                  <span className="font-bold text-teal-300">32 suất khám</span>
                </div>
                <div className="p-3 bg-white/5 rounded-xl border border-white/5 flex justify-between items-center">
                  <span>Thời gian khám trung bình</span>
                  <span className="font-bold text-teal-300">30 phút / ca</span>
                </div>
                <div className="p-3 bg-white/5 rounded-xl border border-white/5 flex justify-between items-center">
                  <span>Phê duyệt đổi lịch bởi Lễ tân</span>
                  <span className="font-bold text-teal-300">Giữ slot an toàn</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* 4 Steps Section */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="text-center max-w-2xl mx-auto mb-12">
          <h2 className="text-2xl sm:text-3xl font-bold text-gray-900 tracking-tight">
            Quy Trình Đặt Lịch Khám 4 Bước Đơn Giản
          </h2>
          <p className="text-gray-600 mt-2 text-sm">
            Thuận tiện cho mọi người dân, không cần cài đặt ứng dụng phức tạp hay đăng ký tài khoản.
          </p>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
          <div className="bg-white p-6 rounded-2xl shadow-sm border border-gray-100 hover:shadow-md transition">
            <div className="w-12 h-12 rounded-xl bg-teal-50 text-teal-600 flex items-center justify-center font-bold text-lg mb-4">
              01
            </div>
            <h3 className="font-semibold text-gray-900 text-base mb-2">Chọn Chuyên Khoa</h3>
            <p className="text-gray-600 text-sm">
              Tìm bác sĩ và dịch vụ chuyên khoa phù hợp với nhu cầu sức khỏe của bạn hoặc người thân.
            </p>
          </div>

          <div className="bg-white p-6 rounded-2xl shadow-sm border border-gray-100 hover:shadow-md transition">
            <div className="w-12 h-12 rounded-xl bg-teal-50 text-teal-600 flex items-center justify-center font-bold text-lg mb-4">
              02
            </div>
            <h3 className="font-semibold text-gray-900 text-base mb-2">Chọn Khung Giờ</h3>
            <p className="text-gray-600 text-sm">
              Xem lịch trống thời gian thực của bác sĩ. Không lo bị trùng lịch nhờ kiểm tra tự động.
            </p>
          </div>

          <div className="bg-white p-6 rounded-2xl shadow-sm border border-gray-100 hover:shadow-md transition">
            <div className="w-12 h-12 rounded-xl bg-teal-50 text-teal-600 flex items-center justify-center font-bold text-lg mb-4">
              03
            </div>
            <h3 className="font-semibold text-gray-900 text-base mb-2">Điền Thông Tin Bệnh Nhân</h3>
            <p className="text-gray-600 text-sm">
              Chỉ cần Họ tên + Ngày sinh hoặc Năm sinh + Số điện thoại nhận OTP xác nhận.
            </p>
          </div>

          <div className="bg-white p-6 rounded-2xl shadow-sm border border-gray-100 hover:shadow-md transition">
            <div className="w-12 h-12 rounded-xl bg-teal-50 text-teal-600 flex items-center justify-center font-bold text-lg mb-4">
              04
            </div>
            <h3 className="font-semibold text-gray-900 text-base mb-2">Nhận Mã Đặt Lịch</h3>
            <p className="text-gray-600 text-sm">
              Nhận ngay BookingCode duy nhất để tra cứu, đổi lịch hoặc hủy lịch linh hoạt.
            </p>
          </div>
        </div>
      </section>

      {/* Specialties Highlight */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex justify-between items-end mb-8">
          <div>
            <h2 className="text-2xl font-bold text-gray-900">Chuyên Khoa Nổi Bật</h2>
            <p className="text-gray-500 text-sm mt-1">Đội ngũ y bác sĩ giàu chuyên môn với trang thiết bị hiện đại</p>
          </div>
          <Link to="/doctors" className="text-teal-600 hover:text-teal-700 text-sm font-semibold flex items-center">
            Xem tất cả bác sĩ <ArrowRight className="w-4 h-4 ml-1" />
          </Link>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
          {mockSpecialties.map((spec) => (
            <div
              key={spec.id}
              className="bg-white p-6 rounded-2xl border border-gray-100 shadow-sm hover:shadow-md hover:border-teal-200 transition group flex flex-col justify-between"
            >
              <div>
                <div className="w-10 h-10 rounded-xl bg-teal-50 text-teal-600 flex items-center justify-center mb-4 group-hover:bg-teal-600 group-hover:text-white transition">
                  <HeartPulse className="w-5 h-5" />
                </div>
                <h3 className="font-bold text-gray-900 text-lg group-hover:text-teal-600 transition">
                  {spec.name}
                </h3>
                <p className="text-gray-600 text-sm mt-2 line-clamp-2">{spec.description}</p>
              </div>

              <div className="mt-6 pt-4 border-t border-gray-100 flex items-center justify-between">
                <span className="text-xs text-gray-500 font-mono">MÃ: {spec.code}</span>
                <Link
                  to={`/booking?specialtyId=${spec.id}`}
                  className="text-xs font-semibold text-teal-600 hover:text-teal-700 flex items-center"
                >
                  Đặt chuyên khoa này <ArrowRight className="w-3.5 h-3.5 ml-1" />
                </Link>
              </div>
            </div>
          ))}
        </div>
      </section>

      {/* Policy and Guarantee Banner */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="bg-teal-50 border border-teal-200 rounded-3xl p-8 sm:p-10">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
            <div className="flex items-start space-x-4">
              <div className="p-3 bg-teal-600 text-white rounded-xl">
                <Clock className="w-6 h-6" />
              </div>
              <div>
                <h4 className="font-bold text-gray-900">Hoàn Phí Rõ Ràng</h4>
                <p className="text-sm text-gray-600 mt-1">
                  Hủy từ 60 phút trở lên: hoàn 100% phí dịch vụ. Hủy dưới 60 phút: hoàn 50%.
                </p>
              </div>
            </div>

            <div className="flex items-start space-x-4">
              <div className="p-3 bg-teal-600 text-white rounded-xl">
                <Award className="w-6 h-6" />
              </div>
              <div>
                <h4 className="font-bold text-gray-900">Bác Sĩ Chuyên Sâu</h4>
                <p className="text-sm text-gray-600 mt-1">
                  Đội ngũ bác sĩ CKI, ThS, TS đầu ngành, lịch khám trực quan, không chồng chéo ca.
                </p>
              </div>
            </div>

            <div className="flex items-start space-x-4">
              <div className="p-3 bg-teal-600 text-white rounded-xl">
                <Users className="w-6 h-6" />
              </div>
              <div>
                <h4 className="font-bold text-gray-900">Độc Lập Dữ Liệu</h4>
                <p className="text-sm text-gray-600 mt-1">
                  Hồ sơ bệnh nhân độc lập, không ép tạo User, bảo vệ định danh y tế theo chuẩn.
                </p>
              </div>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
};
