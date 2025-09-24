using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmployeeManagementAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddJwtAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "user_account",
                columns: new[] { "id", "name", "password", "user_name" },
                values: new object[] { 1, "Administrator", "AQAAAAIAAYagAAAAELKMG82UZs23uDVWAxVrbnNNcZZu4P9sqi538pL0aDE9JPxKo41RoQosBxB/IZ+O0g==", "admin" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "user_account",
                keyColumn: "id",
                keyValue: 1);
        }
    }
}
