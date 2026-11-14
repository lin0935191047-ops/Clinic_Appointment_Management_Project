#!/usr/bin/env bash
# ==============================================================================
# CLINIC APPOINTMENT MANAGEMENT SYSTEM - DATABASE RESTORE SCRIPT (BASH / LINUX)
# ==============================================================================
set -euo pipefail

if [ "$#" -lt 1 ]; then
    echo "Usage: $0 <path_to_backup_file.dump> [--confirm]" >&2
    exit 1
fi

BACKUP_FILE="$1"
CONFIRM_FLAG="${2:-}"

POSTGRES_HOST="${POSTGRES_HOST:-localhost}"
POSTGRES_PORT="${POSTGRES_PORT:-5432}"
POSTGRES_DB="${POSTGRES_DB:-clinicappointmentdb}"
POSTGRES_USER="${POSTGRES_USER:-postgres}"

if [ ! -f "${BACKUP_FILE}" ]; then
    echo "[ERROR] Không tìm thấy file sao lưu: ${BACKUP_FILE}" >&2
    exit 1
fi

if [ "${CONFIRM_FLAG}" != "--confirm" ]; then
    echo "[WARNING] Thao tác này sẽ ghi đè cơ sở dữ liệu ${POSTGRES_DB}!" >&2
    echo "Vui lòng chạy lại kèm tham số --confirm để xác nhận." >&2
    exit 1
fi

echo "=========================================================="
echo "BẮT ĐẦU PHỤC HỒI CƠ SỞ DỮ LIỆU POSTGRESQL"
echo "Tệp sao lưu: ${BACKUP_FILE}"
echo "Cơ sở dữ liệu đích: ${POSTGRES_DB}"
echo "=========================================================="

pg_restore -h "${POSTGRES_HOST}" -p "${POSTGRES_PORT}" -U "${POSTGRES_USER}" -d "${POSTGRES_DB}" --clean --if-exists -v "${BACKUP_FILE}"

echo "Kiểm tra tính toàn vẹn..."
TABLE_COUNT=$(psql -h "${POSTGRES_HOST}" -p "${POSTGRES_PORT}" -U "${POSTGRES_USER}" -d "${POSTGRES_DB}" -t -c "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public';")
echo "[SUCCESS] Phục hồi thành công! Số bảng: $(echo ${TABLE_COUNT} | tr -d ' ')"
