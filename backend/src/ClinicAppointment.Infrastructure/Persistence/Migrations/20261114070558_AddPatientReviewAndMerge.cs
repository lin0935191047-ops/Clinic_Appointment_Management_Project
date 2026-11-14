using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicAppointment.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientReviewAndMerge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_patients_users_user_id",
                table: "patients");

            migrationBuilder.AlterColumn<string>(
                name: "gender",
                table: "patients",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "normalized_full_name",
                table: "patients",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "record_status",
                table: "patients",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "ACTIVE");

            migrationBuilder.CreateTable(
                name: "patient_identity_review_cases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    candidate_patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    match_reason = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    reviewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    review_note = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_patient_identity_review_cases", x => x.id);
                    table.CheckConstraint("ck_identity_review_distinct_patients", "patient_id <> candidate_patient_id");
                    table.ForeignKey(
                        name: "fk_patient_identity_review_cases_patients_candidate_patient_id",
                        column: x => x.candidate_patient_id,
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_patient_identity_review_cases_patients_patient_id",
                        column: x => x.patient_id,
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_patient_identity_review_cases_users_reviewed_by_user_id",
                        column: x => x.reviewed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "patient_merge_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    merged_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    merged_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    before_snapshot_json = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_patient_merge_events", x => x.id);
                    table.CheckConstraint("ck_patient_merge_distinct_patients", "source_patient_id <> target_patient_id");
                    table.ForeignKey(
                        name: "fk_patient_merge_events_patients_source_patient_id",
                        column: x => x.source_patient_id,
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_patient_merge_events_patients_target_patient_id",
                        column: x => x.target_patient_id,
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_patient_merge_events_users_merged_by_user_id",
                        column: x => x.merged_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_patients_merged_into_patient_id",
                table: "patients",
                column: "merged_into_patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_patients_normalized_full_name",
                table: "patients",
                column: "normalized_full_name");

            migrationBuilder.AddCheckConstraint(
                name: "ck_patients_birth_year_matches_dob",
                table: "patients",
                sql: "date_of_birth IS NULL OR birth_year IS NULL OR birth_year = EXTRACT(YEAR FROM date_of_birth)::smallint");

            migrationBuilder.CreateIndex(
                name: "ix_patient_identity_review_cases_candidate_patient_id",
                table: "patient_identity_review_cases",
                column: "candidate_patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_patient_identity_review_cases_patient_id_candidate_patient_",
                table: "patient_identity_review_cases",
                columns: new[] { "patient_id", "candidate_patient_id", "match_reason" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_patient_identity_review_cases_reviewed_by_user_id",
                table: "patient_identity_review_cases",
                column: "reviewed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_patient_merge_events_merged_by_user_id",
                table: "patient_merge_events",
                column: "merged_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_patient_merge_events_source_patient_id",
                table: "patient_merge_events",
                column: "source_patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_patient_merge_events_target_patient_id",
                table: "patient_merge_events",
                column: "target_patient_id");

            migrationBuilder.AddForeignKey(
                name: "fk_patients_patients_merged_into_patient_id",
                table: "patients",
                column: "merged_into_patient_id",
                principalTable: "patients",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_patients_users_user_id",
                table: "patients",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_patients_patients_merged_into_patient_id",
                table: "patients");

            migrationBuilder.DropForeignKey(
                name: "fk_patients_users_user_id",
                table: "patients");

            migrationBuilder.DropTable(
                name: "patient_identity_review_cases");

            migrationBuilder.DropTable(
                name: "patient_merge_events");

            migrationBuilder.DropIndex(
                name: "ix_patients_merged_into_patient_id",
                table: "patients");

            migrationBuilder.DropIndex(
                name: "ix_patients_normalized_full_name",
                table: "patients");

            migrationBuilder.DropCheckConstraint(
                name: "ck_patients_birth_year_matches_dob",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "normalized_full_name",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "record_status",
                table: "patients");

            migrationBuilder.AlterColumn<string>(
                name: "gender",
                table: "patients",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30,
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_patients_users_user_id",
                table: "patients",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id");
        }
    }
}
