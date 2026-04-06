using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourismCMS.Migrations
{
    /// <inheritdoc />
    public partial class RestoreOwnerIdToPOI : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OwnerId",
                table: "POIs",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "POIs");
        }
    }
}
