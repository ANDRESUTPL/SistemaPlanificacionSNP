-- ==========================================
-- 1. CREACIÓN DE LA BASE DE DATOS
-- ==========================================
IF DB_ID(N'SNP_MacroPlanificacion') IS NULL
BEGIN
    CREATE DATABASE [SNP_MacroPlanificacion];
END;
GO

USE [SNP_MacroPlanificacion];
GO

-- ==========================================
-- 2. TABLAS FALTANTES SEGÚN ENTIDADES C#
-- ==========================================

-- Tabla: ObjetivoDesarrolloSostenible
IF OBJECT_ID(N'dbo.ObjetivoDesarrolloSostenible', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ObjetivoDesarrolloSostenible (
        OdsId INT IDENTITY(1,1) PRIMARY KEY,
        Codigo NVARCHAR(50) NOT NULL,
        Nombre NVARCHAR(250) NOT NULL,
        Descripcion NVARCHAR(MAX) NOT NULL,
        Activo BIT NOT NULL CONSTRAINT DF_ODS_Activo DEFAULT 1,
        FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_ODS_FechaCreacion DEFAULT (SYSUTCDATETIME())
    );
END;
GO

-- Tabla: PlanNacionalDesarrollo
IF OBJECT_ID(N'dbo.PlanNacionalDesarrollo', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PlanNacionalDesarrollo (
        PndId INT IDENTITY(1,1) PRIMARY KEY,
        Codigo NVARCHAR(50) NOT NULL,
        Nombre NVARCHAR(250) NOT NULL,
        Descripcion NVARCHAR(MAX) NOT NULL,
        OdsId INT NOT NULL,
        Activo BIT NOT NULL CONSTRAINT DF_PND_Activo DEFAULT 1,
        FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_PND_FechaCreacion DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_PND_Ods FOREIGN KEY (OdsId) REFERENCES dbo.ObjetivoDesarrolloSostenible(OdsId)
    );
END;
GO

-- ==========================================
-- 3. TABLAS ORIGINALES (Consolidadas)
-- ==========================================

-- Tabla: PlanesNacionalesDesarrollo
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

-- Tabla: ObjetivosEstrategicos
IF OBJECT_ID(N'dbo.ObjetivosEstrategicos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ObjetivosEstrategicos (
        ObjetivoEstrategicoId INT IDENTITY(1,1) PRIMARY KEY,
        PlanNacionalId INT NOT NULL,
        Codigo NVARCHAR(30) NOT NULL,
        Nombre NVARCHAR(300) NOT NULL,
        Descripcion NVARCHAR(600) NULL,
        
        -- Campos de auditoría consolidados desde el archivo ALTER
        IsDeleted BIT NOT NULL CONSTRAINT DF_ObjetivosEstrategicos_IsDeleted DEFAULT (0),
        DeletedAtUtc DATETIME2(7) NULL,
        DeletedBy NVARCHAR(100) NULL,

        CONSTRAINT FK_Macro_Objetivos_PlanNacional FOREIGN KEY (PlanNacionalId) REFERENCES dbo.PlanesNacionalesDesarrollo(PlanNacionalId),
        CONSTRAINT UQ_Macro_Objetivos_Codigo UNIQUE (PlanNacionalId, Codigo)
    );
END;
GO

-- Índice consolidado para registros activos
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_ObjetivosEstrategicos_Activos_Plan_Codigo'
            AND object_id = OBJECT_ID('dbo.ObjetivosEstrategicos')
)
BEGIN
    CREATE INDEX IX_ObjetivosEstrategicos_Activos_Plan_Codigo
                ON dbo.ObjetivosEstrategicos (PlanNacionalId, Codigo)
        WHERE IsDeleted = 0;
END
GO