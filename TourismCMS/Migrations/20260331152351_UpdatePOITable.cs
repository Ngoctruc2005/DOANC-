using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourismCMS.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePOITable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Id",
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
                name: "ImagePath",
                table: "POIs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AudioPath",
                table: "POIs",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Id",
                table: "POIs");

            migrationBuilder.DropColumn(
                name: "Radius",
                table: "POIs");

            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "POIs");

            migrationBuilder.DropColumn(
                name: "AudioPath",
                table: "POIs");
        }
    }
}
