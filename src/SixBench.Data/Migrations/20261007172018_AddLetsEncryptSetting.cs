using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SixBench.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLetsEncryptSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LetsEncryptSetting",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Domain = table.Column<string>(type: "TEXT", maxLength: 253, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 254, nullable: true),
                    HttpsPort = table.Column<int>(type: "INTEGER", nullable: false),
                    UseStaging = table.Column<bool>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    LastError = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    LastAttemptUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CertificateDomain = table.Column<string>(type: "TEXT", maxLength: 253, nullable: true),
                    CertificateIsStaging = table.Column<bool>(type: "INTEGER", nullable: false),
                    CertificatePath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    KeyPath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    IssuedUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ExpiresUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LetsEncryptSetting", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LetsEncryptSetting");
        }
    }
}
