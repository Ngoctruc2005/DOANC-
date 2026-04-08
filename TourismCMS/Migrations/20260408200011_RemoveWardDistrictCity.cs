using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourismCMS.Migrations
{
    /// <inheritdoc />
    public partial class RemoveWardDistrictCity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "City",
                table: "POIs");

            migrationBuilder.DropColumn(
                name: "District",
                table: "POIs");

            migrationBuilder.DropColumn(
                name: "Ward",
                table: "POIs");

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "POIs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "Radius",
                table: "POIs",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "POIs");

            migrationBuilder.DropColumn(
                name: "Radius",
                table: "POIs");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "POIs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "District",
                table: "POIs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ward",
                table: "POIs",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
