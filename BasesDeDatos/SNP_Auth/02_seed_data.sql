-- =====================================================================
-- SCRIPT MAESTRO DE INICIALIZACI�N (SEED DATA)
-- Sistema de Planificaci�n SNP
-- Base de Datos: SNP_Auth
-- Uso: Levantar el sistema en un entorno limpio / nueva computadora
-- =====================================================================

USE [SNP_Auth];
GO

-- =====================================================================
-- 1. LIMPIEZA DE DATOS (Orden inverso a las dependencias)
-- =====================================================================
PRINT 'Limpiando base de datos...';
DELETE FROM [dbo].[AuditoriaTransaccional];
DELETE FROM [dbo].[RolPermiso];
DELETE FROM [dbo].[UsuarioRol];
DELETE FROM [dbo].[Pantalla];
DELETE FROM [dbo].[Rol];
DELETE FROM [dbo].[Usuario];
GO

PRINT 'Iniciando carga de configuraci�n base...';

-- =====================================================================
-- 2. INSERTAR ROLES DEL SISTEMA
-- =====================================================================
SET IDENTITY_INSERT [dbo].[Rol] ON;

INSERT INTO [dbo].[Rol] ([RolId], [Nombre], [Descripcion], [Activo])
VALUES 
(1, 'Administrador', 'Acceso total a todos los m�dulos y configuraciones del sistema SNP.', 1),
(2, 'Auditor', 'Acceso de solo lectura para revisar registros, auditor�as y trazabilidad.', 1),
(3, 'Operador Institucional', 'Acceso operativo para carga de proyectos y avances en entidades p�blicas.', 1);

SET IDENTITY_INSERT [dbo].[Rol] OFF;

-- =====================================================================
-- 3. INSERTAR USUARIO SUPER ADMIN
-- =====================================================================
-- Nota: Este hash corresponde a la contrase�a por defecto de tu sistema Admin123
DECLARE @DefaultPasswordHash NVARCHAR(MAX) = '$2a$12$ueevcvKK6ZmxkJyZY3UvFuX4cmSELjFFxHbyV6CTJAo3BIMxvMbka'; 

SET IDENTITY_INSERT [dbo].[Usuario] ON;

INSERT INTO [dbo].[Usuario] ([UsuarioId], [NombreUsuario], [Email], [PasswordHash], [Nombre], [Apellido], [Activo])
VALUES 
(1, 'admin', 'admin@snp.gob.ec', @DefaultPasswordHash, 'Super', 'Administrador', 1);

SET IDENTITY_INSERT [dbo].[Usuario] OFF;

-- Asignar el Rol de Administrador al usuario 'admin'
INSERT INTO [dbo].[UsuarioRol] ([UsuarioId], [RolId])
VALUES (1, 1);

-- =====================================================================
-- 4. CONSTRUCCI�N DEL MEN� DIN�MICO (PANTALLAS)
-- =====================================================================
SET IDENTITY_INSERT [dbo].[Pantalla] ON;

-- ---------------------------------------------------------
-- NIVEL 0: HOME / DASHBOARD GLOBAL
-- ---------------------------------------------------------
INSERT INTO [dbo].[Pantalla] ([PantallaId], [Nombre], [Ruta], [Icono], [PantallaPadrId], [Orden], [Activo])
VALUES (1, 'Dashboard', '/dashboard', 'home', NULL, 1, 1);

-- ---------------------------------------------------------
-- FASE 2: MACRO PLANIFICACI�N
-- ---------------------------------------------------------
INSERT INTO [dbo].[Pantalla] ([PantallaId], [Nombre], [Ruta], [Icono], [PantallaPadrId], [Orden], [Activo])
VALUES (12, 'Macro Planificaci�n', '/macroplanificacion', 'globe-americas', NULL, 2, 1);

INSERT INTO [dbo].[Pantalla] ([PantallaId], [Nombre], [Ruta], [Icono], [PantallaPadrId], [Orden], [Activo])
VALUES (13, 'Plan Nacional (PND)', '/macroplanificacion/planes', 'flag', 12, 1, 1);

-- ---------------------------------------------------------
-- FASE 3: PLANIFICACI�N INSTITUCIONAL
-- ---------------------------------------------------------
INSERT INTO [dbo].[Pantalla] ([PantallaId], [Nombre], [Ruta], [Icono], [PantallaPadrId], [Orden], [Activo])
VALUES (2, 'Planificaci�n', '/planificacion', 'building', NULL, 3, 1);

INSERT INTO [dbo].[Pantalla] ([PantallaId], [Nombre], [Ruta], [Icono], [PantallaPadrId], [Orden], [Activo])
VALUES (14, 'PEI y Proyectos', '/planificacion/institucional', 'folder-open', 2, 1, 1);

-- ---------------------------------------------------------
-- FASE 4: SEGUIMIENTO Y CONTROL
-- ---------------------------------------------------------
INSERT INTO [dbo].[Pantalla] ([PantallaId], [Nombre], [Ruta], [Icono], [PantallaPadrId], [Orden], [Activo])
VALUES (20, 'Seguimiento y Control', '/controlcalidad', 'check-double', NULL, 4, 1);

INSERT INTO [dbo].[Pantalla] ([PantallaId], [Nombre], [Ruta], [Icono], [PantallaPadrId], [Orden], [Activo])
VALUES (21, 'Revisiones T�cnicas', '/controlcalidad/revisiones', 'clipboard-check', 20, 1, 1);

-- ---------------------------------------------------------
-- FASE 5: EVALUACI�N Y REPORTES
-- ---------------------------------------------------------
INSERT INTO [dbo].[Pantalla] ([PantallaId], [Nombre], [Ruta], [Icono], [PantallaPadrId], [Orden], [Activo])
VALUES (25, 'Evaluaci�n y Reportes', '/evaluacion', 'chart-bar', NULL, 5, 1);

INSERT INTO [dbo].[Pantalla] ([PantallaId], [Nombre], [Ruta], [Icono], [PantallaPadrId], [Orden], [Activo])
VALUES (26, 'Dashboard Ejecutivo', '/evaluacion/dashboard', 'chart-pie', 25, 1, 1),
       (27, 'Carga de Avances', '/evaluacion/avances', 'tasks', 25, 2, 1);

-- ---------------------------------------------------------
-- FASE 1: PARAMETRIZACI�N BASE
-- ---------------------------------------------------------
INSERT INTO [dbo].[Pantalla] ([PantallaId], [Nombre], [Ruta], [Icono], [PantallaPadrId], [Orden], [Activo])
VALUES (9, 'Parametrizaci�n', '/parametrizacion', 'cogs', NULL, 6, 1);

INSERT INTO [dbo].[Pantalla] ([PantallaId], [Nombre], [Ruta], [Icono], [PantallaPadrId], [Orden], [Activo])
VALUES (10, 'Cat�logos Maestros', '/parametrizacion/catalogos', 'list-ul', 9, 1, 1),
       (11, 'Entidades P�blicas', '/parametrizacion/instituciones', 'landmark', 9, 2, 1);

-- ---------------------------------------------------------
-- SEGURIDAD Y ADMINISTRACI�N
-- ---------------------------------------------------------
INSERT INTO [dbo].[Pantalla] ([PantallaId], [Nombre], [Ruta], [Icono], [PantallaPadrId], [Orden], [Activo])
VALUES (3, 'Seguridad', '/seguridad', 'shield-alt', NULL, 99, 1);

INSERT INTO [dbo].[Pantalla] ([PantallaId], [Nombre], [Ruta], [Icono], [PantallaPadrId], [Orden], [Activo])
VALUES (6, 'Usuarios', '/seguridad/usuarios', 'users', 3, 1, 1),
       (7, 'Roles y Permisos', '/seguridad/catalogo-roles', 'user-shield', 3, 2, 1),
       (8, 'Auditor�a', '/seguridad/auditoria', 'file-contract', 3, 3, 1);

SET IDENTITY_INSERT [dbo].[Pantalla] OFF;

-- =====================================================================
-- 5. ASIGNACI�N DE PERMISOS AL ADMINISTRADOR (Acceso Total)
-- =====================================================================
PRINT 'Configurando matriz de permisos...';

-- El Rol Administrador (RolId = 1) recibe CRUD total (Lectura, Creacion, Edicion, Eliminacion) para TODAS las pantallas insertadas.
INSERT INTO [dbo].[RolPermiso] ([RolId], [PantallaId], [Lectura], [Creacion], [Edicion], [Eliminacion])
SELECT 1, PantallaId, 1, 1, 1, 1 FROM [dbo].[Pantalla];

PRINT '=======================================================';
PRINT '�Configuraci�n inicial completada con �xito!';
PRINT 'El sistema est� listo para operar.';
PRINT '=======================================================';
GO