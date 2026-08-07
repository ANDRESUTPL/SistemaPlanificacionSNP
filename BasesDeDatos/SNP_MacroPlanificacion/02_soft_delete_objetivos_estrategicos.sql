USE [SNP_MacroPlanificacion];
GO

IF COL_LENGTH('dbo.ObjetivosEstrategicos', 'IsDeleted') IS NULL
BEGIN
    ALTER TABLE dbo.ObjetivosEstrategicos
    ADD IsDeleted bit NOT NULL
        CONSTRAINT DF_ObjetivosEstrategico_IsDeleted DEFAULT (0);
END
GO

IF COL_LENGTH('dbo.ObjetivosEstrategicos', 'DeletedAtUtc') IS NULL
BEGIN
    ALTER TABLE dbo.ObjetivosEstrategicos
    ADD DeletedAtUtc datetime2(7) NULL;
END
GO

IF COL_LENGTH('dbo.ObjetivosEstrategicos', 'DeletedBy') IS NULL
BEGIN
    ALTER TABLE dbo.ObjetivosEstrategicos
    ADD DeletedBy nvarchar(100) NULL;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_ObjetivosEstrategico_Activos_Plan_Codigo'
            AND object_id = OBJECT_ID('dbo.ObjetivosEstrategicos')
)
BEGIN
    CREATE INDEX IX_ObjetivosEstrategico_Activos_Plan_Codigo
                ON dbo.ObjetivosEstrategicos (PlanNacionalId, Codigo)
        WHERE IsDeleted = 0;
END
GO
