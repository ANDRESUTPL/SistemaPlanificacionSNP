USE [SNP_PlanificacionInstitucional];
GO

IF OBJECT_ID(N'dbo.RespaldosEjecucion', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RespaldosEjecucion (
        RespaldoEjecucionId INT IDENTITY(1,1) NOT NULL,
        ProyectoInversionId INT NOT NULL,
        NombreArchivo NVARCHAR(255) NOT NULL,
        RutaArchivo NVARCHAR(500) NOT NULL,
        TipoContenido NVARCHAR(150) NOT NULL,
        TamanoBytes BIGINT NOT NULL,
        FechaCarga DATETIME2 NOT NULL CONSTRAINT DF_RespaldosEjecucion_FechaCarga DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_RespaldosEjecucion PRIMARY KEY (RespaldoEjecucionId),
        CONSTRAINT FK_RespaldosEjecucion_ProyectosInversion FOREIGN KEY (ProyectoInversionId)
            REFERENCES dbo.ProyectosInversion (ProyectoInversionId)
            ON DELETE CASCADE
    );

    CREATE INDEX IX_RespaldosEjecucion_ProyectoInversionId
        ON dbo.RespaldosEjecucion (ProyectoInversionId);
END;
GO
