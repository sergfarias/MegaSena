using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MegaSena.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedPerfilAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Perfil",
                columns: new[] { "Id", "Ativo", "DataCriacao", "Descricao", "Sigla" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000001"), true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Administrador", "Admin" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Perfil",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"));
        }
    }
}
