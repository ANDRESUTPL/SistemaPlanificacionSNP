-- =====================================================================
-- Script de Creación de Base de Datos para SistemaPlanificacionSNP
-- Base de Datos: SNP_Auth
-- =====================================================================

-- 1. Crear la Base de Datos
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'SNP_Auth')
BEGIN
    CREATE DATABASE [SNP_Auth];
END
GO

USE [SNP_Auth];
GO

-- =====================================================================
-- TABLAS PRINCIPALES (Sin Dependencias)
-- =====================================================================

-- Tabla: Usuario
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Usuario]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Usuario] (
        [UsuarioId] INT IDENTITY(1,1) NOT NULL,
        [NombreUsuario] NVARCHAR(100) NOT NULL,
        [Email] NVARCHAR(256) NOT NULL,
        [PasswordHash] NVARCHAR(MAX) NOT NULL,
        [Nombre] NVARCHAR(100) NOT NULL,
        [Apellido] NVARCHAR(100) NOT NULL,
        [Activo] BIT NOT NULL CONSTRAINT [DF_Usuario_Activo] DEFAULT (1),
        [FechaCreacion] DATETIME2 NOT NULL CONSTRAINT [DF_Usuario_FechaCreacion] DEFAULT (GETUTCDATE()),
        [FechaUltimoLogin] DATETIME2 NULL,
        [RefreshToken] NVARCHAR(500) NULL,
        [RefreshTokenExpiracion] DATETIME2 NULL,
        
        CONSTRAINT [PK_Usuario] PRIMARY KEY CLUSTERED ([UsuarioId] ASC)
    );
END
GO

-- Tabla: Rol
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Rol]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Rol] (
        [RolId] INT IDENTITY(1,1) NOT NULL,
        [Nombre] NVARCHAR(100) NOT NULL,
        [Descripcion] NVARCHAR(500) NOT NULL,
        [Activo] BIT NOT NULL CONSTRAINT [DF_Rol_Activo] DEFAULT (1),
        [FechaCreacion] DATETIME2 NOT NULL CONSTRAINT [DF_Rol_FechaCreacion] DEFAULT (GETUTCDATE()),
        
        CONSTRAINT [PK_Rol] PRIMARY KEY CLUSTERED ([RolId] ASC)
    );
END
GO

-- Tabla: Pantalla
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Pantalla]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Pantalla] (
        [PantallaId] INT IDENTITY(1,1) NOT NULL,
        [Nombre] NVARCHAR(100) NOT NULL,
        [Ruta] NVARCHAR(256) NOT NULL,
        [Icono] NVARCHAR(100) NOT NULL,
        [PantallaPadrId] INT NULL, -- Se mantiene el nombre exacto de la entidad de C# (le falta la 'e')
        [Orden] INT NOT NULL,
        [Activo] BIT NOT NULL CONSTRAINT [DF_Pantalla_Activo] DEFAULT (1),
        [FechaCreacion] DATETIME2 NOT NULL CONSTRAINT [DF_Pantalla_FechaCreacion] DEFAULT (GETUTCDATE()),
        
        CONSTRAINT [PK_Pantalla] PRIMARY KEY CLUSTERED ([PantallaId] ASC),
        CONSTRAINT [FK_Pantalla_PantallaPadre] FOREIGN KEY ([PantallaPadrId]) REFERENCES [dbo].[Pantalla] ([PantallaId])
    );
END
GO

-- =====================================================================
-- TABLAS INTERMEDIAS Y CON DEPENDENCIAS
-- =====================================================================

-- Tabla: UsuarioRol
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UsuarioRol]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[UsuarioRol] (
        [UsuarioRolId] INT IDENTITY(1,1) NOT NULL,
        [UsuarioId] INT NOT NULL,
        [RolId] INT NOT NULL,
        [FechaAsignacion] DATETIME2 NOT NULL CONSTRAINT [DF_UsuarioRol_FechaAsignacion] DEFAULT (GETUTCDATE()),
        
        CONSTRAINT [PK_UsuarioRol] PRIMARY KEY CLUSTERED ([UsuarioRolId] ASC),
        CONSTRAINT [FK_UsuarioRol_Usuario] FOREIGN KEY ([UsuarioId]) REFERENCES [dbo].[Usuario] ([UsuarioId]) ON DELETE CASCADE,
        CONSTRAINT [FK_UsuarioRol_Rol] FOREIGN KEY ([RolId]) REFERENCES [dbo].[Rol] ([RolId]) ON DELETE CASCADE
    );
END
GO

-- Tabla: RolPermiso
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RolPermiso]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[RolPermiso] (
        [RolPermisoId] INT IDENTITY(1,1) NOT NULL,
        [RolId] INT NOT NULL,
        [PantallaId] INT NOT NULL,
        [Lectura] BIT NOT NULL CONSTRAINT [DF_RolPermiso_Lectura] DEFAULT (1),
        [Creacion] BIT NOT NULL CONSTRAINT [DF_RolPermiso_Creacion] DEFAULT (0),
        [Edicion] BIT NOT NULL CONSTRAINT [DF_RolPermiso_Edicion] DEFAULT (0),
        [Eliminacion] BIT NOT NULL CONSTRAINT [DF_RolPermiso_Eliminacion] DEFAULT (0),
        [FechaCreacion] DATETIME2 NOT NULL CONSTRAINT [DF_RolPermiso_FechaCreacion] DEFAULT (GETUTCDATE()),
        
        CONSTRAINT [PK_RolPermiso] PRIMARY KEY CLUSTERED ([RolPermisoId] ASC),
        CONSTRAINT [FK_RolPermiso_Rol] FOREIGN KEY ([RolId]) REFERENCES [dbo].[Rol] ([RolId]) ON DELETE CASCADE,
        CONSTRAINT [FK_RolPermiso_Pantalla] FOREIGN KEY ([PantallaId]) REFERENCES [dbo].[Pantalla] ([PantallaId]) ON DELETE CASCADE
    );
END
GO

-- Tabla: AuditoriaTransaccional
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AuditoriaTransaccional]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[AuditoriaTransaccional] (
        [AuditoriaId] INT IDENTITY(1,1) NOT NULL,
        [UsuarioId] INT NOT NULL,
        [Entidad] NVARCHAR(100) NOT NULL,
        [TipoOperacion] NVARCHAR(50) NOT NULL,
        [IdRegistro] INT NULL,
        [DatosAnteriores] NVARCHAR(MAX) NULL, -- Formato JSON o string largo esperado
        [DatosNuevos] NVARCHAR(MAX) NULL,     -- Formato JSON o string largo esperado
        [FechaOperacion] DATETIME2 NOT NULL CONSTRAINT [DF_Auditoria_FechaOperacion] DEFAULT (GETUTCDATE()),
        [Descripcion] NVARCHAR(MAX) NULL,
        
        CONSTRAINT [PK_AuditoriaTransaccional] PRIMARY KEY CLUSTERED ([AuditoriaId] ASC),
        CONSTRAINT [FK_AuditoriaTransaccional_Usuario] FOREIGN KEY ([UsuarioId]) REFERENCES [dbo].[Usuario] ([UsuarioId]) ON DELETE NO ACTION
    );
END
GO

-- =====================================================================
-- ÍNDICES RECOMENDADOS (Basado en el uso típico de estas entidades)
-- =====================================================================

CREATE UNIQUE NONCLUSTERED INDEX [IX_Usuario_NombreUsuario] ON [dbo].[Usuario] ([NombreUsuario]);
CREATE UNIQUE NONCLUSTERED INDEX [IX_Usuario_Email] ON [dbo].[Usuario] ([Email]);
CREATE NONCLUSTERED INDEX [IX_UsuarioRol_UsuarioId] ON [dbo].[UsuarioRol] ([UsuarioId]);
CREATE NONCLUSTERED INDEX [IX_UsuarioRol_RolId] ON [dbo].[UsuarioRol] ([RolId]);
CREATE NONCLUSTERED INDEX [IX_RolPermiso_RolId] ON [dbo].[RolPermiso] ([RolId]);
CREATE NONCLUSTERED INDEX [IX_RolPermiso_PantallaId] ON [dbo].[RolPermiso] ([PantallaId]);
CREATE NONCLUSTERED INDEX [IX_AuditoriaTransaccional_UsuarioId] ON [dbo].[AuditoriaTransaccional] ([UsuarioId]);
GO