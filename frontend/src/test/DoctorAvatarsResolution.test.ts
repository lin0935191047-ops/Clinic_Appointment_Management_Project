import { describe, it, expect } from 'vitest';
import { getDoctorAvatar, getDoctorLocalFallback, DEFAULT_DOCTOR_AVATAR } from '../utils/doctorAvatars';

describe('Doctor Avatar & Identity Resolution Tests', () => {
  it('correctly maps BS. CKI Phạm Thu Trang to dr-trang.jpg (not dr-an.jpg)', () => {
    // Test by fullName
    expect(getDoctorAvatar({ fullName: 'BS. CKI Phạm Thu Trang' })).toBe('/images/doctors/dr-trang.jpg');
    // Test by email
    expect(getDoctorAvatar({ email: 'dr.trang@clinic.vn' })).toBe('/images/doctors/dr-trang.jpg');
    // Test by doctorCode
    expect(getDoctorAvatar({ doctorCode: 'DOC-ENT-01' })).toBe('/images/doctors/dr-trang.jpg');
    // Test fallback SVG
    expect(getDoctorLocalFallback({ fullName: 'BS. CKI Phạm Thu Trang' })).toBe('/images/doctors/dr-trang.svg');
  });

  it('correctly maps BS. CKI Nguyễn Văn An to dr-an.jpg', () => {
    expect(getDoctorAvatar({ fullName: 'BS. CKI Nguyễn Văn An' })).toBe('/images/doctors/dr-an.jpg');
    expect(getDoctorAvatar({ email: 'dr.an@clinic.vn' })).toBe('/images/doctors/dr-an.jpg');
    expect(getDoctorAvatar({ doctorCode: 'DOC-INT-01' })).toBe('/images/doctors/dr-an.jpg');
    expect(getDoctorLocalFallback({ fullName: 'BS. CKI Nguyễn Văn An' })).toBe('/images/doctors/dr-an.svg');
  });

  it('correctly maps ThS. BS Trần Thị Mai to dr-mai.jpg', () => {
    expect(getDoctorAvatar({ fullName: 'ThS. BS Trần Thị Mai' })).toBe('/images/doctors/dr-mai.jpg');
    expect(getDoctorAvatar({ email: 'dr.mai@clinic.vn' })).toBe('/images/doctors/dr-mai.jpg');
    expect(getDoctorAvatar({ doctorCode: 'DOC-PED-01' })).toBe('/images/doctors/dr-mai.jpg');
    expect(getDoctorLocalFallback({ fullName: 'ThS. BS Trần Thị Mai' })).toBe('/images/doctors/dr-mai.svg');
  });

  it('correctly maps TS. BS Lê Hoàng Nam to dr-nam.jpg', () => {
    expect(getDoctorAvatar({ fullName: 'TS. BS Lê Hoàng Nam' })).toBe('/images/doctors/dr-nam.jpg');
    expect(getDoctorAvatar({ email: 'dr.nam@clinic.vn' })).toBe('/images/doctors/dr-nam.jpg');
    expect(getDoctorAvatar({ doctorCode: 'DOC-CARD-01' })).toBe('/images/doctors/dr-nam.jpg');
    expect(getDoctorLocalFallback({ fullName: 'TS. BS Lê Hoàng Nam' })).toBe('/images/doctors/dr-nam.svg');
  });

  it('correctly maps BS. Vũ Đức Minh to dr-minh.jpg', () => {
    expect(getDoctorAvatar({ fullName: 'BS. Vũ Đức Minh' })).toBe('/images/doctors/dr-minh.jpg');
    expect(getDoctorAvatar({ email: 'dr.minh@clinic.vn' })).toBe('/images/doctors/dr-minh.jpg');
    expect(getDoctorAvatar({ doctorCode: 'DOC-DENT-01' })).toBe('/images/doctors/dr-minh.jpg');
    expect(getDoctorLocalFallback({ fullName: 'BS. Vũ Đức Minh' })).toBe('/images/doctors/dr-minh.svg');
  });

  it('correctly maps BS. CKII Đặng Minh Tuấn to dr-tuan.jpg (not dr-minh or dr-an)', () => {
    expect(getDoctorAvatar({ fullName: 'BS. CKII Đặng Minh Tuấn' })).toBe('/images/doctors/dr-tuan.jpg');
    expect(getDoctorAvatar({ email: 'dr.tuan@clinic.vn' })).toBe('/images/doctors/dr-tuan.jpg');
    expect(getDoctorAvatar({ doctorCode: 'DOC-GIST-01' })).toBe('/images/doctors/dr-tuan.jpg');
    expect(getDoctorLocalFallback({ fullName: 'BS. CKII Đặng Minh Tuấn' })).toBe('/images/doctors/dr-tuan.svg');
  });

  it('falls back to default avatar for unknown or admin', () => {
    expect(getDoctorAvatar(null)).toBe(DEFAULT_DOCTOR_AVATAR);
    expect(getDoctorAvatar({ fullName: 'Quản Trị Viên', email: 'admin@clinic.vn' })).toBe(DEFAULT_DOCTOR_AVATAR);
  });
});
