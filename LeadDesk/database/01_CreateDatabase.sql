IF DB_ID(N'LeadDeskDb') IS NULL
BEGIN
    CREATE DATABASE LeadDeskDb;
END;
GO
USE LeadDeskDb;
GO

IF OBJECT_ID(N'dbo.Leads', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Leads
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Leads PRIMARY KEY,
        Name NVARCHAR(150) NOT NULL,
        MobileNumber NVARCHAR(25) NOT NULL,
        Email NVARCHAR(254) NULL,
        CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_Leads_CreatedAtUtc DEFAULT SYSUTCDATETIME()
    );
END;
GO

-- A lead is considered a duplicate when its mobile number or non-empty email already exists.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Leads_MobileNumber' AND object_id = OBJECT_ID(N'dbo.Leads'))
    CREATE UNIQUE INDEX UX_Leads_MobileNumber ON dbo.Leads(MobileNumber);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Leads_Email' AND object_id = OBJECT_ID(N'dbo.Leads'))
    CREATE UNIQUE INDEX UX_Leads_Email ON dbo.Leads(Email) WHERE Email IS NOT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Leads_CreatedAtUtc' AND object_id = OBJECT_ID(N'dbo.Leads'))
    CREATE INDEX IX_Leads_CreatedAtUtc ON dbo.Leads(CreatedAtUtc DESC, Id DESC);
GO

IF OBJECT_ID(N'dbo.ImportSummary', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ImportSummary
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ImportSummary PRIMARY KEY,
        ImportedAtUtc DATETIME2(0) NOT NULL,
        TotalRows INT NOT NULL,
        ImportedCount INT NOT NULL,
        ExistingCount INT NOT NULL,
        InvalidCount INT NOT NULL,
        CONSTRAINT CK_ImportSummary_NonNegativeCounts CHECK
            (TotalRows >= 0 AND ImportedCount >= 0 AND ExistingCount >= 0 AND InvalidCount >= 0)
    );
END;
GO

-- Optional stored procedure for database-side paginated search.
CREATE OR ALTER PROCEDURE dbo.usp_Leads_Search
    @Search NVARCHAR(200) = NULL,
    @PageNumber INT = 1,
    @PageSize INT = 10
AS
BEGIN
    SET NOCOUNT ON;
    IF @PageNumber < 1 OR @PageSize < 1 OR @PageSize > 100
        THROW 50001, 'PageNumber must be >= 1 and PageSize must be between 1 and 100.', 1;

    DECLARE @Pattern NVARCHAR(202) = CASE WHEN NULLIF(LTRIM(RTRIM(@Search)), N'') IS NULL
        THEN NULL ELSE N'%' + LTRIM(RTRIM(@Search)) + N'%' END;

    SELECT COUNT_BIG(1) AS TotalCount
    FROM dbo.Leads
    WHERE @Pattern IS NULL OR Name LIKE @Pattern OR MobileNumber LIKE @Pattern OR Email LIKE @Pattern;

    SELECT Id, Name, MobileNumber, Email, CreatedAtUtc
    FROM dbo.Leads
    WHERE @Pattern IS NULL OR Name LIKE @Pattern OR MobileNumber LIKE @Pattern OR Email LIKE @Pattern
    ORDER BY CreatedAtUtc DESC, Id DESC
    OFFSET CAST(@PageNumber - 1 AS BIGINT) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO
