DECLARE @Cutoff DATETIME2(0) = DATEADD(DAY, -3, SYSUTCDATETIME()); -- pl. 3 napnál régebbiek

UPDATE app.TodoItem
SET Priority = 3
WHERE IsDone = 0
  AND CreatedAt < @Cutoff
  AND Priority <> 3;
