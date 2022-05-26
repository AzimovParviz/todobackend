using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotesMinimalAPI.Migrations
{
    public partial class newtodomigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "done",
                table: "Notes");

            migrationBuilder.AddColumn<DateTime>(
                name: "created",
                table: "Notes",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "Notes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "Notes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated",
                table: "Notes",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "userId",
                table: "Notes",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "name",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "status",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "updated",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "userId",
                table: "Notes");

            migrationBuilder.AddColumn<bool>(
                name: "done",
                table: "Notes",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
