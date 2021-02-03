using Microsoft.EntityFrameworkCore.Migrations;

namespace Telegram.Messaging.Migrations
{
	public partial class msg6 : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropForeignKey(
				name: "FK_Questions_FieldTypes_FieldTypeId",
				table: "Questions");

			migrationBuilder.DropIndex(
				name: "IX_Questions_FieldTypeId",
				table: "Questions");

			migrationBuilder.DropPrimaryKey(
				name: "PK_FieldTypes",
				table: "FieldTypes");

			migrationBuilder.DropColumn(
				name: "Id",
				table: "FieldTypes");

			//migrationBuilder.AddColumn<int>(
			//    name: "Id1",
			//    table: "FieldTypes",
			//    nullable: false,
			//    defaultValue: 0);

			migrationBuilder.AddPrimaryKey(
				name: "PK_FieldTypes",
				table: "FieldTypes",
				column: "Id1");
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropPrimaryKey(
				name: "PK_FieldTypes",
				table: "FieldTypes");

			//migrationBuilder.DropColumn(
			//    name: "Id1",
			//    table: "FieldTypes");

			migrationBuilder.AddColumn<int>(
				name: "Id",
				table: "FieldTypes",
				type: "int",
				nullable: false,
				defaultValue: 0);

			migrationBuilder.AddPrimaryKey(
				name: "PK_FieldTypes",
				table: "FieldTypes",
				column: "Id");

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
	}
}
