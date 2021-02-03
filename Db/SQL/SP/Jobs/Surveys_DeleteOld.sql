SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		Luigi Menghini
-- Create date: 30 Mar 2020
-- Description:	Deletes old surveys and questions
-- =============================================
CREATE PROCEDURE Surveys_DeleteOld  
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	declare @delbefore datetime2  = dateadd(day,-7, getutcdate() ) 
    delete questions where CreatedUtc < @delbefore;
    delete surveys where CreatedUtc < @delbefore;
END
GO
