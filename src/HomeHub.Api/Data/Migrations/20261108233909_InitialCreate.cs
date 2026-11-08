using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HomeHub.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "solar_readings",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    plant_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    inverter_sn = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    timestamp_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    power_w = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    energy_today_kwh = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                    energy_total_kwh = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_solar_readings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sync_log",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    synced_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    success = table.Column<bool>(type: "boolean", nullable: false),
                    records_saved = table.Column<int>(type: "integer", nullable: false),
                    error_message = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sync_log", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_solar_readings_plant_timestamp",
                table: "solar_readings",
                columns: new[] { "plant_id", "timestamp_utc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_solar_readings_timestamp",
                table: "solar_readings",
                column: "timestamp_utc",
                descending: new bool[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "solar_readings");

            migrationBuilder.DropTable(
                name: "sync_log");
        }
    }
}
