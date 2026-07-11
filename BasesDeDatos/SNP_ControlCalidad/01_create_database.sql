IF DB_ID(N'SNP_ControlCalidad') IS NULL
BEGIN
    CREATE DATABASE [SNP_ControlCalidad];
END;
GO

USE [SNP_ControlCalidad];
GO

IF OBJECT_ID(N'dbo.Revisiones', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Revisiones (
        RevisionId INT IDENTITY(1,1) PRIMARY KEY,
        CodigoRevision NVARCHAR(40) NOT NULL UNIQUE,
        Modulo NVARCHAR(100) NOT NULL,
        Estado NVARCHAR(30) NOT NULL,
        FechaRevision DATETIME2 NOT NULL CONSTRAINT DF_CC_Revisiones_FechaRevision DEFAULT (SYSUTCDATETIME()),
        Observaciones NVARCHAR(500) NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.Auditorias', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Auditorias (
        AuditoriaId INT IDENTITY(1,1) PRIMARY KEY,
        RevisionId INT NOT NULL,
        Tipo NVARCHAR(50) NOT NULL,
        Resultado NVARCHAR(30) NOT NULL,
        Responsable NVARCHAR(120) NOT NULL,
        FechaRegistro DATETIME2 NOT NULL CONSTRAINT DF_CC_Auditorias_FechaRegistro DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_CC_Auditorias_Revisiones FOREIGN KEY (RevisionId) REFERENCES dbo.Revisiones(RevisionId)
    );
END;
GO