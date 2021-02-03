using Microsoft.EntityFrameworkCore.Migrations;

namespace Telegram.Messaging.Migrations
{
	public partial class msg7 : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{


			migrationBuilder.RenameColumn(
				name: "id1",
				newName: "id",
				table: "FieldTypes");
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.RenameColumn(
				name: "id",
				newName: "id1",
				table: "FieldTypes");

		}
	}
}
