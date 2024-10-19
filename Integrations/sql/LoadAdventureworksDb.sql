
USE [master]
GO
-- drop DATABASE [AdventureWorks2019]
IF DB_ID('AdventureWorks2019') IS NULL
BEGIN
    CREATE DATABASE [AdventureWorks2019]

    RESTORE DATABASE [AdventureWorks2019] 
    FROM  DISK = N'/var/opt/mssql/data/AdventureWorks2019.bak' 
    WITH 
    MOVE N'AdventureWorks2019' TO N'/var/opt/mssql/data/AdventureWorks2019.mdf',  
    MOVE N'AdventureWorks2019_log' TO N'/var/opt/mssql/data/AdventureWorks2019_log.ldf',  
    NOUNLOAD,  REPLACE,  STATS = 5
END

GO
SELECT 'DONE 2';
GO