IF DB_ID(N'SNP_Auth') IS NULL
BEGIN
    CREATE DATABASE [SNP_Auth];
END;
GO

USE [SNP_Auth];
GO

IF OBJECT_ID(N'dbo.Usuarios', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Usuarios (
        UsuarioId INT IDENTITY(1,1) PRIMARY KEY,
        Usuario NVARCHAR(60) NOT NULL UNIQUE,
        Email NVARCHAR(150) NOT NULL UNIQUE,
        PasswordHash NVARCHAR(300) NOT NULL,
        Activo BIT NOT NULL CONSTRAINT DF_Auth_Usuarios_Activo DEFAULT (1),
        FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_Auth_Usuarios_FechaCreacion DEFAULT (SYSUTCDATETIME())
    );
END;
GO

IF OBJECT_ID(N'dbo.Roles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Roles (
        RolId INT IDENTITY(1,1) PRIMARY KEY,
        Nombre NVARCHAR(60) NOT NULL UNIQUE,
        Activo BIT NOT NULL CONSTRAINT DF_Auth_Roles_Activo DEFAULT (1)
    );
END;
GO

IF OBJECT_ID(N'dbo.UsuarioRoles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UsuarioRoles (
        UsuarioRolId INT IDENTITY(1,1) PRIMARY KEY,
        UsuarioId INT NOT NULL,
        RolId INT NOT NULL,
        FechaAsignacion DATETIME2 NOT NULL CONSTRAINT DF_Auth_UsuarioRoles_FechaAsignacion DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_Auth_UsuarioRoles_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios(UsuarioId),
        CONSTRAINT FK_Auth_UsuarioRoles_Roles FOREIGN KEY (RolId) REFERENCES dbo.Roles(RolId),
        CONSTRAINT UQ_Auth_UsuarioRoles UNIQUE (UsuarioId, RolId)
    );
END;
GO