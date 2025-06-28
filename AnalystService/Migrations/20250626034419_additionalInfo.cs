using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClientService.Migrations
{
    /// <inheritdoc />
    public partial class additionalInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdditionalUserInfo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    HalykToken = table.Column<string>(type: "text", nullable: false),
                    TelegramId = table.Column<string>(type: "text", nullable: false),
                    TimeFlag = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdditionalUserInfo", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdditionalUserInfo");
        }
    }
}
