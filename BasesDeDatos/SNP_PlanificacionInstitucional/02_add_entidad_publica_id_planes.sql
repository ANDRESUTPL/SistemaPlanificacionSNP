USE [SNP_PlanificacionInstitucional];
GO

IF COL_LENGTH('dbo.PlanesEstrategicos', 'EntidadPublicaId') IS NULL
BEGIN
    ALTER TABLE dbo.PlanesEstrategicos
    ADD EntidadPublicaId INT NULL;
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_PlanesEstrategicos_EntidadPublicaId'
      AND object_id = OBJECT_ID(N'dbo.PlanesEstrategicos')
)
BEGIN
    CREATE INDEX IX_PlanesEstrategicos_EntidadPublicaId
        ON dbo.PlanesEstrategicos(EntidadPublicaId);
END;
GO

-- Backfill opcional para registros existentes.
-- Ejecuta los UPDATE necesarios para asignar EntidadPublicaId segun tu catalogo de Entidades Publicas.
-- Ejemplo:
-- UPDATE p
-- SET p.EntidadPublicaId = e.EntidadPublicaId
-- FROM dbo.PlanesEstrategicos p
-- INNER JOIN [SNP_Parametrizacion].dbo.EntidadesPublicas e
--     ON LTRIM(RTRIM(p.Entidad)) = LTRIM(RTRIM(e.Nombre));
