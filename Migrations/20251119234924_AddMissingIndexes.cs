using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Telegram.Messaging.Migrations
{
    public partial class AddMissingIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
			migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_Questions_SurveyId_Id'
      AND object_id = OBJECT_ID('dbo.Questions')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Questions_SurveyId_Id
    ON dbo.Questions (SurveyId, Id DESC);
END
");

			migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_Surveys_TelegramUserId_Id'
      AND object_id = OBJECT_ID('dbo.Surveys')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Surveys_TelegramUserId_Id
    ON dbo.Surveys (TelegramUserId, Id);
END
");
		}

        protected override void Down(MigrationBuilder migrationBuilder)
        {
			migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_Questions_SurveyId_Id'
      AND object_id = OBJECT_ID('dbo.Questions')
)
BEGIN
    DROP INDEX IX_Questions_SurveyId_Id ON dbo.Questions;
END
");

			migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_Surveys_TelegramUserId_Id'
      AND object_id = OBJECT_ID('dbo.Surveys')
)
BEGIN
    DROP INDEX IX_Surveys_TelegramUserId_Id ON dbo.Surveys;
END
");
		}
    }
}
