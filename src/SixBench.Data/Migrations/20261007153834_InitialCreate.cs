using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SixBench.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RokuDevice",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SerialNumber = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    FriendlyName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Model = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Port = table.Column<int>(type: "INTEGER", nullable: false),
                    IsManual = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastSeenUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RokuDevice", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EncoderLink",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CaptureDeviceStableId = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    VideoInput = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    AudioInput = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    RokuDeviceId = table.Column<int>(type: "INTEGER", nullable: true),
                    AllowDeviceAudio = table.Column<bool>(type: "INTEGER", nullable: false),
                    FrameRate = table.Column<double>(type: "REAL", nullable: true),
                    VideoSize = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    PixelFormat = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EncoderLink", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EncoderLink_RokuDevice_RokuDeviceId",
                        column: x => x.RokuDeviceId,
                        principalTable: "RokuDevice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EncoderLink_CaptureDeviceStableId",
                table: "EncoderLink",
                column: "CaptureDeviceStableId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EncoderLink_RokuDeviceId",
                table: "EncoderLink",
                column: "RokuDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_RokuDevice_SerialNumber",
                table: "RokuDevice",
                column: "SerialNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EncoderLink");

            migrationBuilder.DropTable(
                name: "RokuDevice");
        }
    }
}
