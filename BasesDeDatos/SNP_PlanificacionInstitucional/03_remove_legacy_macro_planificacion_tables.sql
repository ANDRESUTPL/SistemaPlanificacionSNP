USE [SNP_PlanificacionInstitucional];
GO
SET NOCOUNT ON;

PRINT 'Eliminando tablas heredadas de Macro Planificación de la base institucional...';
GO

IF OBJECT_ID(N'dbo.FK_PlanesEstrategicos_PlanesNacionalesDesarrollo', N'F') IS NOT NULL
BEGIN
    ALTER TABLE dbo.PlanesEstrategicos
        DROP CONSTRAINT FK_PlanesEstrategicos_PlanesNacionalesDesarrollo;
END;
GO

IF OBJECT_ID(N'dbo.ObjetivosEstrategico', N'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.ObjetivosEstrategico;
END;
GO

IF OBJECT_ID(N'dbo.PlanesNacionalesDesarrollo', N'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.PlanesNacionalesDesarrollo;
END;
GO

IF OBJECT_ID(N'dbo.PlanNacionalDesarrollo', N'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.PlanNacionalDesarrollo;
END;
GO

IF OBJECT_ID(N'dbo.ObjetivoDesarrolloSostenible', N'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.ObjetivoDesarrolloSostenible;
END;
GO

PRINT 'Migración completada: SNP_PlanificacionInstitucional conserva PlanNacionalId sin FK local; SNP_MacroPlanificacion es la única fuente de PND.';
GO