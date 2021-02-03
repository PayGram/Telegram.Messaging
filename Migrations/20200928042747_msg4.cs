using Microsoft.EntityFrameworkCore.Migrations;

namespace Telegram.Messaging.Migrations
{
	public partial class msg4 : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AddColumn<string>(
				name: "MethodNameOnEvent",
				table: "Questions",
				nullable: true);


		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				name: "MethodNameOnEvent",
				table: "Questions");
		}
	}
}
