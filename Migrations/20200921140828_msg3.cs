using Microsoft.EntityFrameworkCore.Migrations;

namespace Telegram.Messaging.Migrations
{
	public partial class msg3 : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AddColumn<string>(
				name: "FollowUpSeparator",
				table: "Questions",
				maxLength: 10,
				nullable: true);

			migrationBuilder.AddColumn<int>(
				name: "MaxButtonsPerRow",
				table: "Questions",
				nullable: false,
				defaultValue: 0);
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				name: "FollowUpSeparator",
				table: "Questions");

			migrationBuilder.DropColumn(
				name: "MaxButtonsPerRow",
				table: "Questions");
		}
	}
}
