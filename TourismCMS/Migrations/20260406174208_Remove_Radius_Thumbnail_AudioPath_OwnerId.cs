using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourismCMS.Migrations
{
    /// <inheritdoc />
    public partial class Remove_Radius_Thumbnail_AudioPath_OwnerId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AudioPath",
                table: "POIs");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "POIs");

            migrationBuilder.DropColumn(
                name: "Radius",
                table: "POIs");

            migrationBuilder.DropColumn(
                name: "Thumbnail",
                table: "POIs");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "POIs",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "POIs",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AudioPath",
                table: "POIs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnerId",
                table: "POIs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "Radius",
                table: "POIs",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Thumbnail",
                table: "POIs",
                type: "varchar(500)",
                unicode: false,
                maxLength: 500,
                nullable: true);
        }
    }
}
