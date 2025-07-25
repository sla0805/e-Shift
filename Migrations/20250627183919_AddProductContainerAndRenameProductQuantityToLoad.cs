using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eShift.Migrations
{
    /// <inheritdoc />
    public partial class AddProductContainerAndRenameProductQuantityToLoad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "Loads",
                newName: "ProductQuantity");

            migrationBuilder.AddColumn<int>(
                name: "ProductContainer",
                table: "Loads",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductContainer",
                table: "Loads");

            migrationBuilder.RenameColumn(
                name: "ProductQuantity",
                table: "Loads",
                newName: "Quantity");
        }
    }
}
