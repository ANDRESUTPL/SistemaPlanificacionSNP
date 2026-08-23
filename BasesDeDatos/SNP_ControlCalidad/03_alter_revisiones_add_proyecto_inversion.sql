USE [SNP_ControlCalidad];
GO
SET NOCOUNT ON;

PRINT 'Iniciando migración de Control de Calidad: ProyectoInversionId y datos de entidad...';
GO

IF OBJECT_ID(N'dbo.Revisiones', N'U') IS NULL
BEGIN
    PRINT 'La tabla dbo.Revisiones no existe. La migración no se ejecuta.';
    RETURN;
END;
GO

IF COL_LENGTH(N'dbo.Revisiones', N'ProyectoInversionId') IS NULL
BEGIN
    ALTER TABLE dbo.Revisiones
        ADD ProyectoInversionId INT NULL;
END;
GO

IF COL_LENGTH(N'dbo.Revisiones', N'EntidadPublicaId') IS NULL
BEGIN
    ALTER TABLE dbo.Revisiones
        ADD EntidadPublicaId INT NULL;
END;
GO

-- Snapshot: la integridad es lógica porque PlanesEstrategicos vive en otra base de datos.
IF COL_LENGTH(N'dbo.Revisiones', N'EntidadNombre') IS NULL
BEGIN
    ALTER TABLE dbo.Revisiones
        ADD EntidadNombre NVARCHAR(200) NULL;
END;
GO

IF COL_LENGTH(N'dbo.Revisiones', N'CodigoProyecto') IS NULL
BEGIN
    ALTER TABLE dbo.Revisiones
        ADD CodigoProyecto NVARCHAR(50) NULL;
END;
GO

UPDATE dbo.Revisiones
SET ProyectoInversionId = NULL
WHERE ProyectoInversionId IS NOT NULL
  AND ProyectoInversionId <= 0;
GO

UPDATE dbo.Revisiones
SET EntidadPublicaId = NULL
WHERE EntidadPublicaId IS NOT NULL
  AND EntidadPublicaId <= 0;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Revisiones_ProyectoInversionId'
      AND object_id = OBJECT_ID(N'dbo.Revisiones')
)
BEGIN
    CREATE INDEX IX_Revisiones_ProyectoInversionId
        ON dbo.Revisiones (ProyectoInversionId);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Revisiones_EntidadPublicaId'
      AND object_id = OBJECT_ID(N'dbo.Revisiones')
)
BEGIN
    CREATE INDEX IX_Revisiones_EntidadPublicaId
        ON dbo.Revisiones (EntidadPublicaId);
END;
GO

PRINT 'Migración completada: dbo.Revisiones ahora referencia el proyecto de inversión auditado.';
GO
