using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SixBench.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRokuDevPassword : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DevPasswordProtected",
                table: "RokuDevice",
                type: "TEXT",
                maxLength: 2048,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DevPasswordProtected",
                table: "RokuDevice");
        }
    }
}
