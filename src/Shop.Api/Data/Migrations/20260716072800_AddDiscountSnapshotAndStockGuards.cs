using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shop.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscountSnapshotAndStockGuards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "Orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Products_StockQuantity",
                table: "Products",
                sql: "\"StockQuantity\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Coupons_Value",
                table: "Coupons",
                sql: "\"Value\" >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Products_StockQuantity",
                table: "Products");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Coupons_Value",
                table: "Coupons");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "Orders");
        }
    }
}
