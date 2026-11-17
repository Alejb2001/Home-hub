using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HomeHub.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWeatherReadings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "weather_readings",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    service_config_id = table.Column<long>(type: "bigint", nullable: false),
                    timestamp_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    temp_c = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    clouds_pct = table.Column<int>(type: "integer", nullable: false),
                    weather_id = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    humidity_pct = table.Column<int>(type: "integer", nullable: false),
                    wind_speed_ms = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weather_readings", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_weather_readings_service_timestamp",
                table: "weather_readings",
                columns: new[] { "service_config_id", "timestamp_utc" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "weather_readings");
        }
    }
}
