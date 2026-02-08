using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SimpleEcommerce.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PendingRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Bank = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CardOrPhone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PendingRequests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PendingRequests_UserId",
                table: "PendingRequests",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PendingRequests");
        }
    }
}
