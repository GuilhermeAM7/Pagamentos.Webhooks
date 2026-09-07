using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "eventos_webhook",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    id_transacao = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    id_contrato = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    data_pagamento = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    data_recebido = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    data_processado = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status_origem = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status_pagamento = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    tentativas = table.Column<int>(type: "integer", nullable: false),
                    ultimo_erro = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_eventos_webhook", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "status_contrato",
                columns: table => new
                {
                    id_contrato = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status_pagamento = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ultimo_valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ultima_data_pagamento = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ultima_recepcao_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ultimo_evento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                    //xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                    // xmin é coluna de sistema do PostgreSQL e já existe em toda tabela.
                    // O gerador do Npgsql emite DDL para ela indevidamente (efcore.pg#3854); removido manualmente.
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_status_contrato", x => x.id_contrato);
                    table.ForeignKey(
                        name: "fk_status_contrato_eventos_webhook_ultimo_evento_id",
                        column: x => x.ultimo_evento_id,
                        principalTable: "eventos_webhook",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_eventos_webhook_id_contrato",
                table: "eventos_webhook",
                column: "id_contrato");

            migrationBuilder.CreateIndex(
                name: "ix_eventos_webhook_status_data_recebido",
                table: "eventos_webhook",
                columns: new[] { "status", "data_recebido" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ux_eventos_webhook_id_transacao",
                table: "eventos_webhook",
                column: "id_transacao",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_status_contrato_ultimo_evento_id",
                table: "status_contrato",
                column: "ultimo_evento_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "status_contrato");

            migrationBuilder.DropTable(
                name: "eventos_webhook");
        }
    }
}
