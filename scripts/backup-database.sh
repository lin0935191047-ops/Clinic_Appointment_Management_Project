#!/usr/bin/env bash
# ==============================================================================
# CLINIC APPOINTMENT MANAGEMENT SYSTEM - DATABASE BACKUP SCRIPT (BASH / LINUX)
# ==============================================================================
set -euo pipefail

BACKUP_DIR="${BACKUP_DIR:-database/backups}"
RETENTION_DAYS="${RETENTION_DAYS:-14}"
POSTGRES_HOST="${POSTGRES_HOST:-localhost}"
POSTGRES_PORT="${POSTGRES_PORT:-5432}"
POSTGRES_DB="${POSTGRES_DB:-clinicappointmentdb}"
POSTGRES_USER="${POSTGRES_USER:-postgres}"

mkdir -p "${BACKUP_DIR}"

TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
BACKUP_FILE="${BACKUP_DIR}/clinic_backup_${TIMESTAMP}.dump"

echo "=========================================================="
echo "BẮT ĐẦU SAO LƯU CƠ SỞ DỮ LIỆU POSTGRESQL (CLINIC SYSTEM)"
echo "Thời gian: $(date +"%Y-%m-%d %H:%M:%S")"
echo "Database: ${POSTGRES_DB} | Host: ${POSTGRES_HOST}:${POSTGRES_PORT}"
echo "File đích: ${BACKUP_FILE}"
echo "=========================================================="

pg_dump -h "${POSTGRES_HOST}" -p "${POSTGRES_PORT}" -U "${POSTGRES_USER}" -Fc -b -v -f "${BACKUP_FILE}" "${POSTGRES_DB}"

if [ -f "${BACKUP_FILE}" ]; then
    FILE_SIZE=$(du -h "${BACKUP_FILE}" | cut -f1)
    echo "[SUCCESS] Sao lưu thành công! Kích thước: ${FILE_SIZE}"
else
    echo "[ERROR] Không tìm thấy file sao lưu!" >&2
    exit 1
fi

echo "Dọn dẹp bản sao lưu cũ hơn ${RETENTION_DAYS} ngày..."
find "${BACKUP_DIR}" -name "clinic_backup_*.dump" -type f -mtime +"${RETENTION_DAYS}" -exec rm -f {} +

echo "=========================================================="
echo "QUY TRÌNH SAO LƯU HOÀN TẤT!"
