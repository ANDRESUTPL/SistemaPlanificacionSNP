USE [SNP_Parametrizacion];
GO

IF COL_LENGTH('dbo.EntidadesPublicas', 'Tipo') IS NULL
BEGIN
    ALTER TABLE [dbo].[EntidadesPublicas]
    ADD [Tipo] [nvarchar](100) NULL;

    UPDATE [dbo].[EntidadesPublicas]
    SET [Tipo] = 'Ministerio'
    WHERE [Tipo] IS NULL;

    ALTER TABLE [dbo].[EntidadesPublicas]
    ALTER COLUMN [Tipo] [nvarchar](100) NOT NULL;
END;
GO

IF COL_LENGTH('dbo.EntidadesPublicas', 'NivelGobierno') IS NULL
BEGIN
    ALTER TABLE [dbo].[EntidadesPublicas]
    ADD [NivelGobierno] [nvarchar](100) NULL;

    UPDATE [dbo].[EntidadesPublicas]
    SET [NivelGobierno] = 'Gobierno Central'
    WHERE [NivelGobierno] IS NULL;

    ALTER TABLE [dbo].[EntidadesPublicas]
    ALTER COLUMN [NivelGobierno] [nvarchar](100) NOT NULL;
END;
GO