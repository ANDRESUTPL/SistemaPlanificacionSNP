-- =====================================================================
-- Script de Datos de Prueba para SistemaPlanificacionSNP (Corregido)
-- Base de Datos: SNP_Auth
-- =====================================================================

USE [SNP_Auth];
GO

-- 1. LIMPIAR DATOS EXISTENTES
PRINT 'Limpiando datos existentes...';
DELETE FROM [dbo].[AuditoriaTransaccional];
DELETE FROM [dbo].[RolPermiso];
DELETE FROM [dbo].[UsuarioRol];
DELETE FROM [dbo].[Pantalla];
DELETE FROM [dbo].[Rol];
DELETE FROM [dbo].[Usuario];
GO

PRINT 'Insertando nuevos datos de prueba...';

-- =====================================================================
-- 2. INSERTAR ROLES (Forzando los IDs 1, 2 y 3)
-- =====================================================================
SET IDENTITY_INSERT [dbo].[Rol] ON;

INSERT INTO [dbo].[Rol] ([RolId], [Nombre], [Descripcion], [Activo])
VALUES 
(1, 'Administrador', 'Acceso total a todos los módulos del sistema', 1),
(2, 'Auditor', 'Acceso de solo lectura para revisar registros y auditorías', 1),
(3, 'Operador', 'Acceso a los módulos de planificación operativa', 1);

SET IDENTITY_INSERT [dbo].[Rol] OFF;

-- =====================================================================
-- 3. INSERTAR USUARIOS (Forzando los IDs 1, 2, 3 y 4)
-- =====================================================================
DECLARE @DefaultPasswordHash NVARCHAR(MAX) = '$2a$12$IRuK7pfP95addxH4k9nQhOReDzSZeRAyGoYd6hzG3.7q/4JitBBJi'; 

SET IDENTITY_INSERT [dbo].[Usuario] ON;

INSERT INTO [dbo].[Usuario] ([UsuarioId], [NombreUsuario], [Email], [PasswordHash], [Nombre], [Apellido], [Activo])
VALUES 
(1, 'admin', 'admin@snp.com', @DefaultPasswordHash, 'Carlos', 'Administrador', 1),
(2, 'auditor.juan', 'juan.auditor@snp.com', @DefaultPasswordHash, 'Juan', 'Pérez', 1),
(3, 'operador.maria', 'maria.operador@snp.com', @DefaultPasswordHash, 'María', 'Gómez', 1),
(4, 'inactivo.user', 'baja@snp.com', @DefaultPasswordHash, 'Usuario', 'Desactivado', 0);

SET IDENTITY_INSERT [dbo].[Usuario] OFF;

-- =====================================================================
-- 4. INSERTAR PANTALLAS (Forzando los IDs)
-- =====================================================================
SET IDENTITY_INSERT [dbo].[Pantalla] ON;

-- Nivel 1: Módulos Principales (Padres)
INSERT INTO [dbo].[Pantalla] ([PantallaId], [Nombre], [Ruta], [Icono], [PantallaPadrId], [Orden], [Activo])
VALUES 
(1, 'Dashboard', '/dashboard', 'home', NULL, 1, 1),             
(2, 'Planificación', '/planificacion', 'calendar', NULL, 2, 1), 
(3, 'Seguridad', '/seguridad', 'shield', NULL, 99, 1);          

-- Nivel 2: Sub-Módulos (Hijos)
INSERT INTO [dbo].[Pantalla] ([PantallaId], [Nombre], [Ruta], [Icono], [PantallaPadrId], [Orden], [Activo])
VALUES 
(4, 'Proyectos', '/planificacion/proyectos', 'briefcase', 2, 1, 1), 
(5, 'Tareas', '/planificacion/tareas', 'check-square', 2, 2, 1),    
(6, 'Usuarios', '/seguridad/usuarios', 'users', 3, 1, 1),           
(7, 'Roles', '/seguridad/roles', 'key', 3, 2, 1),                   
(8, 'Auditoría', '/seguridad/auditoria', 'file-text', 3, 3, 1);     

SET IDENTITY_INSERT [dbo].[Pantalla] OFF;

-- =====================================================================
-- 5. INSERTAR ASIGNACIONES USUARIO - ROL
-- =====================================================================
INSERT INTO [dbo].[UsuarioRol] ([UsuarioId], [RolId])
VALUES 
(1, 1), -- admin -> Administrador
(2, 2), -- auditor.juan -> Auditor
(3, 3); -- operador.maria -> Operador

-- =====================================================================
-- 6. INSERTAR PERMISOS DE ROLES A PANTALLAS
-- =====================================================================
-- 6.1 Permisos Administrador (RolId = 1): Acceso total a todo
INSERT INTO [dbo].[RolPermiso] ([RolId], [PantallaId], [Lectura], [Creacion], [Edicion], [Eliminacion])
SELECT 1, PantallaId, 1, 1, 1, 1 FROM [dbo].[Pantalla];

-- 6.2 Permisos Auditor (RolId = 2): Solo lectura a Dashboard, Proyectos y Auditoría
INSERT INTO [dbo].[RolPermiso] ([RolId], [PantallaId], [Lectura], [Creacion], [Edicion], [Eliminacion])
VALUES 
(2, 1, 1, 0, 0, 0), -- Dashboard
(2, 2, 1, 0, 0, 0), -- Planificacion (Padre)
(2, 4, 1, 0, 0, 0), -- Proyectos
(2, 3, 1, 0, 0, 0), -- Seguridad (Padre)
(2, 8, 1, 0, 0, 0); -- Auditoría

-- 6.3 Permisos Operador (RolId = 3): Acceso operativo a proyectos y tareas
INSERT INTO [dbo].[RolPermiso] ([RolId], [PantallaId], [Lectura], [Creacion], [Edicion], [Eliminacion])
VALUES 
(3, 1, 1, 0, 0, 0), -- Dashboard (Solo lectura)
(3, 2, 1, 0, 0, 0), -- Planificacion (Padre)
(3, 4, 1, 1, 1, 0), -- Proyectos (Crea y edita, no elimina)
(3, 5, 1, 1, 1, 1); -- Tareas (Acceso total a tareas)

-- =====================================================================
-- 7. INSERTAR AUDITORÍA TRANSACCIONAL DE EJEMPLO
-- =====================================================================
INSERT INTO [dbo].[AuditoriaTransaccional] ([UsuarioId], [Entidad], [TipoOperacion], [IdRegistro], [DatosAnteriores], [DatosNuevos], [Descripcion])
VALUES 
(1, 'Usuario', 'CREATE', 2, NULL, '{"NombreUsuario":"auditor.juan", "Activo":true}', 'Creación del usuario auditor inicial'),
(1, 'Usuario', 'CREATE', 3, NULL, '{"NombreUsuario":"operador.maria", "Activo":true}', 'Creación de operador de turno'),
(1, 'UsuarioRol', 'CREATE', 2, NULL, '{"UsuarioId":2, "RolId":2}', 'Asignación de rol Auditor a Juan'),
(1, 'Pantalla', 'UPDATE', 1, '{"Orden": 0}', '{"Orden": 1}', 'Ajuste de orden en menú Dashboard');

PRINT 'Datos de prueba insertados exitosamente.';
GO