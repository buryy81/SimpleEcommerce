using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimpleEcommerce.Migrations
{
    /// <inheritdoc />
    public partial class AddWithdrawalEnabled : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "WithdrawalEnabled",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WithdrawalEnabled",
                table: "Users");
        }
    }
}
