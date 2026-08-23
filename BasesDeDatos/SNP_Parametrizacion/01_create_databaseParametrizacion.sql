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

/****** Object:  Table [dbo].[EntidadesPublicas]    Script Date: 13/8/2026 20:21:14 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[EntidadesPublicas](
	[EntidadPublicaId] [int] IDENTITY(1,1) NOT NULL,
	[Codigo] [nvarchar](50) NOT NULL,
	[Nombre] [nvarchar](200) NOT NULL,
	[Sigla] [nvarchar](50) NOT NULL,
	[Tipo] [nvarchar](100) NOT NULL,
	[NivelGobierno] [nvarchar](100) NOT NULL,
	[Mision] [nvarchar](max) NULL,
	[PeriodoPlanificacionId] [int] NOT NULL,
	[Activo] [bit] NOT NULL,
	[FechaCreacion] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[EntidadPublicaId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[Codigo] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[EntidadesPublicas] ADD  CONSTRAINT [DF_Param_Entidades_Activo]  DEFAULT ((1)) FOR [Activo]
GO

ALTER TABLE [dbo].[EntidadesPublicas] ADD  CONSTRAINT [DF_Param_Entidades_FechaCreacion]  DEFAULT (sysutcdatetime()) FOR [FechaCreacion]
GO

ALTER TABLE [dbo].[EntidadesPublicas]  WITH CHECK ADD  CONSTRAINT [FK_Param_Entidades_Periodos] FOREIGN KEY([PeriodoPlanificacionId])
REFERENCES [dbo].[PeriodosPlanificacion] ([PeriodoPlanificacionId])
GO

ALTER TABLE [dbo].[EntidadesPublicas] CHECK CONSTRAINT [FK_Param_Entidades_Periodos]
GO


CREATE TABLE [dbo].[PeriodosPlanificacion](
	[PeriodoPlanificacionId] [int] IDENTITY(1,1) NOT NULL,
	[Codigo] [nvarchar](50) NOT NULL,
	[Nombre] [nvarchar](150) NOT NULL,
	[FechaInicio] [datetime2](7) NOT NULL,
	[FechaFin] [datetime2](7) NOT NULL,
	[Activo] [bit] NOT NULL,
	[FechaCreacion] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[PeriodoPlanificacionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[Codigo] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[PeriodosPlanificacion] ADD  CONSTRAINT [DF_Param_Periodos_Activo]  DEFAULT ((1)) FOR [Activo]
GO

ALTER TABLE [dbo].[PeriodosPlanificacion] ADD  CONSTRAINT [DF_Param_Periodos_FechaCreacion]  DEFAULT (sysutcdatetime()) FOR [FechaCreacion]
GO


