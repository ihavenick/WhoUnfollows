namespace DataBaseYaratici.Migrations
{
    public partial class inital : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TakipEtmeyenler",
                columns: table => new
                {
                    Pk = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IsVerified = table.Column<bool>(nullable: false),
                    IsPrivate = table.Column<bool>(nullable: false),
                    ProfilePicture = table.Column<string>(nullable: true),
                    ProfilePicUrl = table.Column<string>(nullable: true),
                    ProfilePictureId = table.Column<string>(nullable: true),
                    UserName = table.Column<string>(nullable: true),
                    FullName = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TakipEtmeyenler", x => x.Pk);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TakipEtmeyenler");
        }
    }
}
