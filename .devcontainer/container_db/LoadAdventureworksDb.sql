
USE [master]
GO
-- drop DATABASE [AdventureWorks2019]
-- IF DB_ID('AdventureWorks2019') IS NULL
-- BEGIN
--     CREATE DATABASE [AdventureWorks2019]

--     RESTORE DATABASE [AdventureWorks2019] 
--     FROM  DISK = N'/var/opt/mssql/data/AdventureWorks2019.bak' 
--     WITH 
--     MOVE N'AdventureWorks2019' TO N'/var/opt/mssql/data/AdventureWorks2019.mdf',  
--     MOVE N'AdventureWorks2019_log' TO N'/var/opt/mssql/data/AdventureWorks2019_log.ldf',  
--     NOUNLOAD,  REPLACE,  STATS = 5
-- END

GO

-- Create table SourceDb if it does not exist
USE [master]
IF DB_ID('SourceDb') IS NULL
BEGIN
    CREATE DATABASE [SourceDb]
    ALTER DATABASE [SourceDb] SET RECOVERY SIMPLE
END
GO

Use SourceDb
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SourceTable')
BEGIN
    CREATE TABLE [SourceDb].[dbo].[SourceTable] (Id INT IDENTITY(1,1), Name NVARCHAR(100), Age int NULL)
    INSERT INTO [SourceDb].[dbo].[SourceTable] VALUES ('Name1-renamed', 150)
    INSERT INTO [SourceDb].[dbo].[SourceTable] VALUES ('Name2', 200)
    INSERT INTO [SourceDb].[dbo].[SourceTable] VALUES ('Name3', 300)
    
END

-- Create TargetTable if it does not exist
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TargetTable')
BEGIN
    CREATE TABLE [SourceDb].[dbo].[TargetTable] (
        Id INT UNIQUE,
        Name NVARCHAR(100),
        Age INT NULL
    );
    INSERT INTO [SourceDb].[dbo].[TargetTable] VALUES (1, 'Name1', 100)
END

GO

SELECT 'DONE 2';
GO