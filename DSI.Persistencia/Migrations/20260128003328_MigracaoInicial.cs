using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DSI.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class MigracaoInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Conexoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    TipoBanco = table.Column<int>(type: "INTEGER", nullable: false),
                    ModoConexao = table.Column<int>(type: "INTEGER", nullable: false),
                    StringConexaoCriptografada = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conexoes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Execucoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    JobId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IniciadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FinalizadoEm = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ResumoJson = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: false, defaultValue: "{}")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Execucoes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ConexaoOrigemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ConexaoDestinoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Modo = table.Column<int>(type: "INTEGER", nullable: false),
                    TamanhoLote = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1000),
                    PoliticaErro = table.Column<int>(type: "INTEGER", nullable: false),
                    EstrategiaConflito = table.Column<int>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ErrosExecucao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExecucaoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TabelaJobId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChaveLinha = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Coluna = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    TipoRegra = table.Column<int>(type: "INTEGER", nullable: true),
                    ValorOriginal = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Mensagem = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    OcorridoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrosExecucao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ErrosExecucao_Execucoes_ExecucaoId",
                        column: x => x.ExecucaoId,
                        principalTable: "Execucoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EstatisticasTabelas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExecucaoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TabelaJobId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LinhasLidas = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    LinhasInseridas = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    LinhasAtualizadas = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    LinhasPuladas = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    LinhasComErro = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    DuracaoMs = table.Column<long>(type: "INTEGER", nullable: false, defaultValue: 0L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstatisticasTabelas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EstatisticasTabelas_Execucoes_ExecucaoId",
                        column: x => x.ExecucaoId,
                        principalTable: "Execucoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TabelasJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    JobId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TabelaOrigem = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    TabelaDestino = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    OrdemExecucao = table.Column<int>(type: "INTEGER", nullable: false),
                    ColunaIncremental = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UltimoCheckpoint = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TabelasJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TabelasJobs_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Mapeamentos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TabelaJobId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ColunaOrigem = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ColunaDestino = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    TipoDestino = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Ignorada = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    ValorConstante = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mapeamentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Mapeamentos_TabelasJobs_TabelaJobId",
                        column: x => x.TabelaJobId,
                        principalTable: "TabelasJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Regras",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MapeamentoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TipoRegra = table.Column<int>(type: "INTEGER", nullable: false),
                    ParametrosJson = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false, defaultValue: "{}"),
                    AcaoQuandoFalhar = table.Column<int>(type: "INTEGER", nullable: false),
                    Ordem = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Regras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Regras_Mapeamentos_MapeamentoId",
                        column: x => x.MapeamentoId,
                        principalTable: "Mapeamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Conexoes_Nome",
                table: "Conexoes",
                column: "Nome");

            migrationBuilder.CreateIndex(
                name: "IX_ErrosExecucao_ExecucaoId",
                table: "ErrosExecucao",
                column: "ExecucaoId");

            migrationBuilder.CreateIndex(
                name: "IX_ErrosExecucao_OcorridoEm",
                table: "ErrosExecucao",
                column: "OcorridoEm");

            migrationBuilder.CreateIndex(
                name: "IX_ErrosExecucao_TabelaJobId",
                table: "ErrosExecucao",
                column: "TabelaJobId");

            migrationBuilder.CreateIndex(
                name: "IX_EstatisticasTabelas_ExecucaoId",
                table: "EstatisticasTabelas",
                column: "ExecucaoId");

            migrationBuilder.CreateIndex(
                name: "IX_EstatisticasTabelas_TabelaJobId",
                table: "EstatisticasTabelas",
                column: "TabelaJobId");

            migrationBuilder.CreateIndex(
                name: "IX_Execucoes_IniciadoEm",
                table: "Execucoes",
                column: "IniciadoEm");

            migrationBuilder.CreateIndex(
                name: "IX_Execucoes_JobId",
                table: "Execucoes",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_Execucoes_JobId_IniciadoEm",
                table: "Execucoes",
                columns: new[] { "JobId", "IniciadoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_Execucoes_Status",
                table: "Execucoes",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_ConexaoDestinoId",
                table: "Jobs",
                column: "ConexaoDestinoId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_ConexaoOrigemId",
                table: "Jobs",
                column: "ConexaoOrigemId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_Nome",
                table: "Jobs",
                column: "Nome");

            migrationBuilder.CreateIndex(
                name: "IX_Mapeamentos_TabelaJobId",
                table: "Mapeamentos",
                column: "TabelaJobId");

            migrationBuilder.CreateIndex(
                name: "IX_Regras_MapeamentoId",
                table: "Regras",
                column: "MapeamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Regras_MapeamentoId_Ordem",
                table: "Regras",
                columns: new[] { "MapeamentoId", "Ordem" });

            migrationBuilder.CreateIndex(
                name: "IX_TabelasJobs_JobId",
                table: "TabelasJobs",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_TabelasJobs_JobId_OrdemExecucao",
                table: "TabelasJobs",
                columns: new[] { "JobId", "OrdemExecucao" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Conexoes");

            migrationBuilder.DropTable(
                name: "ErrosExecucao");

            migrationBuilder.DropTable(
                name: "EstatisticasTabelas");

            migrationBuilder.DropTable(
                name: "Regras");

            migrationBuilder.DropTable(
                name: "Execucoes");

            migrationBuilder.DropTable(
                name: "Mapeamentos");

            migrationBuilder.DropTable(
                name: "TabelasJobs");

            migrationBuilder.DropTable(
                name: "Jobs");
        }
    }
}
