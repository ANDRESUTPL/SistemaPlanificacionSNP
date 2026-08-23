-- ==========================================
-- 1. CREACIÓN DE LA BASE DE DATOS
-- ==========================================
IF DB_ID(N'SNP_PlanificacionInstitucional') IS NULL
BEGIN
    CREATE DATABASE [SNP_PlanificacionInstitucional];
END;
GO

USE [SNP_PlanificacionInstitucional];
GO

-- ==========================================
-- 2. TABLAS DE PLANIFICACIÓN INSTITUCIONAL
-- ==========================================

-- Tabla: PlanesEstrategicos
IF OBJECT_ID(N'dbo.PlanesEstrategicos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PlanesEstrategicos (
        PlanEstrategicoId INT IDENTITY(1,1) PRIMARY KEY,
        Entidad NVARCHAR(200) NOT NULL,
        EntidadPublicaId INT NULL, -- Campo consolidado desde el ALTER TABLE
        -- Referencia lógica al PND administrado por SNP_MacroPlanificacion.
        -- No se crea una FK entre bases de datos.
        PlanNacionalId INT NULL,
        PeriodoPlanificacionId INT NULL,
        PeriodoInicio INT NOT NULL,
        PeriodoFin INT NOT NULL,
        Estado NVARCHAR(30) NOT NULL,
        FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_PI_PlanesEstrategicos_FechaCreacion DEFAULT (SYSUTCDATETIME())
    );
END;
GO

-- Índices para PlanesEstrategicos
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = N'IX_PlanesEstrategicos_PeriodoPlanificacionId' AND object_id = OBJECT_ID(N'dbo.PlanesEstrategicos')
)
BEGIN
    CREATE INDEX IX_PlanesEstrategicos_PeriodoPlanificacionId ON dbo.PlanesEstrategicos(PeriodoPlanificacionId);
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_PlanesEstrategicos_PlanNacionalId' AND object_id = OBJECT_ID(N'dbo.PlanesEstrategicos')
)
BEGIN
    CREATE INDEX IX_PlanesEstrategicos_PlanNacionalId ON dbo.PlanesEstrategicos(PlanNacionalId);
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = N'IX_PlanesEstrategicos_EntidadPublicaId' AND object_id = OBJECT_ID(N'dbo.PlanesEstrategicos')
)
BEGIN
    CREATE INDEX IX_PlanesEstrategicos_EntidadPublicaId ON dbo.PlanesEstrategicos(EntidadPublicaId);
END;
GO

-- Tabla: ProyectosInversion
IF OBJECT_ID(N'dbo.ProyectosInversion', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProyectosInversion (
        ProyectoInversionId INT IDENTITY(1,1) PRIMARY KEY,
        PlanEstrategicoId INT NOT NULL,
        CodigoProyecto NVARCHAR(50) NOT NULL,
        Nombre NVARCHAR(250) NOT NULL,
        Monto DECIMAL(18,2) NOT NULL,
        Estado NVARCHAR(30) NOT NULL,
        CONSTRAINT FK_PI_Proyectos_Planes FOREIGN KEY (PlanEstrategicoId) REFERENCES dbo.PlanesEstrategicos(PlanEstrategicoId),
        CONSTRAINT UQ_PI_Proyectos_Codigo UNIQUE (CodigoProyecto)
    );
END;
GO