-- =====================================================================
-- SCRIPT DE SEED DATA REALISTA
-- Base de Datos: SistemaPlanificacionSNP (Catálogos y Usuarios legacy/generales)
-- =====================================================================

USE [SistemaPlanificacionSNP];
GO

PRINT 'Limpiando base de datos SistemaPlanificacionSNP...';
DELETE FROM [dbo].[ItemsCatalogo];
DELETE FROM [dbo].[Catalogos];
DELETE FROM [dbo].[Roles];
DELETE FROM [dbo].[Usuarios];
GO

PRINT 'Insertando datos de Usuarios y Roles...';

SET IDENTITY_INSERT [dbo].[Usuarios] ON;
INSERT INTO [dbo].[Usuarios] ([UsuarioId], [Nombres], [Apellidos], [Email], [Activo], [FechaCreacion])
VALUES 
(1, 'Carlos', 'Administrador', 'admin@snp.gob.ec', 1, SYSUTCDATETIME()),
(2, 'Ana', 'Planificadora', 'ana.planificacion@msp.gob.ec', 1, SYSUTCDATETIME()),
(3, 'Luis', 'Auditor Externo', 'luis.auditor@cge.gob.ec', 1, SYSUTCDATETIME());
SET IDENTITY_INSERT [dbo].[Usuarios] OFF;

SET IDENTITY_INSERT [dbo].[Roles] ON;
INSERT INTO [dbo].[Roles] ([RolId], [Nombre], [Descripcion], [Activo])
VALUES 
(1, 'SuperAdmin', 'Administrador Global del Sistema', 1),
(2, 'Planificador_Institucional', 'Encargado de formular PEI y Proyectos', 1),
(3, 'Auditor_Calidad', 'Revisor de cumplimiento y calidad', 1);
SET IDENTITY_INSERT [dbo].[Roles] OFF;

PRINT 'Insertando datos de Catálogos...';

SET IDENTITY_INSERT [dbo].[Catalogos] ON;
INSERT INTO [dbo].[Catalogos] ([CatalogoId], [Codigo], [Nombre], [Descripcion], [Activo])
VALUES 
(1, 'SECTORES_ESTADO', 'Sectores del Estado', 'Clasificación de las entidades públicas por sector', 1),
(2, 'NIVELES_GOBIERNO', 'Niveles de Gobierno', 'Clasificación territorial y administrativa', 1);
SET IDENTITY_INSERT [dbo].[Catalogos] OFF;

SET IDENTITY_INSERT [dbo].[ItemsCatalogo] ON;
INSERT INTO [dbo].[ItemsCatalogo] ([ItemCatalogoId], [CatalogoId], [Codigo], [Nombre], [Valor], [Orden], [Activo])
VALUES 
(1, 1, 'SEC_SALUD', 'Sector Salud', 'Salud Pública y Saneamiento', 1, 1),
(2, 1, 'SEC_EDUCACION', 'Sector Educación', 'Educación, Ciencia y Tecnología', 2, 1),
(3, 1, 'SEC_SEGURIDAD', 'Sector Seguridad', 'Defensa y Seguridad Ciudadana', 3, 1),
(4, 2, 'NIV_CENTRAL', 'Gobierno Central', 'Ministerios y Secretarías de Estado', 1, 1),
(5, 2, 'NIV_GAD_PROV', 'GAD Provincial', 'Gobiernos Autónomos Provinciales', 2, 1),
(6, 2, 'NIV_GAD_MUN', 'GAD Municipal', 'Gobiernos Autónomos Municipales', 3, 1);
SET IDENTITY_INSERT [dbo].[ItemsCatalogo] OFF;

PRINT '¡Seed completado para SistemaPlanificacionSNP!';
GO