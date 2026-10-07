using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SixBench.Data.Migrations
{
    /// <inheritdoc />
    public partial class LetsEncryptDnsChallenge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DnsRecordValue",
                table: "LetsEncryptSetting",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PendingDomainVerified",
                table: "LetsEncryptSetting",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PendingExpiresUtc",
                table: "LetsEncryptSetting",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PendingOrderUrl",
                table: "LetsEncryptSetting",
                type: "TEXT",
                maxLength: 1024,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DnsRecordValue",
                table: "LetsEncryptSetting");

            migrationBuilder.DropColumn(
                name: "PendingDomainVerified",
                table: "LetsEncryptSetting");

            migrationBuilder.DropColumn(
                name: "PendingExpiresUtc",
                table: "LetsEncryptSetting");

            migrationBuilder.DropColumn(
                name: "PendingOrderUrl",
                table: "LetsEncryptSetting");
        }
    }
}
