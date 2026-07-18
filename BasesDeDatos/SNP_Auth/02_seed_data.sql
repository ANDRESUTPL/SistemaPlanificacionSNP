-- =====================================================================
-- SCRIPT MAESTRO DE INICIALIZACIÓN (SEED DATA)
-- Sistema de Planificación SNP
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

PRINT 'Iniciando carga de configuración base...';

-- =====================================================================
-- 2. INSERTAR ROLES DEL SISTEMA
-- =====================================================================
SET IDENTITY_INSERT [dbo].[Rol] ON;

INSERT INTO [dbo].[Rol] ([RolId], [Nombre], [Descripcion], [Activo])
VALUES 
(1, 'Administrador', 'Acceso total a todos los módulos y configuraciones del sistema SNP.', 1),
(2, 'Auditor', 'Acceso de solo lectura para revisar registros, auditorías y trazabilidad.', 1),
(3, 'Operador Institucional', 'Acceso operativo para carga de proyectos y avances en entidades públicas.', 1);

SET IDENTITY_INSERT [dbo].[Rol] OFF;

-- =====================================================================
-- 3. INSERTAR USUARIO SUPER ADMIN
-- =====================================================================
-- Nota: Este hash corresponde a la contraseña por defecto de tu sistema
DECLARE @DefaultPasswordHash NVARCHAR(MAX) = '$2a$12$IRuK7pfP95addxH4k9nQhOReDzSZeRAyGoYd6hzG3.7q/4JitBBJi'; 

SET IDENTITY_INSERT [dbo].[Usuario] ON;

INSERT INTO [dbo].[Usuario] ([UsuarioId], [NombreUsuario], [Email], [PasswordHash], [Nombre], [Apellido], [Activo])
VALUES 
(1, 'admin', 'admin@snp.gob.ec', @DefaultPasswordHash, 'Super', 'Administrador', 1);

SET IDENTITY_INSERT [dbo].[Usuario] OFF;

-- Asignar el Rol de Administrador al usuario 'admin'
INSERT INTO [dbo].[UsuarioRol] ([UsuarioId], [RolId])
VALUES (1, 1);

-- =====================================================================
-- 4. CONSTRUCCIÓN DEL MENÚ DINÁMICO (PANTALLAS)
-- =====================================================================
SET IDENTITY_INSERT [dbo].[Pantalla] ON;

-- ---------------------------------------------------------
-- NIVEL 0: HOME / DASHBOARD GLOBAL
-- ---------------------------------------------------------
INSERT INTO [dbo].[Pantalla] ([PantallaId], [Nombre], [Ruta], [Icono], [PantallaPadrId], [Orden], [Activo])
VALUES (1, 'Dashboard', '/dashboard', 'home', NULL, 1, 1);

-- ---------------------------------------------------------
-- FASE 2: MACRO PLANIFICACIÓN
-- ---------------------------------------------------------
INSERT INTO [dbo].[Pantalla] ([PantallaId], [Nombre], [Ruta], [Icono], [PantallaPadrId], [Orden], [Activo])
VALUES (12, 'Macro Planificación', '/macroplanificacion', 'globe-americas', NULL, 2, 1);

INSERT INTO [dbo].[Pantalla] ([PantallaId], [Nombre], [Ruta], [Icono], [PantallaPadrId], [Orden], [Activo])
VALUES (13, 'Plan Nacional (PND)', '/macroplanificacion/planes', 'flag', 12, 1, 1);

-- ---------------------------------------------------------
-- FASE 3: PLANIFICACIÓN INSTITUCIONAL
-- ---------------------------------------------------------
INSERT INTO [dbo].[Pantalla] ([PantallaId], [Nombre], [Ruta], [Icono], [PantallaPadrId], [Orden], [Activo])
VALUES (2, 'Planificación', '/planificacion', 'building', NULL, 3, 1);

INSERT INTO [dbo].[Pantalla] ([PantallaId], [Nombre], [Ruta], [Icono], [PantallaPadrId], [Orden], [Activo])
VALUES (14, 'PEI y Proyectos', '/planificacion/institucional', 'folder-open', 2, 1, 1);

-- ---------------------------------------------------------
-- FASE 4: SEGUIMIENTO Y CONTROL
-- ---------------------------------------------------------
INSERT INTO [dbo].[Pantalla] ([PantallaId], [Nombre], [Ruta], [Icono], [PantallaPadrId], [Orden], [Activo])
VALUES (20, 'Seguimiento y Control', '/controlcalidad', 'check-double', NULL, 4, 1);

INSERT INTO [dbo].[Pantalla] ([PantallaId], [Nombre], [Ruta], [Icono], [PantallaPadrId], [Orden], [Activo])
VALUES (21, 'Revisiones Técnicas', '/controlcalidad/revisiones', 'clipboard-check', 20, 1, 1);

-- ---------------------------------------------------------
-- FASE 5: EVALUACIÓN Y REPORTES
-- ---------------------------------------------------------
INSERT INTO [dbo].[Pantalla] ([PantallaId], [Nombre], [Ruta], [Icono], [PantallaPadrId], [Orden], [Activo])
VALUES (25, 'Evaluación y Reportes', '/evaluacion', 'chart-bar', NULL, 5, 1);

INSERT INTO [dbo].[Pantalla] ([PantallaId], [Nombre], [Ruta], [Icono], [PantallaPadrId], [Orden], [Activo])
VALUES (26, 'Dashboard Ejecutivo', '/evaluacion/dashboard', 'chart-pie', 25, 1, 1),
       (27, 'Carga de Avances', '/evaluacion/avances', 'tasks', 25, 2, 1);

-- ---------------------------------------------------------
-- FASE 1: PARAMETRIZACIÓN BASE
-- ---------------------------------------------------------
INSERT INTO [dbo].[Pantalla] ([PantallaId], [Nombre], [Ruta], [Icono], [PantallaPadrId], [Orden], [Activo])
VALUES (9, 'Parametrización', '/parametrizacion', 'cogs', NULL, 6, 1);

INSERT INTO [dbo].[Pantalla] ([PantallaId], [Nombre], [Ruta], [Icono], [PantallaPadrId], [Orden], [Activo])
VALUES (10, 'Catálogos Maestros', '/parametrizacion/catalogos', 'list-ul', 9, 1, 1),
       (11, 'Entidades Públicas', '/parametrizacion/instituciones', 'landmark', 9, 2, 1);

-- ---------------------------------------------------------
-- SEGURIDAD Y ADMINISTRACIÓN
-- ---------------------------------------------------------
INSERT INTO [dbo].[Pantalla] ([PantallaId], [Nombre], [Ruta], [Icono], [PantallaPadrId], [Orden], [Activo])
VALUES (3, 'Seguridad', '/seguridad', 'shield-alt', NULL, 99, 1);

INSERT INTO [dbo].[Pantalla] ([PantallaId], [Nombre], [Ruta], [Icono], [PantallaPadrId], [Orden], [Activo])
VALUES (6, 'Usuarios', '/seguridad/usuarios', 'users', 3, 1, 1),
       (7, 'Roles y Permisos', '/seguridad/catalogo-roles', 'user-shield', 3, 2, 1),
       (8, 'Auditoría', '/seguridad/auditoria', 'file-contract', 3, 3, 1);

SET IDENTITY_INSERT [dbo].[Pantalla] OFF;

-- =====================================================================
-- 5. ASIGNACIÓN DE PERMISOS AL ADMINISTRADOR (Acceso Total)
-- =====================================================================
PRINT 'Configurando matriz de permisos...';

-- El Rol Administrador (RolId = 1) recibe CRUD total (Lectura, Creacion, Edicion, Eliminacion) para TODAS las pantallas insertadas.
INSERT INTO [dbo].[RolPermiso] ([RolId], [PantallaId], [Lectura], [Creacion], [Edicion], [Eliminacion])
SELECT 1, PantallaId, 1, 1, 1, 1 FROM [dbo].[Pantalla];

PRINT '=======================================================';
PRINT '¡Configuración inicial completada con éxito!';
PRINT 'El sistema está listo para operar.';
PRINT '=======================================================';
GO