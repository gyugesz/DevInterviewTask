/* 0) Adatbázis létrehozása, ha még nincs */
IF DB_ID(N'TodoDb') IS NULL
BEGIN
    CREATE DATABASE TodoDb;
END
GO

USE TodoDb;
GO

/* 1) Séma létrehozása (namespacing) */
IF SCHEMA_ID(N'app') IS NULL EXEC('CREATE SCHEMA app');
GO

/* 2) Táblát dobjuk, ha már létezne (ismételt futtatáshoz kényelmes) */
IF OBJECT_ID('app.TodoItem', 'U') IS NOT NULL
    DROP TABLE app.TodoItem;
GO

/* 3) Tábla létrehozása – profi típusválasztásokkal
      (TEXT helyett NVARCHAR-t használok, datetime2, CHECK constraint) */
CREATE TABLE app.TodoItem
(
    Id            INT IDENTITY(1,1) NOT NULL
                  CONSTRAINT PK_TodoItem PRIMARY KEY CLUSTERED,
    Name          NVARCHAR(64)       NOT NULL,
    [Description] NVARCHAR(256)      NULL,
    Priority      TINYINT            NOT NULL
                  CONSTRAINT CK_TodoItem_Priority CHECK (Priority BETWEEN 1 AND 3),
    CreatedAt     DATETIME2(0)       NOT NULL
                  CONSTRAINT DF_TodoItem_CreatedAt DEFAULT (SYSUTCDATETIME()),
    IsDone        BIT                NOT NULL
                  CONSTRAINT DF_TodoItem_IsDone DEFAULT (0),
    CONSTRAINT CK_TodoItem_Name_NotEmpty CHECK (LEN(LTRIM(RTRIM(Name))) > 0)
);
GO

/* 4) Index a name oszlopon (keresés/rendezés gyorsítására) */
CREATE NONCLUSTERED INDEX IX_TodoItem_Name ON app.TodoItem(Name);
GO

/* 5) Pár minta rekord */
INSERT INTO app.TodoItem (Name, [Description], Priority)
VALUES
 (N'Venni pelenkát', N'XL, ha van akció', 2),
 (N'Jenkins pipeline refaktor', N'Deploy lépések tisztítása', 3),
 (N'Névötletek rendezése', N'Brackets app döntő', 1),
 (N'Futás 5 km', N'Laza tempó', 1);
GO

/* 6) Ellenőrzés */
SELECT TOP 50 * FROM app.TodoItem ORDER BY Id;
