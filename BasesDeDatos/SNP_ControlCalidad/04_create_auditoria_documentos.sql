USE [SNP_ControlCalidad];
GO

IF OBJECT_ID(N'dbo.AuditoriaDocumentos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditoriaDocumentos (
        AuditoriaDocumentoId INT IDENTITY(1,1) NOT NULL,
        AuditoriaId INT NOT NULL,
        NombreArchivo NVARCHAR(255) NOT NULL,
        RutaArchivo NVARCHAR(500) NOT NULL,
        TipoContenido NVARCHAR(150) NOT NULL,
        TamanoBytes BIGINT NOT NULL,
        FechaCarga DATETIME2 NOT NULL CONSTRAINT DF_CC_AuditoriaDocumentos_FechaCarga DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_CC_AuditoriaDocumentos PRIMARY KEY (AuditoriaDocumentoId),
        CONSTRAINT FK_CC_AuditoriaDocumentos_Auditorias FOREIGN KEY (AuditoriaId)
            REFERENCES dbo.Auditorias(AuditoriaId)
            ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_AuditoriaDocumentos_AuditoriaId'
      AND object_id = OBJECT_ID(N'dbo.AuditoriaDocumentos'))
BEGIN
    CREATE INDEX IX_AuditoriaDocumentos_AuditoriaId
        ON dbo.AuditoriaDocumentos (AuditoriaId);
END;
GO