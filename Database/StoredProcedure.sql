USE TodoDb;
GO

CREATE OR ALTER PROCEDURE app.sp_PromoteOldUndoneTodos
    @DaysBack INT = 7  -- alap: 1 hét
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Cutoff DATETIME2(0) = DATEADD(DAY, -@DaysBack, SYSUTCDATETIME());

    -- Csak az elvégzetlen, nem High elemeket emeljük High-ra
    UPDATE t
    SET t.Priority = 3
    FROM app.TodoItem AS t WITH (ROWLOCK)
    WHERE t.IsDone = 0
      AND t.Priority <> 3
      AND t.CreatedAt < @Cutoff;
END
GO
  

