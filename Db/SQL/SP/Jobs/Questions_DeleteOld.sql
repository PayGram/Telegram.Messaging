GO
/****** Object:  StoredProcedure [dbo].[Questions_DeleteOld]    Script Date: 11/20/2025 2:07:56 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER   PROCEDURE [dbo].[Questions_DeleteOld]
(
    @CutoffDate  datetime,       -- delete rows older than this
    @BatchSize   int = 1000,     -- how many rows per batch
    @MaxBatches  int = 0         -- 0 = no limit, otherwise max number of batches per call
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @RowsAffected int = 1;
    DECLARE @BatchesRun   int = 0;

    WHILE @RowsAffected > 0
    BEGIN
        -- optional limit on number of batches per execution
        IF @MaxBatches > 0 AND @BatchesRun >= @MaxBatches
            BREAK;

        ;WITH to_delete AS
        (
            SELECT TOP (@BatchSize) Id
            FROM dbo.questions WITH (ROWLOCK, READPAST)   -- hints to reduce blocking
            WHERE CreatedUtc < @CutoffDate
            ORDER BY Id                                  -- use indexed column
        )
        DELETE FROM to_delete;

        SET @RowsAffected = @@ROWCOUNT;
        SET @BatchesRun   = @BatchesRun + 1;

        -- small pause to let other transactions breathe (optional)
        IF @RowsAffected > 0
            WAITFOR DELAY '00:00:01';   -- 1 second
    END
END
