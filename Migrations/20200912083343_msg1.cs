using Microsoft.EntityFrameworkCore.Migrations;

namespace Telegram.Messaging.Migrations
{
	public partial class msg1 : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.CreateTable(
				name: "FieldTypes",
				columns: table => new
				{
					Id = table.Column<int>(nullable: false)
						.Annotation("SqlServer:Identity", "1, 1"),
					Name = table.Column<string>(nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_FieldTypes", x => x.Id);
					table.UniqueConstraint("AK_FieldTypes_Name", x => x.Name);
				});

			migrationBuilder.CreateTable(
				name: "Surveys",
				columns: table => new
				{
					Id = table.Column<int>(nullable: false)
						.Annotation("SqlServer:Identity", "1, 1"),
					TelegramMessageId = table.Column<long>(nullable: true),
					TelegramUserId = table.Column<long>(nullable: false),
					IsActive = table.Column<bool>(nullable: false),
					IsCancelled = table.Column<bool>(nullable: false),
					IsCompleted = table.Column<bool>(nullable: false),
					CreatedUtc = table.Column<DateTime>(nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Surveys", x => x.Id);
				});

			migrationBuilder.CreateTable(
				name: "Questions",
				columns: table => new
				{
					Id = table.Column<int>(nullable: false)
						.Annotation("SqlServer:Identity", "1, 1"),
					SurveyId = table.Column<int>(nullable: false),
					FieldTypeId = table.Column<int>(nullable: false),
					PickOnlyDefaultAnswers = table.Column<bool>(nullable: false),
					IsCompleted = table.Column<bool>(nullable: false),
					IsMandatory = table.Column<bool>(nullable: false),
					ExpectsCommand = table.Column<bool>(nullable: false),
					CreatedUtc = table.Column<DateTime>(nullable: false),
					QuestionText = table.Column<string>(nullable: true),
					InternalId = table.Column<int>(nullable: false),
					FollowUp = table.Column<string>(nullable: true),
					Answers = table.Column<string>(nullable: true),
					Constraints = table.Column<string>(nullable: true),
					DefaultAnswers = table.Column<string>(nullable: true),
					CallbackHandlerAssemblyName = table.Column<string>(nullable: true)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Questions", x => x.Id);
					table.ForeignKey(
						name: "FK_Questions_FieldTypes_FieldTypeId",
						column: x => x.FieldTypeId,
						principalTable: "FieldTypes",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
					table.ForeignKey(
						name: "FK_Questions_Surveys_SurveyId",
						column: x => x.SurveyId,
						principalTable: "Surveys",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateIndex(
				name: "IX_Questions_FieldTypeId",
				table: "Questions",
				column: "FieldTypeId");

			migrationBuilder.CreateIndex(
				name: "IX_Questions_SurveyId",
				table: "Questions",
				column: "SurveyId");
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(
				name: "Questions");

			migrationBuilder.DropTable(
				name: "FieldTypes");

			migrationBuilder.DropTable(
				name: "Surveys");
		}
	}
}
