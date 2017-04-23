using Microsoft.EntityFrameworkCore.Migrations;

namespace PalaverCore.Data.Migrations
{
    public partial class AddUserNotifications : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "notificationenabled",
                table: "user",
                nullable: false,
                defaultValueSql: "true");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "notificationenabled",
                table: "user");
        }
    }
}
