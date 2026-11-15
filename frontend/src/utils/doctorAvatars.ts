/**
 * Tiện ích quản lý ảnh đại diện bác sĩ (Doctor Avatars)
 * Cung cấp ảnh chân dung y tế chất lượng cao và ảnh vector cục bộ offline-first
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
    primaryUrl: '/images/doctors/dr-mai.jpg?v=2',
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

/**
 * Lấy URL ảnh đại diện tốt nhất cho bác sĩ
 */
export function getDoctorAvatar(doctor?: {
  id?: string;
  fullName?: string;
  doctorCode?: string;
  avatarUrl?: string;
} | null): string {
  if (!doctor) return DEFAULT_DOCTOR_AVATAR;

  // 1. Ưu tiên avatarUrl được gán trực tiếp nếu có (loại trừ URL lỗi cũ)
  if (doctor.avatarUrl && doctor.avatarUrl.trim() !== '' && !doctor.avatarUrl.includes('photo-1594824813627-2c98d6c708c9')) {
    return doctor.avatarUrl;
  }

  const code = (doctor.doctorCode || '').trim().toUpperCase();
  const name = (doctor.fullName || '').trim().toLowerCase();
  const id = (doctor.id || '').trim().toLowerCase();

  // 2. Tra cứu theo mã bác sĩ
  if (code) {
    const match = DOCTOR_AVATARS.find((d) => d.code === code);
    if (match) return match.primaryUrl;
  }

  // 3. Tra cứu theo ID mock (doc-1 -> Dr. An, doc-2 -> Dr. Mai, ...)
  if (id === 'doc-1') return DOCTOR_AVATARS[0].primaryUrl;
  if (id === 'doc-2') return DOCTOR_AVATARS[1].primaryUrl;
  if (id === 'doc-3') return DOCTOR_AVATARS[2].primaryUrl;
  if (id === 'doc-4') return DOCTOR_AVATARS[3].primaryUrl;
  if (id === 'doc-5') return DOCTOR_AVATARS[4].primaryUrl;
  if (id === 'doc-6') return DOCTOR_AVATARS[5].primaryUrl;

  // 4. Tra cứu theo tên
  if (name) {
    if (name.includes('an')) return DOCTOR_AVATARS[0].primaryUrl;
    if (name.includes('mai')) return DOCTOR_AVATARS[1].primaryUrl;
    if (name.includes('nam')) return DOCTOR_AVATARS[2].primaryUrl;
    if (name.includes('trang')) return DOCTOR_AVATARS[3].primaryUrl;
    if (name.includes('minh')) return DOCTOR_AVATARS[4].primaryUrl;
    if (name.includes('tuấn') || name.includes('tuan')) return DOCTOR_AVATARS[5].primaryUrl;
  }

  // 5. Mặc định
  return DEFAULT_DOCTOR_AVATAR;
}

/**
 * Lấy ảnh SVG vector dự phòng khi trình duyệt gặp lỗi tải ảnh online (offline mode)
 */
export function getDoctorLocalFallback(doctor?: {
  id?: string;
  fullName?: string;
  doctorCode?: string;
} | null): string {
  if (!doctor) return DEFAULT_DOCTOR_AVATAR;

  const code = (doctor.doctorCode || '').trim().toUpperCase();
  const name = (doctor.fullName || '').trim().toLowerCase();
  const id = (doctor.id || '').trim().toLowerCase();

  if (code) {
    const match = DOCTOR_AVATARS.find((d) => d.code === code);
    if (match) return match.localSvg;
  }

  if (id === 'doc-1' || name.includes('an')) return DOCTOR_AVATARS[0].localSvg;
  if (id === 'doc-2' || name.includes('mai')) return DOCTOR_AVATARS[1].localSvg;
  if (id === 'doc-3' || name.includes('nam')) return DOCTOR_AVATARS[2].localSvg;
  if (id === 'doc-4' || name.includes('trang')) return DOCTOR_AVATARS[3].localSvg;
  if (id === 'doc-5' || name.includes('minh')) return DOCTOR_AVATARS[4].localSvg;
  if (id === 'doc-6' || name.includes('tuấn') || name.includes('tuan')) return DOCTOR_AVATARS[5].localSvg;

  return DEFAULT_DOCTOR_AVATAR;
}
