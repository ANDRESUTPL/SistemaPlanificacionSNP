-- 1. CREACIÓN DE LA BASE DE DATOS
IF DB_ID(N'SNP_PlanificacionInstitucional') IS NULL
BEGIN
    CREATE DATABASE [SNP_PlanificacionInstitucional];
END;
GO

USE [SNP_PlanificacionInstitucional];
GO

-- ==========================================
-- 2. TABLAS DE MACRO PLANIFICACIÓN (Nuevas)
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

-- Tabla: PlanNacionalDesarrollo (Relacionada con ODS)
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

-- Tabla: PlanesNacionalesDesarrollo
IF OBJECT_ID(N'dbo.PlanesNacionalesDesarrollo', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PlanesNacionalesDesarrollo (
        PlanNacionalId INT IDENTITY(1,1) PRIMARY KEY,
        Nombre NVARCHAR(250) NOT NULL,
        PeriodoInicio INT NOT NULL,
        PeriodoFin INT NOT NULL,
        Estado NVARCHAR(30) NOT NULL,
        FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_PlanesNac_FechaCreacion DEFAULT (SYSUTCDATETIME())
    );
END;
GO

-- Tabla: ObjetivosEstrategico (Relacionada con PlanesNacionalesDesarrollo)
IF OBJECT_ID(N'dbo.ObjetivosEstrategico', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ObjetivosEstrategico (
        ObjetivoEstrategicoId INT IDENTITY(1,1) PRIMARY KEY,
        PlanNacionalId INT NOT NULL,
        Codigo NVARCHAR(50) NOT NULL,
        Nombre NVARCHAR(250) NOT NULL,
        Descripcion NVARCHAR(MAX) NULL, -- Es nullable en C# (string?)
        CONSTRAINT FK_ObjEstrategico_PlanNacional FOREIGN KEY (PlanNacionalId) REFERENCES dbo.PlanesNacionalesDesarrollo(PlanNacionalId)
    );
END;
GO

-- ==========================================
-- 3. TABLAS DE PLANIFICACIÓN INSTITUCIONAL (Existentes)
-- ==========================================

-- Tabla: PlanesEstrategicos
IF OBJECT_ID(N'dbo.PlanesEstrategicos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PlanesEstrategicos (
        PlanEstrategicoId INT IDENTITY(1,1) PRIMARY KEY,
        Entidad NVARCHAR(200) NOT NULL,
        PeriodoInicio INT NOT NULL,
        PeriodoFin INT NOT NULL,
        Estado NVARCHAR(30) NOT NULL,
        FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_PI_PlanesEstrategicos_FechaCreacion DEFAULT (SYSUTCDATETIME())
    );
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