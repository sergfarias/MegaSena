using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MegaSena.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMensagemContato : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MensagemContato",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Mensagem = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DataEnvio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Lida = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MensagemContato", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MensagemContato_Email",
                table: "MensagemContato",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_MensagemContato_Lida",
                table: "MensagemContato",
                column: "Lida");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MensagemContato");
        }
    }
}
