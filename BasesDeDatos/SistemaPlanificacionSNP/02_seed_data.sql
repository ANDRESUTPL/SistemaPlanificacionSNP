USE [SistemaPlanificacionSNP];
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = N'Administrador')
BEGIN
    INSERT INTO dbo.Roles (Nombre, Descripcion)
    VALUES (N'Administrador', N'Acceso total al sistema');
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = N'Analista')
BEGIN
    INSERT INTO dbo.Roles (Nombre, Descripcion)
    VALUES (N'Analista', N'Consulta y seguimiento de planes');
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE Email = N'admin@snp.gob.ec')
BEGIN
    INSERT INTO dbo.Usuarios (Nombres, Apellidos, Email)
    VALUES (N'Usuario', N'Administrador', N'admin@snp.gob.ec');
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Catalogos WHERE Codigo = N'ESTADO_PLAN')
BEGIN
    INSERT INTO dbo.Catalogos (Codigo, Nombre, Descripcion)
    VALUES (N'ESTADO_PLAN', N'Estado de Plan', N'Catalogo de estados de los planes institucionales');
END;
GO

DECLARE @CatalogoEstadoPlanId INT;
SELECT @CatalogoEstadoPlanId = CatalogoId
FROM dbo.Catalogos
WHERE Codigo = N'ESTADO_PLAN';

IF @CatalogoEstadoPlanId IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM dbo.ItemsCatalogo
    WHERE CatalogoId = @CatalogoEstadoPlanId
      AND Codigo = N'BORRADOR'
)
BEGIN
    INSERT INTO dbo.ItemsCatalogo (CatalogoId, Codigo, Nombre, Valor, Orden)
    VALUES (@CatalogoEstadoPlanId, N'BORRADOR', N'Borrador', N'BORRADOR', 1);
END;
GO

DECLARE @CatalogoEstadoPlanId INT;
SELECT @CatalogoEstadoPlanId = CatalogoId
FROM dbo.Catalogos
WHERE Codigo = N'ESTADO_PLAN';

IF @CatalogoEstadoPlanId IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM dbo.ItemsCatalogo
    WHERE CatalogoId = @CatalogoEstadoPlanId
      AND Codigo = N'APROBADO'
)
BEGIN
    INSERT INTO dbo.ItemsCatalogo (CatalogoId, Codigo, Nombre, Valor, Orden)
    VALUES (@CatalogoEstadoPlanId, N'APROBADO', N'Aprobado', N'APROBADO', 2);
END;
GO