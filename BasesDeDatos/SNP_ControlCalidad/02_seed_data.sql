USE [SNP_ControlCalidad];
GO

IF NOT EXISTS (
    SELECT 1 FROM dbo.Revisiones
    WHERE CodigoRevision = N'REV-2026-0001'
)
BEGIN
    INSERT INTO dbo.Revisiones (CodigoRevision, Modulo, Estado, Observaciones)
    VALUES (
        N'REV-2026-0001',
        N'Planificacion Institucional',
        N'OBSERVADO',
        N'Se requieren ajustes en los indicadores de gestion.'
    );
END;
GO

DECLARE @RevisionId INT;
SELECT @RevisionId = RevisionId
FROM dbo.Revisiones
WHERE CodigoRevision = N'REV-2026-0001';

IF @RevisionId IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM dbo.Auditorias
    WHERE RevisionId = @RevisionId AND Tipo = N'Funcional'
)
BEGIN
    INSERT INTO dbo.Auditorias (RevisionId, Tipo, Resultado, Responsable)
    VALUES (@RevisionId, N'Funcional', N'CON_HALLAZGOS', N'Equipo QA SNP');
END;
GO