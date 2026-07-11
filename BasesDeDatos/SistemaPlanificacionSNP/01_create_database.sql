IF DB_ID(N'SistemaPlanificacionSNP') IS NULL
BEGIN
    CREATE DATABASE [SistemaPlanificacionSNP];
END;
GO

USE [SistemaPlanificacionSNP];
GO

IF OBJECT_ID(N'dbo.Usuarios', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Usuarios (
        UsuarioId INT IDENTITY(1,1) PRIMARY KEY,
        Nombres NVARCHAR(100) NOT NULL,
        Apellidos NVARCHAR(100) NOT NULL,
        Email NVARCHAR(150) NOT NULL UNIQUE,
        Activo BIT NOT NULL CONSTRAINT DF_Usuarios_Activo DEFAULT (1),
        FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_Usuarios_FechaCreacion DEFAULT (SYSUTCDATETIME())
    );
END;
GO

IF OBJECT_ID(N'dbo.Roles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Roles (
        RolId INT IDENTITY(1,1) PRIMARY KEY,
        Nombre NVARCHAR(60) NOT NULL UNIQUE,
        Descripcion NVARCHAR(200) NULL,
        Activo BIT NOT NULL CONSTRAINT DF_Roles_Activo DEFAULT (1)
    );
END;
GO

IF OBJECT_ID(N'dbo.Catalogos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Catalogos (
        CatalogoId INT IDENTITY(1,1) PRIMARY KEY,
        Codigo NVARCHAR(50) NOT NULL UNIQUE,
        Nombre NVARCHAR(120) NOT NULL,
        Descripcion NVARCHAR(250) NULL,
        Activo BIT NOT NULL CONSTRAINT DF_Catalogos_Activo DEFAULT (1)
    );
END;
GO

IF OBJECT_ID(N'dbo.ItemsCatalogo', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ItemsCatalogo (
        ItemCatalogoId INT IDENTITY(1,1) PRIMARY KEY,
        CatalogoId INT NOT NULL,
        Codigo NVARCHAR(50) NOT NULL,
        Nombre NVARCHAR(120) NOT NULL,
        Valor NVARCHAR(200) NULL,
        Orden INT NOT NULL CONSTRAINT DF_ItemsCatalogo_Orden DEFAULT (0),
        Activo BIT NOT NULL CONSTRAINT DF_ItemsCatalogo_Activo DEFAULT (1),
        CONSTRAINT FK_ItemsCatalogo_Catalogos FOREIGN KEY (CatalogoId) REFERENCES dbo.Catalogos(CatalogoId),
        CONSTRAINT UQ_ItemsCatalogo_CatalogoCodigo UNIQUE (CatalogoId, Codigo)
    );
END;
GO