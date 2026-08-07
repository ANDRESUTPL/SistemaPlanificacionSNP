IF DB_ID(N'SNP_MacroPlanificacion') IS NULL
BEGIN
    CREATE DATABASE [SNP_MacroPlanificacion];
END;
GO

USE [SNP_MacroPlanificacion];
GO

IF OBJECT_ID(N'dbo.PlanesNacionalesDesarrollo', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PlanesNacionalesDesarrollo (
        PlanNacionalId INT IDENTITY(1,1) PRIMARY KEY,
        Nombre NVARCHAR(200) NOT NULL,
        PeriodoPlanificacionId INT NULL,
        PeriodoInicio INT NOT NULL,
        PeriodoFin INT NOT NULL,
        Estado NVARCHAR(30) NOT NULL,
        FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_Macro_PlanNacional_FechaCreacion DEFAULT (SYSUTCDATETIME())
    );
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_PlanesNacionales_PeriodoPlanificacionId'
      AND object_id = OBJECT_ID(N'dbo.PlanesNacionalesDesarrollo')
)
BEGIN
    CREATE INDEX IX_PlanesNacionales_PeriodoPlanificacionId
        ON dbo.PlanesNacionalesDesarrollo(PeriodoPlanificacionId);
END;
GO

IF OBJECT_ID(N'dbo.ObjetivosEstrategicos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ObjetivosEstrategicos (
        ObjetivoEstrategicoId INT IDENTITY(1,1) PRIMARY KEY,
        PlanNacionalId INT NOT NULL,
        Codigo NVARCHAR(30) NOT NULL,
        Nombre NVARCHAR(300) NOT NULL,
        Descripcion NVARCHAR(600) NULL,
        CONSTRAINT FK_Macro_Objetivos_PlanNacional FOREIGN KEY (PlanNacionalId) REFERENCES dbo.PlanesNacionalesDesarrollo(PlanNacionalId),
        CONSTRAINT UQ_Macro_Objetivos_Codigo UNIQUE (PlanNacionalId, Codigo)
    );
END;
GO