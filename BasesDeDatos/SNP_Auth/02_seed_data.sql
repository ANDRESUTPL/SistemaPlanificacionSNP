-- =====================================================================
-- Script de Datos de Prueba para SistemaPlanificacionSNP
-- Base de Datos: SNP_Auth
-- =====================================================================

USE [SNP_Auth];
GO

-- 1. LIMPIAR DATOS EXISTENTES (Para permitir re-ejecución del script)
-- Se eliminan en orden inverso a las dependencias para evitar errores de Foreign Keys
PRINT 'Limpiando datos existentes...';
DELETE FROM [dbo].[AuditoriaTransaccional];
DELETE FROM [dbo].[RolPermiso];
DELETE FROM [dbo].[UsuarioRol];
DELETE FROM [dbo].[Pantalla];
DELETE FROM [dbo].[Rol];
DELETE FROM [dbo].[Usuario];

-- 2. REINICIAR CONTADORES DE IDENTIDAD
DBCC CHECKIDENT ('[dbo].[AuditoriaTransaccional]', RESEED, 0);
DBCC CHECKIDENT ('[dbo].[RolPermiso]', RESEED, 0);
DBCC CHECKIDENT ('[dbo].[UsuarioRol]', RESEED, 0);
DBCC CHECKIDENT ('[dbo].[Pantalla]', RESEED, 0);
DBCC CHECKIDENT ('[dbo].[Rol]', RESEED, 0);
DBCC CHECKIDENT ('[dbo].[Usuario]', RESEED, 0);
GO

PRINT 'Insertando nuevos datos de prueba...';

-- =====================================================================
-- 3. INSERTAR ROLES
-- =====================================================================
INSERT INTO [dbo].[Rol] ([Nombre], [Descripcion], [Activo])
VALUES 
('Administrador', 'Acceso total a todos los módulos del sistema', 1),
('Auditor', 'Acceso de solo lectura para revisar registros y auditorías', 1),
('Operador', 'Acceso a los módulos de planificación operativa', 1);

-- =====================================================================
-- 4. INSERTAR USUARIOS
-- =====================================================================
-- Nota: El PasswordHash corresponde a la contraseña "Admin123!" en formato BCrypt
DECLARE @DefaultPasswordHash NVARCHAR(MAX) = '$2a$12$IRuK7pfP95addxH4k9nQhOReDzSZeRAyGoYd6hzG3.7q/4JitBBJi'; 

INSERT INTO [dbo].[Usuario] ([NombreUsuario], [Email], [PasswordHash], [Nombre], [Apellido], [Activo])
VALUES 
('admin', 'admin@snp.com', @DefaultPasswordHash, 'Carlos', 'Administrador', 1),
('auditor.juan', 'juan.auditor@snp.com', @DefaultPasswordHash, 'Juan', 'Pérez', 1),
('operador.maria', 'maria.operador@snp.com', @DefaultPasswordHash, 'María', 'Gómez', 1),
('inactivo.user', 'baja@snp.com', @DefaultPasswordHash, 'Usuario', 'Desactivado', 0);

-- =====================================================================
-- 5. INSERTAR PANTALLAS (Menús del sistema)
-- =====================================================================
-- Nivel 1: Módulos Principales (Padres)
INSERT INTO [dbo].[Pantalla] ([Nombre], [Ruta], [Icono], [PantallaPadrId], [Orden], [Activo])
VALUES 
('Dashboard', '/dashboard', 'home', NULL, 1, 1),             -- PantallaId: 1
('Planificación', '/planificacion', 'calendar', NULL, 2, 1), -- PantallaId: 2
('Seguridad', '/seguridad', 'shield', NULL, 99, 1);          -- PantallaId: 3

-- Nivel 2: Sub-Módulos (Hijos)
-- Hijas de Planificación (Padre = 2)
INSERT INTO [dbo].[Pantalla] ([Nombre], [Ruta], [Icono], [PantallaPadrId], [Orden], [Activo])
VALUES 
('Proyectos', '/planificacion/proyectos', 'briefcase', 2, 1, 1), -- PantallaId: 4
('Tareas', '/planificacion/tareas', 'check-square', 2, 2, 1);    -- PantallaId: 5

-- Hijas de Seguridad (Padre = 3)
INSERT INTO [dbo].[Pantalla] ([Nombre], [Ruta], [Icono], [PantallaPadrId], [Orden], [Activo])
VALUES 
('Usuarios', '/seguridad/usuarios', 'users', 3, 1, 1),           -- PantallaId: 6
('Roles', '/seguridad/roles', 'key', 3, 2, 1),                   -- PantallaId: 7
('Auditoría', '/seguridad/auditoria', 'file-text', 3, 3, 1);     -- PantallaId: 8

-- =====================================================================
-- 6. INSERTAR ASIGNACIONES USUARIO - ROL
-- =====================================================================
INSERT INTO [dbo].[UsuarioRol] ([UsuarioId], [RolId])
VALUES 
(1, 1), -- admin -> Administrador
(2, 2), -- auditor.juan -> Auditor
(3, 3); -- operador.maria -> Operador

-- =====================================================================
-- 7. INSERTAR PERMISOS DE ROLES A PANTALLAS
-- =====================================================================
-- 7.1 Permisos Administrador (RolId = 1): Acceso total a todo
INSERT INTO [dbo].[RolPermiso] ([RolId], [PantallaId], [Lectura], [Creacion], [Edicion], [Eliminacion])
SELECT 1, PantallaId, 1, 1, 1, 1 FROM [dbo].[Pantalla];

-- 7.2 Permisos Auditor (RolId = 2): Solo lectura a Dashboard, Proyectos y Auditoría
INSERT INTO [dbo].[RolPermiso] ([RolId], [PantallaId], [Lectura], [Creacion], [Edicion], [Eliminacion])
VALUES 
(2, 1, 1, 0, 0, 0), -- Dashboard
(2, 2, 1, 0, 0, 0), -- Planificacion (Padre)
(2, 4, 1, 0, 0, 0), -- Proyectos
(2, 3, 1, 0, 0, 0), -- Seguridad (Padre)
(2, 8, 1, 0, 0, 0); -- Auditoría

-- 7.3 Permisos Operador (RolId = 3): Acceso operativo a proyectos y tareas
INSERT INTO [dbo].[RolPermiso] ([RolId], [PantallaId], [Lectura], [Creacion], [Edicion], [Eliminacion])
VALUES 
(3, 1, 1, 0, 0, 0), -- Dashboard (Solo lectura)
(3, 2, 1, 0, 0, 0), -- Planificacion (Padre)
(3, 4, 1, 1, 1, 0), -- Proyectos (Crea y edita, no elimina)
(3, 5, 1, 1, 1, 1); -- Tareas (Acceso total a tareas)

-- =====================================================================
-- 8. INSERTAR AUDITORÍA TRANSACCIONAL DE EJEMPLO
-- =====================================================================
INSERT INTO [dbo].[AuditoriaTransaccional] ([UsuarioId], [Entidad], [TipoOperacion], [IdRegistro], [DatosAnteriores], [DatosNuevos], [Descripcion])
VALUES 
(1, 'Usuario', 'CREATE', 2, NULL, '{"NombreUsuario":"auditor.juan", "Activo":true}', 'Creación del usuario auditor inicial'),
(1, 'Usuario', 'CREATE', 3, NULL, '{"NombreUsuario":"operador.maria", "Activo":true}', 'Creación de operador de turno'),
(1, 'UsuarioRol', 'CREATE', 2, NULL, '{"UsuarioId":2, "RolId":2}', 'Asignación de rol Auditor a Juan'),
(1, 'Pantalla', 'UPDATE', 1, '{"Orden": 0}', '{"Orden": 1}', 'Ajuste de orden en menú Dashboard');

PRINT 'Datos de prueba insertados exitosamente.';
GO