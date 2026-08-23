USE [SNP_PlanificacionInstitucional];
GO
SET NOCOUNT ON;

PRINT 'Iniciando migración de referencia lógica PEI -> Plan Nacional...';
GO

IF OBJECT_ID(N'dbo.PlanesEstrategicos', N'U') IS NULL
BEGIN
    PRINT 'La tabla dbo.PlanesEstrategicos no existe. La migración no se ejecuta.';
    RETURN;
END;
GO

-- 1) Agregar la columna si no existe
IF COL_LENGTH(N'dbo.PlanesEstrategicos', N'PlanNacionalId') IS NULL
BEGIN
    ALTER TABLE dbo.PlanesEstrategicos
        ADD PlanNacionalId INT NULL;
END;
GO

-- 2) Crear índice para consultas por plan nacional.
-- La existencia y el período del PND se validan en la API de Planificación Institucional
-- contra SNP_MacroPlanificacion; no se usa una FK entre bases de datos.
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_PlanesEstrategicos_PlanNacionalId'
      AND object_id = OBJECT_ID(N'dbo.PlanesEstrategicos')
)
BEGIN
    CREATE INDEX IX_PlanesEstrategicos_PlanNacionalId
        ON dbo.PlanesEstrategicos (PlanNacionalId);
END;
GO

PRINT 'Migración completada: dbo.PlanesEstrategicos ahora incluye PlanNacionalId como referencia lógica al catálogo de Macro Planificación.';
GO
