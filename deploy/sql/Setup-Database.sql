-- One-time setup: run against the SQL Server instance as a sysadmin (SSMS, or
-- `sqlcmd -S . -E -i deploy\sql\Setup-Database.sql`) BEFORE the first deploy.
--
-- Creates the HAMS database and grants the IIS application pool's Windows identity permission to
-- both apply EF Core migrations (DDL - every module creates its own schema/tables on first startup,
-- see HAMS.WebHost/Program.cs) and read/write data at runtime.
--
-- Assumes SQL Server and IIS are on the SAME machine - 'IIS AppPool\<name>' virtual accounts are
-- only resolvable locally, which is exactly this deployment's topology (Server=. / local default
-- instance, no TCP/IP needed, Shared Memory is enough). If the app pool is named something other
-- than "HAMS", replace every 'IIS AppPool\HAMS' below to match.

IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = N'HAMS')
BEGIN
    CREATE DATABASE [HAMS];
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'IIS AppPool\HAMS')
BEGIN
    CREATE LOGIN [IIS AppPool\HAMS] FROM WINDOWS;
END
GO

USE [HAMS];
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'IIS AppPool\HAMS')
BEGIN
    CREATE USER [IIS AppPool\HAMS] FOR LOGIN [IIS AppPool\HAMS];
END
GO

-- db_owner is the simplest sufficient role for a solo-admin, no-dedicated-DBA deployment (the
-- build plan's own operational philosophy for this system) - the app needs to both apply EF Core
-- migrations (CREATE SCHEMA/TABLE) on startup and read/write every module's own schema at runtime.
-- A stricter split (a migration-only login vs. a runtime-only db_datareader/db_datawriter login)
-- is possible later if a dedicated DBA role ever exists, but isn't required for this deployment.
ALTER ROLE db_owner ADD MEMBER [IIS AppPool\HAMS];
GO

PRINT 'HAMS database ready; IIS AppPool\HAMS granted db_owner.';
