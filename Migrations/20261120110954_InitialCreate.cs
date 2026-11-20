using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeterinerKlinikSistemi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EvcilHayvanlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Tur = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Irk = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    DogumTarihi = table.Column<DateOnly>(type: "date", nullable: false),
                    Kilo = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    FotografYolu = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SahipId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvcilHayvanlar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Klinikler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Adres = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Telefon = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AcilisSaati = table.Column<TimeOnly>(type: "time", nullable: false),
                    KapanisSaati = table.Column<TimeOnly>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Klinikler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AsiKayitlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EvcilHayvanId = table.Column<int>(type: "int", nullable: false),
                    AsiAdi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    YapildigiTarih = table.Column<DateOnly>(type: "date", nullable: false),
                    SonrakiDozTarihi = table.Column<DateOnly>(type: "date", nullable: true),
                    Notlar = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AsiKayitlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AsiKayitlari_EvcilHayvanlar_EvcilHayvanId",
                        column: x => x.EvcilHayvanId,
                        principalTable: "EvcilHayvanlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Hizmetler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SureDakika = table.Column<int>(type: "int", nullable: false),
                    Ucret = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    KlinikId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hizmetler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Hizmetler_Klinikler_KlinikId",
                        column: x => x.KlinikId,
                        principalTable: "Klinikler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Veterinerler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdSoyad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UzmanlikAlanlari = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Eposta = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Telefon = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    KlinikId = table.Column<int>(type: "int", nullable: false),
                    KullaniciId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Veterinerler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Veterinerler_Klinikler_KlinikId",
                        column: x => x.KlinikId,
                        principalTable: "Klinikler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Randevular",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EvcilHayvanId = table.Column<int>(type: "int", nullable: false),
                    VeterinerId = table.Column<int>(type: "int", nullable: false),
                    HizmetId = table.Column<int>(type: "int", nullable: false),
                    BaslangicZamani = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SureDakika = table.Column<int>(type: "int", nullable: false),
                    Ucret = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Durum = table.Column<int>(type: "int", nullable: false),
                    MuayeneNotu = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OlusturulmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Randevular", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Randevular_EvcilHayvanlar_EvcilHayvanId",
                        column: x => x.EvcilHayvanId,
                        principalTable: "EvcilHayvanlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Randevular_Hizmetler_HizmetId",
                        column: x => x.HizmetId,
                        principalTable: "Hizmetler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Randevular_Veterinerler_VeterinerId",
                        column: x => x.VeterinerId,
                        principalTable: "Veterinerler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VeterinerHizmetler",
                columns: table => new
                {
                    VeterinerId = table.Column<int>(type: "int", nullable: false),
                    HizmetId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VeterinerHizmetler", x => new { x.VeterinerId, x.HizmetId });
                    table.ForeignKey(
                        name: "FK_VeterinerHizmetler_Hizmetler_HizmetId",
                        column: x => x.HizmetId,
                        principalTable: "Hizmetler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VeterinerHizmetler_Veterinerler_VeterinerId",
                        column: x => x.VeterinerId,
                        principalTable: "Veterinerler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VeterinerMusaitlikler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VeterinerId = table.Column<int>(type: "int", nullable: false),
                    Gun = table.Column<int>(type: "int", nullable: false),
                    BaslangicSaati = table.Column<TimeOnly>(type: "time", nullable: false),
                    BitisSaati = table.Column<TimeOnly>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VeterinerMusaitlikler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VeterinerMusaitlikler_Veterinerler_VeterinerId",
                        column: x => x.VeterinerId,
                        principalTable: "Veterinerler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AsiKayitlari_EvcilHayvanId",
                table: "AsiKayitlari",
                column: "EvcilHayvanId");

            migrationBuilder.CreateIndex(
                name: "IX_Hizmetler_KlinikId",
                table: "Hizmetler",
                column: "KlinikId");

            migrationBuilder.CreateIndex(
                name: "IX_Randevular_EvcilHayvanId",
                table: "Randevular",
                column: "EvcilHayvanId");

            migrationBuilder.CreateIndex(
                name: "IX_Randevular_HizmetId",
                table: "Randevular",
                column: "HizmetId");

            migrationBuilder.CreateIndex(
                name: "IX_Randevular_VeterinerId",
                table: "Randevular",
                column: "VeterinerId");

            migrationBuilder.CreateIndex(
                name: "IX_VeterinerHizmetler_HizmetId",
                table: "VeterinerHizmetler",
                column: "HizmetId");

            migrationBuilder.CreateIndex(
                name: "IX_Veterinerler_KlinikId",
                table: "Veterinerler",
                column: "KlinikId");

            migrationBuilder.CreateIndex(
                name: "IX_VeterinerMusaitlikler_VeterinerId",
                table: "VeterinerMusaitlikler",
                column: "VeterinerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AsiKayitlari");

            migrationBuilder.DropTable(
                name: "Randevular");

            migrationBuilder.DropTable(
                name: "VeterinerHizmetler");

            migrationBuilder.DropTable(
                name: "VeterinerMusaitlikler");

            migrationBuilder.DropTable(
                name: "EvcilHayvanlar");

            migrationBuilder.DropTable(
                name: "Hizmetler");

            migrationBuilder.DropTable(
                name: "Veterinerler");

            migrationBuilder.DropTable(
                name: "Klinikler");
        }
    }
}
