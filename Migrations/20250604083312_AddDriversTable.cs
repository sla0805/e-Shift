using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eShift.Migrations
{
    /// <inheritdoc />
    public partial class AddDriversTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DriverLicensenum",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "DriverName",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "DriverPhone",
                table: "Drivers");

            migrationBuilder.RenameColumn(
                name: "DriverId",
                table: "Drivers",
                newName: "Driver_Id");

            migrationBuilder.AddColumn<string>(
                name: "Driver_Licensenum",
                table: "Drivers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Driver_Name",
                table: "Drivers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Driver_Phone",
                table: "Drivers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Driver_Licensenum",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "Driver_Name",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "Driver_Phone",
                table: "Drivers");

            migrationBuilder.RenameColumn(
                name: "Driver_Id",
                table: "Drivers",
                newName: "DriverId");

            migrationBuilder.AddColumn<string>(
                name: "DriverLicensenum",
                table: "Drivers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DriverName",
                table: "Drivers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DriverPhone",
                table: "Drivers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
