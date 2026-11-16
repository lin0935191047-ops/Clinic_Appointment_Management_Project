/**
 * Tiện ích quản lý ảnh đại diện bác sĩ (Doctor Avatars)
 * Cung cấp ảnh chân dung y tế chuẩn và ảnh vector cục bộ offline-first cho từng bác sĩ
 */

export interface DoctorAvatarInfo {
  code: string;
  nameKey: string;
  primaryUrl: string; // Ảnh chân dung y tế chất lượng cao
  localSvg: string;   // Ảnh SVG vector cục bộ (không bao giờ lỗi kể cả khi offline)
}

export const DOCTOR_AVATARS: DoctorAvatarInfo[] = [
  {
    code: 'DOC-INT-01',
    nameKey: 'nguyễn văn an',
    primaryUrl: '/images/doctors/dr-an.jpg',
    localSvg: '/images/doctors/dr-an.svg',
  },
  {
    code: 'DOC-PED-01',
    nameKey: 'trần thị mai',
    primaryUrl: '/images/doctors/dr-mai.jpg',
    localSvg: '/images/doctors/dr-mai.svg',
  },
  {
    code: 'DOC-CARD-01',
    nameKey: 'lê hoàng nam',
    primaryUrl: '/images/doctors/dr-nam.jpg',
    localSvg: '/images/doctors/dr-nam.svg',
  },
  {
    code: 'DOC-ENT-01',
    nameKey: 'phạm thu trang',
    primaryUrl: '/images/doctors/dr-trang.jpg',
    localSvg: '/images/doctors/dr-trang.svg',
  },
  {
    code: 'DOC-DENT-01',
    nameKey: 'vũ đức minh',
    primaryUrl: '/images/doctors/dr-minh.jpg',
    localSvg: '/images/doctors/dr-minh.svg',
  },
  {
    code: 'DOC-GIST-01',
    nameKey: 'đặng minh tuấn',
    primaryUrl: '/images/doctors/dr-tuan.jpg',
    localSvg: '/images/doctors/dr-tuan.svg',
  },
];

export const DEFAULT_DOCTOR_AVATAR = '/images/doctors/default-doctor.svg';

export interface DoctorLookupParam {
  id?: string;
  fullName?: string;
  doctorCode?: string;
  avatarUrl?: string;
  email?: string;
  specialtyName?: string;
}

/**
 * Xác định chính xác chỉ số của bác sĩ trong hệ thống
 */
function findDoctorIndex(doctor?: DoctorLookupParam | null): number {
  if (!doctor) return -1;

  const code = (doctor.doctorCode || '').trim().toUpperCase();
  const email = (doctor.email || '').trim().toLowerCase();
  const id = (doctor.id || '').trim().toLowerCase();
  const name = (doctor.fullName || '').trim().toLowerCase();
  const specialty = (doctor.specialtyName || '').trim().toLowerCase();

  // 1. Tra cứu theo Mã Bác sĩ (DoctorCode) - Độ chính xác 100%
  if (code === 'DOC-INT-01') return 0;
  if (code === 'DOC-PED-01') return 1;
  if (code === 'DOC-CARD-01') return 2;
  if (code === 'DOC-ENT-01') return 3;
  if (code === 'DOC-DENT-01') return 4;
  if (code === 'DOC-GIST-01') return 5;

  // 2. Tra cứu theo Email tài khoản Bác sĩ - Độ chính xác 100%
  if (email.includes('dr.trang') || email.includes('doctor.trang')) return 3;
  if (email.includes('dr.tuan') || email.includes('doctor.tuan')) return 5;
  if (email.includes('dr.mai') || email.includes('doctor.mai')) return 1;
  if (email.includes('dr.nam') || email.includes('doctor.nam')) return 2;
  if (email.includes('dr.minh') || email.includes('doctor.minh')) return 4;
  if (email.includes('dr.an') || email.includes('doctor.an')) return 0;

  // 3. Tra cứu theo ID Mock
  if (id === 'doc-1') return 0;
  if (id === 'doc-2') return 1;
  if (id === 'doc-3') return 2;
  if (id === 'doc-4') return 3;
  if (id === 'doc-5') return 4;
  if (id === 'doc-6') return 5;

  // 4. Tra cứu theo Tên Bác sĩ (Xử lý thứ tự ưu tiên chặt chẽ)
  if (name) {
    // 4.1. BS. Phạm Thu Trang (Tai Mũi Họng) -> 'trang' (Phải xét trước 'an' vì 'trang' chứa ký tự 'an')
    if (name.includes('trang')) return 3;

    // 4.2. BS. Đặng Minh Tuấn (Nội thần kinh - CXK) -> 'tuấn' / 'tuan' (Phải xét trước 'minh' vì chứa chữ 'minh')
    if (name.includes('tuấn') || name.includes('tuan')) return 5;

    // 4.3. ThS. BS Trần Thị Mai (Nhi Khoa)
    if (name.includes('mai')) return 1;

    // 4.4. TS. BS Lê Hoàng Nam (Tim Mạch)
    if (name.includes('nam')) return 2;

    // 4.5. BS. Vũ Đức Minh (Răng Hàm Mặt)
    if (name.includes('minh')) return 4;

    // 4.6. BS. CKI Nguyễn Văn An (Nội Tổng Quát)
    // CHỈ match khi là tên 'An' độc lập, tránh nhầm các tên có chứa 'an'
    if (
      /\b(an|văn an|van an)\b/i.test(name) ||
      name.endsWith(' an') ||
      name.includes('nguyễn văn an') ||
      name.includes('nguyen van an')
    ) {
      return 0;
    }
  }

  // 5. Tra cứu theo Chuyên khoa liên kết
  if (specialty) {
    if (specialty.includes('tai mũi họng') || specialty.includes('ent')) return 3;
    if (
      specialty.includes('thần kinh') ||
      specialty.includes('xương khớp') ||
      specialty.includes('huyết học') ||
      specialty.includes('neuro') ||
      specialty.includes('gist')
    ) {
      return 5;
    }
    if (specialty.includes('nhi') || specialty.includes('ped')) return 1;
    if (specialty.includes('tim') || specialty.includes('card')) return 2;
    if (specialty.includes('răng') || specialty.includes('nha') || specialty.includes('dent')) return 4;
    if (specialty.includes('nội') || specialty.includes('int_med')) return 0;
  }

  return -1;
}

/**
 * Lấy URL ảnh chân dung y tế chuẩn cho bác sĩ
 */
export function getDoctorAvatar(doctor?: DoctorLookupParam | null): string {
  if (!doctor) return DEFAULT_DOCTOR_AVATAR;

  // 1. Ưu tiên avatarUrl được gán trực tiếp nếu hợp lệ (loại trừ URL lỗi cũ)
  if (
    doctor.avatarUrl &&
    doctor.avatarUrl.trim() !== '' &&
    !doctor.avatarUrl.includes('photo-1594824813627-2c98d6c708c9')
  ) {
    return doctor.avatarUrl;
  }

  // 2. Tra cứu ảnh theo thông tin bác sĩ
  const idx = findDoctorIndex(doctor);
  if (idx >= 0 && idx < DOCTOR_AVATARS.length) {
    return DOCTOR_AVATARS[idx].primaryUrl;
  }

  // 3. Mặc định
  return DEFAULT_DOCTOR_AVATAR;
}

/**
 * Lấy ảnh SVG vector dự phòng khi trình duyệt gặp lỗi tải ảnh online (offline-first)
 */
export function getDoctorLocalFallback(doctor?: DoctorLookupParam | null): string {
  if (!doctor) return DEFAULT_DOCTOR_AVATAR;

  const idx = findDoctorIndex(doctor);
  if (idx >= 0 && idx < DOCTOR_AVATARS.length) {
    return DOCTOR_AVATARS[idx].localSvg;
  }

  return DEFAULT_DOCTOR_AVATAR;
}
