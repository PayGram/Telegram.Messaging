-- 1) Su Questions: SurveyId + Id DESC
IF NOT EXISTS (SELECT 1 FROM sys.indexes 
               WHERE name = 'IX_Questions_SurveyId_Id'
                 AND object_id = OBJECT_ID('dbo.Questions'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Questions_SurveyId_Id
    ON dbo.Questions (SurveyId, Id DESC);
END
GO

-- 2) Su Surveys: TelegramUserId + Id
IF NOT EXISTS (SELECT 1 FROM sys.indexes 
               WHERE name = 'IX_Surveys_TelegramUserId_Id'
                 AND object_id = OBJECT_ID('dbo.Surveys'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Surveys_TelegramUserId_Id
    ON dbo.Surveys (TelegramUserId, Id);
END
GO
