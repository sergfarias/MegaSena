using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MegaSena.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTabelaConcurso : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Concurso",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroConcurso = table.Column<int>(type: "int", nullable: false),
                    DataHoraSorteio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CidadeSorteio = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UfSorteio = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    DezenasSorteadas = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DezenasOrdemSorteio = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    QtdGanhadoresSena = table.Column<int>(type: "int", nullable: false),
                    QtdGanhadoresQuina = table.Column<int>(type: "int", nullable: false),
                    QtdGanhadoresQuadra = table.Column<int>(type: "int", nullable: false),
                    ValorPremioSena = table.Column<decimal>(type: "decimal(15,2)", precision: 15, scale: 2, nullable: true),
                    ValorPremioQuina = table.Column<decimal>(type: "decimal(15,2)", precision: 15, scale: 2, nullable: true),
                    ValorPremioQuadra = table.Column<decimal>(type: "decimal(15,2)", precision: 15, scale: 2, nullable: true),
                    ValorArrecadado = table.Column<decimal>(type: "decimal(15,2)", precision: 15, scale: 2, nullable: true),
                    Acumulado = table.Column<bool>(type: "bit", nullable: false),
                    ValorAcumulado = table.Column<decimal>(type: "decimal(15,2)", precision: 15, scale: 2, nullable: true),
                    ListaRateioPremioJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NumeroConcursoAnterior = table.Column<int>(type: "int", nullable: true),
                    ProximoConcurso = table.Column<int>(type: "int", nullable: true),
                    DataProximoConcurso = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DataImportacao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Concurso", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Concurso_NumeroConcurso",
                table: "Concurso",
                column: "NumeroConcurso",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Concurso");
        }
    }
}
