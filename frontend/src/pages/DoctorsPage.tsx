import React, { useState, useEffect } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { catalogApi, mockSpecialties, mockDoctors } from '../api/client';
import { Doctor, Specialty } from '../types';
import { getDoctorAvatar, getDoctorLocalFallback } from '../utils/doctorAvatars';
import { Calendar, Award, MapPin, DollarSign, Search } from 'lucide-react';

export const DoctorsPage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const [specialties, setSpecialties] = useState<Specialty[]>(mockSpecialties);
  const [doctors, setDoctors] = useState<Doctor[]>(mockDoctors);
  const [selectedSpecialty, setSelectedSpecialty] = useState<string>(searchParams.get('specialtyId') || 'all');
  const [searchQuery, setSearchQuery] = useState<string>('');
  const [loading, setLoading] = useState<boolean>(false);

  useEffect(() => {
    const fetchData = async () => {
      setLoading(true);
      try {
        const [specs, docs] = await Promise.all([
          catalogApi.getSpecialties(),
          catalogApi.getDoctors(selectedSpecialty !== 'all' ? selectedSpecialty : undefined),
        ]);
        setSpecialties(specs);
        setDoctors(docs);
      } catch (err) {
        console.error('Lỗi tải danh mục:', err);
      } finally {
        setLoading(false);
      }
    };
    fetchData();
  }, [selectedSpecialty]);

  const filteredDoctors = doctors.filter((doc) => {
    const matchesSearch =
      doc.fullName.toLowerCase().includes(searchQuery.toLowerCase()) ||
      doc.specialtyName.toLowerCase().includes(searchQuery.toLowerCase());
    return matchesSearch;
  });

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-10 space-y-8">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-extrabold text-gray-900">Đội Ngũ Bác Sĩ Chuyên Khoa</h1>
        <p className="text-gray-600 mt-2 text-sm">
          Đội ngũ chuyên gia y tế giàu kinh nghiệm, tận tâm vì sức khỏe người bệnh.
        </p>
      </div>

      {/* Filters */}
      <div className="bg-white p-4 sm:p-6 rounded-2xl border border-gray-200 shadow-sm flex flex-col md:flex-row gap-4 justify-between items-center">
        {/* Search */}
        <div className="relative w-full md:w-80">
          <Search className="w-4 h-4 absolute left-3 top-3 text-gray-400" />
          <input
            type="text"
            placeholder="Tìm theo tên bác sĩ..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="w-full pl-9 pr-4 py-2 border border-gray-300 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-teal-500 focus:border-teal-500"
          />
        </div>

        {/* Specialty filter buttons */}
        <div className="flex flex-wrap gap-2 w-full md:w-auto">
          <button
            onClick={() => setSelectedSpecialty('all')}
            className={`px-3 py-1.5 rounded-lg text-xs font-semibold transition ${
              selectedSpecialty === 'all'
                ? 'bg-teal-600 text-white'
                : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
            }`}
          >
            Tất cả chuyên khoa
          </button>
          {specialties.map((spec) => (
            <button
              key={spec.id}
              onClick={() => setSelectedSpecialty(spec.id)}
              className={`px-3 py-1.5 rounded-lg text-xs font-semibold transition ${
                selectedSpecialty === spec.id
                  ? 'bg-teal-600 text-white'
                  : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
              }`}
            >
              {spec.name}
            </button>
          ))}
        </div>
      </div>

      {/* Doctors Grid */}
      {loading ? (
        <div className="text-center py-12 text-gray-500">Đang tải danh sách bác sĩ...</div>
      ) : filteredDoctors.length === 0 ? (
        <div className="text-center py-12 bg-white rounded-2xl border border-gray-100 text-gray-500">
          Không tìm thấy bác sĩ phù hợp với tiêu chí tìm kiếm.
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {filteredDoctors.map((doc) => (
            <div
              key={doc.id}
              className="bg-white rounded-2xl border border-gray-200 shadow-sm overflow-hidden hover:shadow-md transition flex flex-col justify-between"
            >
              <div className="p-6 space-y-4">
                <div className="flex items-start space-x-4">
                  <div className="relative w-16 h-16 rounded-2xl overflow-hidden bg-teal-50 border-2 border-teal-100 flex-shrink-0 shadow-sm">
                    <img
                      src={doc.avatarUrl || getDoctorAvatar(doc)}
                      alt={doc.fullName}
                      className="w-full h-full object-cover"
                      loading="lazy"
                      onError={(e) => {
                        const target = e.currentTarget;
                        target.onerror = null;
                        const fallback = getDoctorLocalFallback(doc);
                        target.src = fallback;
                      }}
                    />
                  </div>
                  <div className="space-y-1">
                    <span className="inline-block px-2.5 py-0.5 rounded-md text-xs font-semibold bg-teal-50 text-teal-700 border border-teal-200">
                      {doc.specialtyName}
                    </span>
                    <h3 className="font-bold text-gray-900 text-lg leading-snug">{doc.fullName}</h3>
                    <p className="text-xs text-gray-500 flex items-center">
                      <Award className="w-3.5 h-3.5 mr-1 text-teal-600" />
                      {doc.title}
                    </p>
                  </div>
                </div>

                {doc.bio && <p className="text-xs text-gray-600 leading-relaxed line-clamp-3">{doc.bio}</p>}

                <div className="pt-3 border-t border-gray-100 space-y-1 text-xs text-gray-500">
                  {doc.roomNumber && (
                    <div className="flex items-center">
                      <MapPin className="w-3.5 h-3.5 mr-1.5 text-gray-400" />
                      <span>Phòng khám: {doc.roomNumber}</span>
                    </div>
                  )}
                  <div className="flex items-center font-medium text-gray-800">
                    <DollarSign className="w-3.5 h-3.5 mr-1.5 text-teal-600" />
                    <span>
                      Giá khám:{' '}
                      <strong className="text-teal-700">
                        {new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(
                          doc.consultationFee || 150000
                        )}
                      </strong>
                    </span>
                  </div>
                </div>
              </div>

              <div className="p-4 bg-gray-50 border-t border-gray-100">
                <Link
                  to={`/booking?doctorId=${doc.id}&specialtyId=${doc.specialtyId}`}
                  className="w-full inline-flex items-center justify-center px-4 py-2.5 bg-teal-600 hover:bg-teal-700 text-white rounded-xl text-sm font-semibold transition"
                >
                  <Calendar className="w-4 h-4 mr-2" />
                  Đặt Lịch Với Bác Sĩ
                </Link>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};
