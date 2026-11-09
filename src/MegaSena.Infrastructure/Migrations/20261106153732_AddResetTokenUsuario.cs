using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MegaSena.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddResetTokenUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResetToken",
                table: "Usuario",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResetTokenExpiraEm",
                table: "Usuario",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_ResetToken",
                table: "Usuario",
                column: "ResetToken");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Usuario_ResetToken",
                table: "Usuario");

            migrationBuilder.DropColumn(
                name: "ResetToken",
                table: "Usuario");

            migrationBuilder.DropColumn(
                name: "ResetTokenExpiraEm",
                table: "Usuario");
        }
    }
}
