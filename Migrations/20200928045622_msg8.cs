using Microsoft.EntityFrameworkCore.Migrations;

namespace Telegram.Messaging.Migrations
{
	public partial class msg8 : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.CreateIndex(
				name: "IX_Questions_FieldTypeId",
				table: "Questions",
				column: "FieldTypeId");

			migrationBuilder.AddForeignKey(
				name: "FK_Questions_FieldTypes_FieldTypeId",
				table: "Questions",
				column: "FieldTypeId",
				principalTable: "FieldTypes",
				principalColumn: "Id",
				onDelete: ReferentialAction.Cascade);
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropForeignKey(
				name: "FK_Questions_FieldTypes_FieldTypeId",
				table: "Questions");

			migrationBuilder.DropIndex(
				name: "IX_Questions_FieldTypeId",
				table: "Questions");
		}
	}
}
