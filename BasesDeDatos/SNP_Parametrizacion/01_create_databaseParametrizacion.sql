IF DB_ID(N'SNP_Parametrizacion') IS NULL
BEGIN
    CREATE DATABASE [SNP_Parametrizacion];
END;
GO

USE [SNP_Parametrizacion];
GO

IF OBJECT_ID(N'dbo.Catalogos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Catalogos (
        CatalogoId INT IDENTITY(1,1) PRIMARY KEY,
        Codigo NVARCHAR(50) NOT NULL UNIQUE,
        Descripcion NVARCHAR(100) NOT NULL,
        Nombre NVARCHAR(120) NOT NULL,
		FechaCreacion DATETIME NOT NULL,
        Activo BIT NOT NULL CONSTRAINT DF_Param_Catalogos_Activo DEFAULT (1)
    );
END;
GO

IF OBJECT_ID(N'dbo.ItemsCatalogo', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ItemsCatalogo (
        ItemCatalogoId INT IDENTITY(1,1) PRIMARY KEY,
        CatalogoId INT NOT NULL,
        Codigo NVARCHAR(50) NOT NULL,
        Descripcion NVARCHAR(100) NOT NULL,
        Nombre NVARCHAR(120) NOT NULL,
        Valor NVARCHAR(200) NULL,
		FechaCreacion DATETIME NOT NULL,
        Orden INT NOT NULL CONSTRAINT DF_Param_ItemsCatalogo_Orden DEFAULT (0),
        Activo BIT NOT NULL CONSTRAINT DF_Param_ItemsCatalogo_Activo DEFAULT (1),
        CONSTRAINT FK_Param_ItemsCatalogo_Catalogos FOREIGN KEY (CatalogoId) REFERENCES dbo.Catalogos(CatalogoId),
        CONSTRAINT UQ_Param_ItemsCatalogo_CatalogoCodigo UNIQUE (CatalogoId, Codigo)
    );
END;
GO