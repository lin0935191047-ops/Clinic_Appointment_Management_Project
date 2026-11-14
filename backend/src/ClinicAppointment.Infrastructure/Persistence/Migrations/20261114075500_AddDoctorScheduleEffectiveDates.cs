using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicAppointment.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDoctorScheduleEffectiveDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "effective_from",
                table: "doctor_schedules",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(2026, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "effective_to",
                table: "doctor_schedules",
                type: "date",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_doctor_schedules_doctor_id_day_of_week_is_active",
                table: "doctor_schedules",
                columns: new[] { "doctor_id", "day_of_week", "is_active" });

            migrationBuilder.AddCheckConstraint(
                name: "chk_doctor_schedules_day_of_week",
                table: "doctor_schedules",
                sql: "day_of_week >= 1 AND day_of_week <= 7");

            migrationBuilder.AddCheckConstraint(
                name: "chk_doctor_schedules_time_order",
                table: "doctor_schedules",
                sql: "start_time < end_time");

            migrationBuilder.AddCheckConstraint(
                name: "chk_doctor_schedules_effective_date_order",
                table: "doctor_schedules",
                sql: "effective_to IS NULL OR effective_to >= effective_from");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "chk_doctor_schedules_effective_date_order",
                table: "doctor_schedules");

            migrationBuilder.DropCheckConstraint(
                name: "chk_doctor_schedules_time_order",
                table: "doctor_schedules");

            migrationBuilder.DropCheckConstraint(
                name: "chk_doctor_schedules_day_of_week",
                table: "doctor_schedules");

            migrationBuilder.DropIndex(
                name: "ix_doctor_schedules_doctor_id_day_of_week_is_active",
                table: "doctor_schedules");

            migrationBuilder.DropColumn(
                name: "effective_to",
                table: "doctor_schedules");

            migrationBuilder.DropColumn(
                name: "effective_from",
                table: "doctor_schedules");
        }
    }
}
