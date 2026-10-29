using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using GRUPAL.Data;

#nullable disable
namespace GRUPAL.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261028000000_FuncionesDemoAlToke")]
public class FuncionesDemoAlToke : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("ArticulosAyuda", columns: t => new
        {
            Id = t.Column<int>(nullable: false).Annotation("Sqlite:Autoincrement", true),
            Pregunta = t.Column<string>(maxLength: 160, nullable: false),
            Respuesta = t.Column<string>(maxLength: 4000, nullable: false),
            CreadoEn = t.Column<DateTime>(nullable: false)
        }, constraints: t => t.PrimaryKey("PK_ArticulosAyuda", x => x.Id));

        migrationBuilder.CreateTable("MensajesContacto", columns: t => new
        {
            Id = t.Column<int>(nullable: false).Annotation("Sqlite:Autoincrement", true),
            Nombre = t.Column<string>(maxLength: 100, nullable: false),
            Correo = t.Column<string>(maxLength: 150, nullable: false),
            Asunto = t.Column<string>(maxLength: 140, nullable: false),
            Mensaje = t.Column<string>(maxLength: 3000, nullable: false),
            CreadoEn = t.Column<DateTime>(nullable: false),
            Atendido = t.Column<bool>(nullable: false),
            Sentimiento = t.Column<string>(maxLength: 20, nullable: false)
        }, constraints: t => t.PrimaryKey("PK_MensajesContacto", x => x.Id));

        migrationBuilder.CreateTable("Favoritos", columns: t => new
        {
            Id = t.Column<int>(nullable: false).Annotation("Sqlite:Autoincrement", true),
            UsuarioId = t.Column<int>(nullable: false),
            ObjetoPerdidoId = t.Column<int>(nullable: false),
            CreadoEn = t.Column<DateTime>(nullable: false)
        }, constraints: t =>
        {
            t.PrimaryKey("PK_Favoritos", x => x.Id);
            t.ForeignKey("FK_Favoritos_Usuarios_UsuarioId", x => x.UsuarioId, "Usuarios", "Id", onDelete: ReferentialAction.Cascade);
            t.ForeignKey("FK_Favoritos_ObjetosPerdidos_ObjetoPerdidoId", x => x.ObjetoPerdidoId, "ObjetosPerdidos", "Id", onDelete: ReferentialAction.Cascade);
        });

        migrationBuilder.CreateTable("SolicitudesRecuperacion", columns: t => new
        {
            Id = t.Column<int>(nullable: false).Annotation("Sqlite:Autoincrement", true),
            ObjetoPerdidoId = t.Column<int>(nullable: false),
            SolicitanteId = t.Column<int>(nullable: false),
            Mensaje = t.Column<string>(maxLength: 1000, nullable: false),
            Respuesta = t.Column<string>(maxLength: 1000, nullable: true),
            Estado = t.Column<int>(nullable: false),
            CreadoEn = t.Column<DateTime>(nullable: false)
        }, constraints: t =>
        {
            t.PrimaryKey("PK_SolicitudesRecuperacion", x => x.Id);
            t.ForeignKey("FK_SolicitudesRecuperacion_Usuarios_SolicitanteId", x => x.SolicitanteId, "Usuarios", "Id", onDelete: ReferentialAction.Restrict);
            t.ForeignKey("FK_SolicitudesRecuperacion_ObjetosPerdidos_ObjetoPerdidoId", x => x.ObjetoPerdidoId, "ObjetosPerdidos", "Id", onDelete: ReferentialAction.Cascade);
        });
        migrationBuilder.CreateIndex("IX_Favoritos_UsuarioId_ObjetoPerdidoId", "Favoritos", new[] { "UsuarioId", "ObjetoPerdidoId" }, unique: true);
        migrationBuilder.CreateIndex("IX_Favoritos_ObjetoPerdidoId", "Favoritos", "ObjetoPerdidoId");
        migrationBuilder.CreateIndex("IX_SolicitudesRecuperacion_SolicitanteId_ObjetoPerdidoId", "SolicitudesRecuperacion", new[] { "SolicitanteId", "ObjetoPerdidoId" }, unique: true);
        migrationBuilder.CreateIndex("IX_SolicitudesRecuperacion_ObjetoPerdidoId", "SolicitudesRecuperacion", "ObjetoPerdidoId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("ArticulosAyuda");
        migrationBuilder.DropTable("MensajesContacto");
        migrationBuilder.DropTable("Favoritos");
        migrationBuilder.DropTable("SolicitudesRecuperacion");
    }
}
