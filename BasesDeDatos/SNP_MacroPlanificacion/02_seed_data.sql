USE [SNP_MacroPlanificacion];
GO

IF NOT EXISTS (
    SELECT 1
    FROM dbo.PlanesNacionalesDesarrollo
    WHERE Nombre = N'Plan Nacional de Desarrollo 2025-2029'
)
BEGIN
    INSERT INTO dbo.PlanesNacionalesDesarrollo (Nombre, PeriodoInicio, PeriodoFin, Estado)
    VALUES (N'Plan Nacional de Desarrollo 2025-2029', 2025, 2029, N'VIGENTE');
END;
GO

DECLARE @PlanNacionalId INT;
SELECT @PlanNacionalId = PlanNacionalId
FROM dbo.PlanesNacionalesDesarrollo
WHERE Nombre = N'Plan Nacional de Desarrollo 2025-2029';

IF @PlanNacionalId IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM dbo.ObjetivosEstrategicos
    WHERE PlanNacionalId = @PlanNacionalId
      AND Codigo = N'OE-01'
)
BEGIN
    INSERT INTO dbo.ObjetivosEstrategicos (PlanNacionalId, Codigo, Nombre, Descripcion)
    VALUES (
        @PlanNacionalId,
        N'OE-01',
        N'Reducir brechas territoriales',
        N'Orientado a mejorar la equidad y cobertura de servicios publicos.'
    );
END;
GO