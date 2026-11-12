using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeHub.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "energy_month_kwh",
                table: "solar_readings",
                type: "numeric(10,3)",
                precision: 10,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "device_model",
                table: "solar_readings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "nominal_power_w",
                table: "solar_readings",
                type: "numeric(10,1)",
                precision: 10,
                scale: 1,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "plant_name",
                table: "solar_readings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "energy_month_kwh", table: "solar_readings");
            migrationBuilder.DropColumn(name: "device_model", table: "solar_readings");
            migrationBuilder.DropColumn(name: "nominal_power_w", table: "solar_readings");
            migrationBuilder.DropColumn(name: "plant_name", table: "solar_readings");
        }
    }
}
