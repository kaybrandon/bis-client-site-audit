using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BisAudit.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIssuePurchaseNeedsAttention : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NeedsAttention",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "NeedsAttention",
                table: "Issues");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NeedsAttention",
                table: "Purchases",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NeedsAttention",
                table: "Issues",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
