USE [SNP_Parametrizacion];
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Catalogos WHERE Codigo = N'TIPO_ENTIDAD')
BEGIN
    INSERT INTO dbo.Catalogos (Codigo, Nombre)
    VALUES (N'TIPO_ENTIDAD', N'Tipo de Entidad Publica');
END;
GO

DECLARE @CatalogoTipoEntidadId INT;
SELECT @CatalogoTipoEntidadId = CatalogoId
FROM dbo.Catalogos
WHERE Codigo = N'TIPO_ENTIDAD';

IF @CatalogoTipoEntidadId IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM dbo.ItemsCatalogo
    WHERE CatalogoId = @CatalogoTipoEntidadId AND Codigo = N'MINISTERIO'
)
BEGIN
    INSERT INTO dbo.ItemsCatalogo (CatalogoId, Codigo, Nombre, Valor, Orden)
    VALUES (@CatalogoTipoEntidadId, N'MINISTERIO', N'Ministerio', N'MINISTERIO', 1);
END;

IF @CatalogoTipoEntidadId IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM dbo.ItemsCatalogo
    WHERE CatalogoId = @CatalogoTipoEntidadId AND Codigo = N'GAD'
)
BEGIN
    INSERT INTO dbo.ItemsCatalogo (CatalogoId, Codigo, Nombre, Valor, Orden)
    VALUES (@CatalogoTipoEntidadId, N'GAD', N'Gobierno Autonomo Descentralizado', N'GAD', 2);
END;
GO