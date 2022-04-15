using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Telegram.Messaging.Migrations
{
    public partial class m1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FieldTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false)
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
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TelegramMessageId = table.Column<int>(type: "int", nullable: true),
                    TelegramUserId = table.Column<long>(type: "bigint", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsCancelled = table.Column<bool>(type: "bit", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastInteractionUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Surveys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Questions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SurveyId = table.Column<int>(type: "int", nullable: false),
                    FieldTypeId = table.Column<int>(type: "int", nullable: false),
                    PickOnlyDefaultAnswers = table.Column<bool>(type: "bit", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false),
                    ExpectsCommand = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    QuestionText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InternalId = table.Column<int>(type: "int", nullable: false),
                    FollowUp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaxButtonsPerRow = table.Column<int>(type: "int", nullable: false),
                    FollowUpSeparator = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Answers = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Constraints = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultAnswers = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CallbackHandlerAssemblyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MethodNameOnEvent = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
