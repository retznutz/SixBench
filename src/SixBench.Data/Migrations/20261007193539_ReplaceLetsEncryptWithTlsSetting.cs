using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SixBench.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceLetsEncryptWithTlsSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LetsEncryptSetting");

            migrationBuilder.CreateTable(
                name: "TlsSetting",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Domain = table.Column<string>(type: "TEXT", maxLength: 253, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 254, nullable: true),
                    DnsProvider = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    EncryptedDnsCredentials = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TlsSetting", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TlsSetting");

            migrationBuilder.CreateTable(
                name: "LetsEncryptSetting",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    CertificateDomain = table.Column<string>(type: "TEXT", maxLength: 253, nullable: true),
                    CertificateIsStaging = table.Column<bool>(type: "INTEGER", nullable: false),
                    CertificatePath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    DnsRecordValue = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Domain = table.Column<string>(type: "TEXT", maxLength: 253, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 254, nullable: true),
                    ExpiresUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    HttpsPort = table.Column<int>(type: "INTEGER", nullable: false),
                    IssuedUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    KeyPath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    LastAttemptUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastError = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    PendingDomainVerified = table.Column<bool>(type: "INTEGER", nullable: false),
                    PendingExpiresUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PendingOrderUrl = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    UseStaging = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LetsEncryptSetting", x => x.Id);
                });
        }
    }
}
