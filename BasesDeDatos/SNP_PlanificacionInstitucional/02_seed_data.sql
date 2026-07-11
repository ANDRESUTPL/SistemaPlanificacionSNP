USE [SNP_PlanificacionInstitucional];
GO

IF NOT EXISTS (
    SELECT 1 FROM dbo.PlanesEstrategicos
    WHERE Entidad = N'Ministerio de Economia y Finanzas'
      AND PeriodoInicio = 2026
      AND PeriodoFin = 2029
)
BEGIN
    INSERT INTO dbo.PlanesEstrategicos (Entidad, PeriodoInicio, PeriodoFin, Estado)
    VALUES (N'Ministerio de Economia y Finanzas', 2026, 2029, N'EN_FORMULACION');
END;
GO

DECLARE @PlanEstrategicoId INT;
SELECT TOP (1) @PlanEstrategicoId = PlanEstrategicoId
FROM dbo.PlanesEstrategicos
WHERE Entidad = N'Ministerio de Economia y Finanzas'
  AND PeriodoInicio = 2026
  AND PeriodoFin = 2029
ORDER BY PlanEstrategicoId DESC;

IF @PlanEstrategicoId IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM dbo.ProyectosInversion
    WHERE CodigoProyecto = N'PI-2026-0001'
)
BEGIN
    INSERT INTO dbo.ProyectosInversion (PlanEstrategicoId, CodigoProyecto, Nombre, Monto, Estado)
    VALUES (
        @PlanEstrategicoId,
        N'PI-2026-0001',
        N'Mejoramiento de infraestructura educativa',
        2500000.00,
        N'PRIORIZADO'
    );
END;
GO