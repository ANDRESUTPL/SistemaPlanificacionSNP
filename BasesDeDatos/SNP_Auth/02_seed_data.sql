USE [SNP_Auth];
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = N'Administrador')
BEGIN
    INSERT INTO dbo.Roles (Nombre) VALUES (N'Administrador');
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = N'Operador')
BEGIN
    INSERT INTO dbo.Roles (Nombre) VALUES (N'Operador');
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE Usuario = N'admin')
BEGIN
    INSERT INTO dbo.Usuarios (Usuario, Email, PasswordHash)
    VALUES (N'admin', N'admin@snp.gob.ec', N'REEMPLAZAR_HASH_SEGURO');
END;
GO

DECLARE @UsuarioAdminId INT;
DECLARE @RolAdminId INT;

SELECT @UsuarioAdminId = UsuarioId FROM dbo.Usuarios WHERE Usuario = N'admin';
SELECT @RolAdminId = RolId FROM dbo.Roles WHERE Nombre = N'Administrador';

IF @UsuarioAdminId IS NOT NULL
AND @RolAdminId IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM dbo.UsuarioRoles
    WHERE UsuarioId = @UsuarioAdminId AND RolId = @RolAdminId
)
BEGIN
    INSERT INTO dbo.UsuarioRoles (UsuarioId, RolId)
    VALUES (@UsuarioAdminId, @RolAdminId);
END;
GO