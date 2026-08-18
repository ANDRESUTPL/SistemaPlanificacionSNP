IF DB_ID(N'SNP_Auth') IS NULL
BEGIN
    CREATE DATABASE [SNP_Auth];
END;
GO

USE [SNP_Auth];
GO
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- 1. CREACIÓN DE TABLA USUARIO (Corregida con los nuevos campos)
IF OBJECT_ID(N'dbo.Usuario', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Usuario] (
        [UsuarioId] [int] IDENTITY(1,1) NOT NULL,
        [NombreUsuario] [nvarchar](100) NOT NULL,
        [Email] [nvarchar](256) NOT NULL,
        [PasswordHash] [nvarchar](max) NOT NULL,
        [Nombre] [nvarchar](100) NOT NULL,
        [Apellido] [nvarchar](100) NOT NULL,
        [Activo] [bit] NOT NULL,
        [FechaCreacion] [datetime2](7) NOT NULL,
        [FechaUltimoLogin] [datetime2](7) NULL,
        [RefreshToken] [nvarchar](500) NULL,
        [RefreshTokenExpiracion] [datetime2](7) NULL,
        CONSTRAINT [PK_Usuario] PRIMARY KEY CLUSTERED ([UsuarioId] ASC)
    );
END;
GO

-- Restableciendo los valores por defecto (opcional, pero recomendado para mantener la lógica anterior)
IF OBJECT_ID(N'DF_Usuario_Activo', 'D') IS NULL
    ALTER TABLE [dbo].[Usuario] ADD CONSTRAINT [DF_Usuario_Activo] DEFAULT ((1)) FOR [Activo];
GO
IF OBJECT_ID(N'DF_Usuario_FechaCreacion', 'D') IS NULL
    ALTER TABLE [dbo].[Usuario] ADD CONSTRAINT [DF_Usuario_FechaCreacion] DEFAULT (GETUTCDATE()) FOR [FechaCreacion];
GO

-- 2. CREACIÓN DE TABLA ROL
IF OBJECT_ID(N'dbo.Rol', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Rol](
        [RolId] [int] IDENTITY(1,1) NOT NULL,
        [Nombre] [nvarchar](100) NOT NULL,
        [Descripcion] [nvarchar](500) NOT NULL,
        [Activo] [bit] NOT NULL,
        [FechaCreacion] [datetime2](7) NOT NULL,
    CONSTRAINT [PK_Rol] PRIMARY KEY CLUSTERED 
    (
        [RolId] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
    ) ON [PRIMARY];

    ALTER TABLE [dbo].[Rol] ADD CONSTRAINT [DF_Rol_Activo] DEFAULT ((1)) FOR [Activo];
    ALTER TABLE [dbo].[Rol] ADD CONSTRAINT [DF_Rol_FechaCreacion] DEFAULT (GETUTCDATE()) FOR [FechaCreacion];
END;
GO

-- 3. CREACIÓN DE TABLA USUARIOROL (Actualizada referencia a [dbo].[Usuario])
IF OBJECT_ID(N'dbo.UsuarioRol', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UsuarioRol (
        UsuarioRolId INT IDENTITY(1,1) PRIMARY KEY,
        UsuarioId INT NOT NULL,
        RolId INT NOT NULL,
        FechaAsignacion DATETIME2 NOT NULL CONSTRAINT DF_Auth_UsuarioRol_FechaAsignacion DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_Auth_UsuarioRol_Usuario FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuario(UsuarioId),
        CONSTRAINT FK_Auth_UsuarioRol_Roles FOREIGN KEY (RolId) REFERENCES dbo.Rol(RolId),
        CONSTRAINT UQ_Auth_UsuarioRol UNIQUE (UsuarioId, RolId)
    );
END;
GO

-- 4. CREACIÓN DE TABLA AUDITORIATRANSACCIONAL
IF OBJECT_ID(N'dbo.AuditoriaTransaccional', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AuditoriaTransaccional](
        [AuditoriaId] [int] IDENTITY(1,1) NOT NULL,
        [UsuarioId] [int] NOT NULL,
        [Entidad] [nvarchar](100) NOT NULL,
        [TipoOperacion] [nvarchar](50) NOT NULL,
        [IdRegistro] [int] NULL,
        [DatosAnteriores] [nvarchar](max) NULL,
        [DatosNuevos] [nvarchar](max) NULL,
        [FechaOperacion] [datetime2](7) NOT NULL,
        [Descripcion] [nvarchar](max) NULL,
    CONSTRAINT [PK_AuditoriaTransaccional] PRIMARY KEY CLUSTERED 
    (
        [AuditoriaId] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
    ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO

IF OBJECT_ID(N'DF_Auditoria_FechaOperacion', 'D') IS NULL
    ALTER TABLE [dbo].[AuditoriaTransaccional] ADD CONSTRAINT [DF_Auditoria_FechaOperacion] DEFAULT (GETUTCDATE()) FOR [FechaOperacion]
GO

-- Actualizada referencia para que coincida con [dbo].[Usuario]
IF OBJECT_ID(N'FK_AuditoriaTransaccional_Usuario', 'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[AuditoriaTransaccional] WITH CHECK ADD CONSTRAINT [FK_AuditoriaTransaccional_Usuario] FOREIGN KEY([UsuarioId])
    REFERENCES [dbo].[Usuario] ([UsuarioId]);
    
    ALTER TABLE [dbo].[AuditoriaTransaccional] CHECK CONSTRAINT [FK_AuditoriaTransaccional_Usuario];
END
GO

-- 5. CREACIÓN DE TABLA PANTALLA
IF OBJECT_ID(N'dbo.Pantalla', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Pantalla](
        [PantallaId] [int] IDENTITY(1,1) NOT NULL,
        [Nombre] [nvarchar](100) NOT NULL,
        [Ruta] [nvarchar](256) NOT NULL,
        [Icono] [nvarchar](100) NOT NULL,
        [PantallaPadrId] [int] NULL,
        [Orden] [int] NOT NULL,
        [Activo] [bit] NOT NULL,
        [FechaCreacion] [datetime2](7) NOT NULL,
    CONSTRAINT [PK_Pantalla] PRIMARY KEY CLUSTERED 
    (
        [PantallaId] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
    ) ON [PRIMARY]
END
GO

IF OBJECT_ID(N'DF_Pantalla_Activo', 'D') IS NULL
    ALTER TABLE [dbo].[Pantalla] ADD CONSTRAINT [DF_Pantalla_Activo] DEFAULT ((1)) FOR [Activo]
GO
IF OBJECT_ID(N'DF_Pantalla_FechaCreacion', 'D') IS NULL
    ALTER TABLE [dbo].[Pantalla] ADD CONSTRAINT [DF_Pantalla_FechaCreacion] DEFAULT (GETUTCDATE()) FOR [FechaCreacion]
GO

IF OBJECT_ID(N'FK_Pantalla_PantallaPadre', 'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[Pantalla] WITH CHECK ADD CONSTRAINT [FK_Pantalla_PantallaPadre] FOREIGN KEY([PantallaPadrId])
    REFERENCES [dbo].[Pantalla] ([PantallaId]);
    
    ALTER TABLE [dbo].[Pantalla] CHECK CONSTRAINT [FK_Pantalla_PantallaPadre];
END
GO

-- 6. CREACIÓN DE TABLA ROLPERMISO
IF OBJECT_ID(N'dbo.RolPermiso', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[RolPermiso](
        [RolPermisoId] [int] IDENTITY(1,1) NOT NULL,
        [RolId] [int] NOT NULL,
        [PantallaId] [int] NOT NULL,
        [Lectura] [bit] NOT NULL,
        [Creacion] [bit] NOT NULL,
        [Edicion] [bit] NOT NULL,
        [Eliminacion] [bit] NOT NULL,
        [FechaCreacion] [datetime2](7) NOT NULL,
    CONSTRAINT [PK_RolPermiso] PRIMARY KEY CLUSTERED 
    (
        [RolPermisoId] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
    ) ON [PRIMARY]
END
GO

IF OBJECT_ID(N'DF_RolPermiso_Lectura', 'D') IS NULL
    ALTER TABLE [dbo].[RolPermiso] ADD CONSTRAINT [DF_RolPermiso_Lectura] DEFAULT ((1)) FOR [Lectura]
GO
IF OBJECT_ID(N'DF_RolPermiso_Creacion', 'D') IS NULL
    ALTER TABLE [dbo].[RolPermiso] ADD CONSTRAINT [DF_RolPermiso_Creacion] DEFAULT ((0)) FOR [Creacion]
GO
IF OBJECT_ID(N'DF_RolPermiso_Edicion', 'D') IS NULL
    ALTER TABLE [dbo].[RolPermiso] ADD CONSTRAINT [DF_RolPermiso_Edicion] DEFAULT ((0)) FOR [Edicion]
GO
IF OBJECT_ID(N'DF_RolPermiso_Eliminacion', 'D') IS NULL
    ALTER TABLE [dbo].[RolPermiso] ADD CONSTRAINT [DF_RolPermiso_Eliminacion] DEFAULT ((0)) FOR [Eliminacion]
GO
IF OBJECT_ID(N'DF_RolPermiso_FechaCreacion', 'D') IS NULL
    ALTER TABLE [dbo].[RolPermiso] ADD CONSTRAINT [DF_RolPermiso_FechaCreacion] DEFAULT (GETUTCDATE()) FOR [FechaCreacion]
GO

IF OBJECT_ID(N'FK_RolPermiso_Pantalla', 'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[RolPermiso] WITH CHECK ADD CONSTRAINT [FK_RolPermiso_Pantalla] FOREIGN KEY([PantallaId])
    REFERENCES [dbo].[Pantalla] ([PantallaId]) ON DELETE CASCADE;
    
    ALTER TABLE [dbo].[RolPermiso] CHECK CONSTRAINT [FK_RolPermiso_Pantalla];
END
GO

IF OBJECT_ID(N'FK_RolPermiso_Rol', 'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[RolPermiso] WITH CHECK ADD CONSTRAINT [FK_RolPermiso_Rol] FOREIGN KEY([RolId])
    REFERENCES [dbo].[Rol] ([RolId]) ON DELETE CASCADE;
    
    ALTER TABLE [dbo].[RolPermiso] CHECK CONSTRAINT [FK_RolPermiso_Rol];
END
GO