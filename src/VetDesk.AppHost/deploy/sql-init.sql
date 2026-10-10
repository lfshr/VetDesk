-- Creates the VetDesk database and the vetdesk_app login VetDesk.Web connects with, so the app never uses sa.
-- Safe to run again: it only creates what is missing and resets the login's password to APP_SQL_PASSWORD.
-- $(APP_SQL_PASSWORD) is a sqlcmd variable read from the environment. It must not contain a single quote.

IF DB_ID(N'VetDesk') IS NULL
    CREATE DATABASE [VetDesk];
GO

IF SUSER_ID(N'vetdesk_app') IS NULL
    CREATE LOGIN [vetdesk_app] WITH PASSWORD = N'$(APP_SQL_PASSWORD)', DEFAULT_DATABASE = [VetDesk];
ELSE
    ALTER LOGIN [vetdesk_app] WITH PASSWORD = N'$(APP_SQL_PASSWORD)';
GO

USE [VetDesk];
GO

IF USER_ID(N'vetdesk_app') IS NULL
    CREATE USER [vetdesk_app] FOR LOGIN [vetdesk_app];

-- Read and write data, apply EF Core migrations at start-up (ddladmin) and take the nightly backup.
-- No rights outside this database.
ALTER ROLE [db_datareader] ADD MEMBER [vetdesk_app];
ALTER ROLE [db_datawriter] ADD MEMBER [vetdesk_app];
ALTER ROLE [db_ddladmin] ADD MEMBER [vetdesk_app];
ALTER ROLE [db_backupoperator] ADD MEMBER [vetdesk_app];
GO
