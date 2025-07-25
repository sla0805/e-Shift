using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eShift.Migrations
{
    /// <inheritdoc />
    public partial class UpdateJobCustomerRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TransportAssignments_JobId",
                table: "TransportAssignments");

            migrationBuilder.CreateIndex(
                name: "IX_TransportAssignments_JobId",
                table: "TransportAssignments",
                column: "JobId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TransportAssignments_JobId",
                table: "TransportAssignments");

            migrationBuilder.CreateIndex(
                name: "IX_TransportAssignments_JobId",
                table: "TransportAssignments",
                column: "JobId",
                unique: true);
        }
    }
}
