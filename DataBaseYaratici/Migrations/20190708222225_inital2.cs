sing Microsoft.EntityFrameworkCore.Migrations;

namespace DataBaseYaratici.Migrations
{
    public partial class inital2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_TakipEtmeyenler",
                table: "TakipEtmeyenler");

            migrationBuilder.RenameTable(
                name: "TakipEtmeyenler",
                newName: "InstaUserShort");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InstaUserShort",
                table: "InstaUserShort",
                column: "Pk");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_InstaUserShort",
                table: "InstaUserShort");

            migrationBuilder.RenameTable(
                name: "InstaUserShort",
                newName: "TakipEtmeyenler");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TakipEtmeyenler",
                table: "TakipEtmeyenler",
                column: "Pk");
        }
    }
}
