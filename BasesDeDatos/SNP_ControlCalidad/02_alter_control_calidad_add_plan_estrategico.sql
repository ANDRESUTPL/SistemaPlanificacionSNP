USE [SNP_ControlCalidad];
GO
SET NOCOUNT ON;

PRINT 'Iniciando migración de Control de Calidad: PlanEstrategicoId...';
GO

IF OBJECT_ID(N'dbo.Revisiones', N'U') IS NULL
BEGIN
    PRINT 'La tabla dbo.Revisiones no existe. La migración no se ejecuta.';
    RETURN;
END;
GO

IF COL_LENGTH(N'dbo.Revisiones', N'PlanEstrategicoId') IS NULL
BEGIN
    ALTER TABLE dbo.Revisiones
        ADD PlanEstrategicoId INT NULL;
END;
GO

UPDATE dbo.Revisiones
SET PlanEstrategicoId = NULL
WHERE PlanEstrategicoId IS NOT NULL
  AND PlanEstrategicoId <= 0;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Revisiones_PlanEstrategicoId'
      AND object_id = OBJECT_ID(N'dbo.Revisiones')
)
BEGIN
    CREATE INDEX IX_Revisiones_PlanEstrategicoId
        ON dbo.Revisiones (PlanEstrategicoId);
END;
GO

PRINT 'Migración completada: dbo.Revisiones incluye PlanEstrategicoId y su índice de consulta.';
GO
