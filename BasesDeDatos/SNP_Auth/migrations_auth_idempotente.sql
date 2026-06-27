IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625040703_Auth_InitialBaseline'
)
BEGIN
    CREATE TABLE [Catalogos] (
        [CatalogoId] int NOT NULL IDENTITY,
        [Codigo] nvarchar(50) NOT NULL,
        [Nombre] nvarchar(100) NOT NULL,
        [Descripcion] nvarchar(255) NOT NULL,
        [Activo] bit NOT NULL,
        [FechaCreacion] datetime2 NOT NULL,
        CONSTRAINT [PK_Catalogos] PRIMARY KEY ([CatalogoId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625040703_Auth_InitialBaseline'
)
BEGIN
    CREATE TABLE [ObjetivosDesarrolloSostenible] (
        [OdsId] int NOT NULL IDENTITY,
        [Codigo] nvarchar(10) NOT NULL,
        [Nombre] nvarchar(200) NOT NULL,
        [Descripcion] nvarchar(max) NOT NULL,
        [Activo] bit NOT NULL,
        [FechaCreacion] datetime2 NOT NULL,
        CONSTRAINT [PK_ObjetivosDesarrolloSostenible] PRIMARY KEY ([OdsId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625040703_Auth_InitialBaseline'
)
BEGIN
    CREATE TABLE [dbo].[Pantalla] (
        [PantallaId] int NOT NULL IDENTITY,
        [Nombre] nvarchar(100) NOT NULL,
        [Ruta] nvarchar(256) NOT NULL,
        [Icono] nvarchar(100) NOT NULL,
        [PantallaPadrId] int NULL,
        [Orden] int NOT NULL,
        [Activo] bit NOT NULL,
        [FechaCreacion] datetime2 NOT NULL,
        CONSTRAINT [PK_Pantalla] PRIMARY KEY ([PantallaId]),
        CONSTRAINT [FK_Pantalla_Pantalla_PantallaPadrId] FOREIGN KEY ([PantallaPadrId]) REFERENCES [dbo].[Pantalla] ([PantallaId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625040703_Auth_InitialBaseline'
)
BEGIN
    CREATE TABLE [PeriodosPlanificacion] (
        [PeriodoPlanificacionId] int NOT NULL IDENTITY,
        [Codigo] nvarchar(20) NOT NULL,
        [Nombre] nvarchar(100) NOT NULL,
        [FechaInicio] datetime2 NOT NULL,
        [FechaFin] datetime2 NOT NULL,
        [Activo] bit NOT NULL,
        [FechaCreacion] datetime2 NOT NULL,
        CONSTRAINT [PK_PeriodosPlanificacion] PRIMARY KEY ([PeriodoPlanificacionId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625040703_Auth_InitialBaseline'
)
BEGIN
    CREATE TABLE [dbo].[Rol] (
        [RolId] int NOT NULL IDENTITY,
        [Nombre] nvarchar(50) NOT NULL,
        [Descripcion] nvarchar(500) NOT NULL,
        [Activo] bit NOT NULL,
        [FechaCreacion] datetime2 NOT NULL,
        CONSTRAINT [PK_Rol] PRIMARY KEY ([RolId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625040703_Auth_InitialBaseline'
)
BEGIN
    CREATE TABLE [dbo].[Usuario] (
        [UsuarioId] int NOT NULL IDENTITY,
        [NombreUsuario] nvarchar(100) NOT NULL,
        [Email] nvarchar(256) NOT NULL,
        [PasswordHash] nvarchar(max) NOT NULL,
        [Nombre] nvarchar(100) NOT NULL,
        [Apellido] nvarchar(100) NOT NULL,
        [Activo] bit NOT NULL,
        [FechaCreacion] datetime2 NOT NULL,
        [FechaUltimoLogin] datetime2 NULL,
        [RefreshToken] nvarchar(500) NULL,
        [RefreshTokenExpiracion] datetime2 NULL,
        CONSTRAINT [PK_Usuario] PRIMARY KEY ([UsuarioId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625040703_Auth_InitialBaseline'
)
BEGIN
    CREATE TABLE [ItemsCatalogo] (
        [ItemCatalogoId] int NOT NULL IDENTITY,
        [CatalogoId] int NOT NULL,
        [Codigo] nvarchar(50) NOT NULL,
        [Nombre] nvarchar(100) NOT NULL,
        [Descripcion] nvarchar(255) NULL,
        [Orden] int NOT NULL,
        [Activo] bit NOT NULL,
        [FechaCreacion] datetime2 NOT NULL,
        CONSTRAINT [PK_ItemsCatalogo] PRIMARY KEY ([ItemCatalogoId]),
        CONSTRAINT [FK_ItemsCatalogo_Catalogos_CatalogoId] FOREIGN KEY ([CatalogoId]) REFERENCES [Catalogos] ([CatalogoId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625040703_Auth_InitialBaseline'
)
BEGIN
    CREATE TABLE [PlanesNacionalesDesarrollo] (
        [PndId] int NOT NULL IDENTITY,
        [Codigo] nvarchar(20) NOT NULL,
        [Nombre] nvarchar(200) NOT NULL,
        [Descripcion] nvarchar(max) NOT NULL,
        [OdsId] int NOT NULL,
        [Activo] bit NOT NULL,
        [FechaCreacion] datetime2 NOT NULL,
        CONSTRAINT [PK_PlanesNacionalesDesarrollo] PRIMARY KEY ([PndId]),
        CONSTRAINT [FK_PlanesNacionalesDesarrollo_ObjetivosDesarrolloSostenible_OdsId] FOREIGN KEY ([OdsId]) REFERENCES [ObjetivosDesarrolloSostenible] ([OdsId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625040703_Auth_InitialBaseline'
)
BEGIN
    CREATE TABLE [EntidadesPublicas] (
        [EntidadPublicaId] int NOT NULL IDENTITY,
        [Codigo] nvarchar(20) NOT NULL,
        [Nombre] nvarchar(200) NOT NULL,
        [Sigla] nvarchar(20) NOT NULL,
        [Mision] nvarchar(max) NOT NULL,
        [PeriodoPlanificacionId] int NOT NULL,
        [Activo] bit NOT NULL,
        [FechaCreacion] datetime2 NOT NULL,
        CONSTRAINT [PK_EntidadesPublicas] PRIMARY KEY ([EntidadPublicaId]),
        CONSTRAINT [FK_EntidadesPublicas_PeriodosPlanificacion_PeriodoPlanificacionId] FOREIGN KEY ([PeriodoPlanificacionId]) REFERENCES [PeriodosPlanificacion] ([PeriodoPlanificacionId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625040703_Auth_InitialBaseline'
)
BEGIN
    CREATE TABLE [dbo].[RolPermiso] (
        [RolPermisoId] int NOT NULL IDENTITY,
        [RolId] int NOT NULL,
        [PantallaId] int NOT NULL,
        [Lectura] bit NOT NULL,
        [Creacion] bit NOT NULL,
        [Edicion] bit NOT NULL,
        [Eliminacion] bit NOT NULL,
        [FechaCreacion] datetime2 NOT NULL,
        CONSTRAINT [PK_RolPermiso] PRIMARY KEY ([RolPermisoId]),
        CONSTRAINT [FK_RolPermiso_Pantalla_PantallaId] FOREIGN KEY ([PantallaId]) REFERENCES [dbo].[Pantalla] ([PantallaId]) ON DELETE CASCADE,
        CONSTRAINT [FK_RolPermiso_Rol_RolId] FOREIGN KEY ([RolId]) REFERENCES [dbo].[Rol] ([RolId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625040703_Auth_InitialBaseline'
)
BEGIN
    CREATE TABLE [dbo].[AuditoriaTransaccional] (
        [AuditoriaId] int NOT NULL IDENTITY,
        [UsuarioId] int NOT NULL,
        [Entidad] nvarchar(100) NOT NULL,
        [TipoOperacion] nvarchar(50) NOT NULL,
        [IdRegistro] int NULL,
        [DatosAnteriores] nvarchar(max) NULL,
        [DatosNuevos] nvarchar(max) NULL,
        [FechaOperacion] datetime2 NOT NULL,
        [Descripcion] nvarchar(max) NULL,
        CONSTRAINT [PK_AuditoriaTransaccional] PRIMARY KEY ([AuditoriaId]),
        CONSTRAINT [FK_AuditoriaTransaccional_Usuario_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [dbo].[Usuario] ([UsuarioId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625040703_Auth_InitialBaseline'
)
BEGIN
    CREATE TABLE [dbo].[UsuarioRol] (
        [UsuarioRolId] int NOT NULL IDENTITY,
        [UsuarioId] int NOT NULL,
        [RolId] int NOT NULL,
        [FechaAsignacion] datetime2 NOT NULL,
        CONSTRAINT [PK_UsuarioRol] PRIMARY KEY ([UsuarioRolId]),
        CONSTRAINT [FK_UsuarioRol_Rol_RolId] FOREIGN KEY ([RolId]) REFERENCES [dbo].[Rol] ([RolId]) ON DELETE CASCADE,
        CONSTRAINT [FK_UsuarioRol_Usuario_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [dbo].[Usuario] ([UsuarioId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625040703_Auth_InitialBaseline'
)
BEGIN
    CREATE TABLE [PlanesEstrategicos] (
        [PeiId] int NOT NULL IDENTITY,
        [Codigo] nvarchar(50) NOT NULL,
        [Nombre] nvarchar(200) NOT NULL,
        [Descripcion] nvarchar(max) NOT NULL,
        [EntidadPublicaId] int NOT NULL,
        [FechaInicio] datetime2 NOT NULL,
        [FechaFin] datetime2 NOT NULL,
        [Estado] nvarchar(20) NOT NULL,
        [Activo] bit NOT NULL,
        [FechaCreacion] datetime2 NOT NULL,
        CONSTRAINT [PK_PlanesEstrategicos] PRIMARY KEY ([PeiId]),
        CONSTRAINT [FK_PlanesEstrategicos_EntidadesPublicas_EntidadPublicaId] FOREIGN KEY ([EntidadPublicaId]) REFERENCES [EntidadesPublicas] ([EntidadPublicaId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625040703_Auth_InitialBaseline'
)
BEGIN
    CREATE TABLE [ObjetivosEstrategicos] (
        [OeiId] int NOT NULL IDENTITY,
        [Codigo] nvarchar(50) NOT NULL,
        [Nombre] nvarchar(200) NOT NULL,
        [Descripcion] nvarchar(max) NOT NULL,
        [PeiId] int NOT NULL,
        [PndId] int NULL,
        [Orden] int NOT NULL,
        [Activo] bit NOT NULL,
        [FechaCreacion] datetime2 NOT NULL,
        [PlanNacionalPndId] int NULL,
        CONSTRAINT [PK_ObjetivosEstrategicos] PRIMARY KEY ([OeiId]),
        CONSTRAINT [FK_ObjetivosEstrategicos_PlanesEstrategicos_PeiId] FOREIGN KEY ([PeiId]) REFERENCES [PlanesEstrategicos] ([PeiId]) ON DELETE CASCADE,
        CONSTRAINT [FK_ObjetivosEstrategicos_PlanesNacionalesDesarrollo_PlanNacionalPndId] FOREIGN KEY ([PlanNacionalPndId]) REFERENCES [PlanesNacionalesDesarrollo] ([PndId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625040703_Auth_InitialBaseline'
)
BEGIN
    CREATE TABLE [Revisiones] (
        [RevisionId] int NOT NULL IDENTITY,
        [PeiId] int NOT NULL,
        [Estado] nvarchar(20) NOT NULL,
        [Comentarios] nvarchar(max) NULL,
        [UsuarioRevisor] int NULL,
        [FechaRevision] datetime2 NOT NULL,
        [Activo] bit NOT NULL,
        [RevisorUsuarioId] int NULL,
        CONSTRAINT [PK_Revisiones] PRIMARY KEY ([RevisionId]),
        CONSTRAINT [FK_Revisiones_PlanesEstrategicos_PeiId] FOREIGN KEY ([PeiId]) REFERENCES [PlanesEstrategicos] ([PeiId]) ON DELETE CASCADE,
        CONSTRAINT [FK_Revisiones_Usuario_RevisorUsuarioId] FOREIGN KEY ([RevisorUsuarioId]) REFERENCES [dbo].[Usuario] ([UsuarioId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625040703_Auth_InitialBaseline'
)
BEGIN
    CREATE TABLE [ProgramasPresupuestarios] (
        [ProgramaId] int NOT NULL IDENTITY,
        [Codigo] nvarchar(50) NOT NULL,
        [Nombre] nvarchar(200) NOT NULL,
        [Descripcion] nvarchar(max) NOT NULL,
        [OeiId] int NOT NULL,
        [PresupuestoAsignado] decimal(18,2) NOT NULL,
        [Orden] int NOT NULL,
        [Activo] bit NOT NULL,
        [FechaCreacion] datetime2 NOT NULL,
        CONSTRAINT [PK_ProgramasPresupuestarios] PRIMARY KEY ([ProgramaId]),
        CONSTRAINT [FK_ProgramasPresupuestarios_ObjetivosEstrategicos_OeiId] FOREIGN KEY ([OeiId]) REFERENCES [ObjetivosEstrategicos] ([OeiId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625040703_Auth_InitialBaseline'
)
BEGIN
    CREATE TABLE [MatricesIndicadores] (
        [MatrizIndicadorId] int NOT NULL IDENTITY,
        [Codigo] nvarchar(50) NOT NULL,
        [Nombre] nvarchar(200) NOT NULL,
        [Descripcion] nvarchar(max) NOT NULL,
        [ProgramaId] int NOT NULL,
        [TipoIndicador] nvarchar(50) NOT NULL,
        [Unidad] nvarchar(50) NOT NULL,
        [ValorBase] decimal(18,4) NOT NULL,
        [ValorMeta] decimal(18,4) NOT NULL,
        [Orden] int NOT NULL,
        [Activo] bit NOT NULL,
        [FechaCreacion] datetime2 NOT NULL,
        CONSTRAINT [PK_MatricesIndicadores] PRIMARY KEY ([MatrizIndicadorId]),
        CONSTRAINT [FK_MatricesIndicadores_ProgramasPresupuestarios_ProgramaId] FOREIGN KEY ([ProgramaId]) REFERENCES [ProgramasPresupuestarios] ([ProgramaId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625040703_Auth_InitialBaseline'
)
BEGIN
    CREATE TABLE [ProyectosInversion] (
        [ProyectoId] int NOT NULL IDENTITY,
        [Codigo] nvarchar(50) NOT NULL,
        [Nombre] nvarchar(200) NOT NULL,
        [Descripcion] nvarchar(max) NOT NULL,
        [ProgramaId] int NOT NULL,
        [CostoTotal] decimal(18,2) NOT NULL,
        [FechaInicio] datetime2 NOT NULL,
        [FechaFin] datetime2 NOT NULL,
        [Estado] nvarchar(20) NOT NULL,
        [Activo] bit NOT NULL,
        [FechaCreacion] datetime2 NOT NULL,
        CONSTRAINT [PK_ProyectosInversion] PRIMARY KEY ([ProyectoId]),
        CONSTRAINT [FK_ProyectosInversion_ProgramasPresupuestarios_ProgramaId] FOREIGN KEY ([ProgramaId]) REFERENCES [ProgramasPresupuestarios] ([ProgramaId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625040703_Auth_InitialBaseline'
)
BEGIN
    CREATE TABLE [MetasTerritorial] (
        [MetaTerritorialId] int NOT NULL IDENTITY,
        [MatrizIndicadorId] int NOT NULL,
        [Territorio] nvarchar(100) NOT NULL,
        [MetaFisica] decimal(18,4) NOT NULL,
        [MetaFinanciera] decimal(18,2) NOT NULL,
        [Orden] int NOT NULL,
        [Activo] bit NOT NULL,
        [FechaCreacion] datetime2 NOT NULL,
        CONSTRAINT [PK_MetasTerritorial] PRIMARY KEY ([MetaTerritorialId]),
        CONSTRAINT [FK_MetasTerritorial_MatricesIndicadores_MatrizIndicadorId] FOREIGN KEY ([MatrizIndicadorId]) REFERENCES [MatricesIndicadores] ([MatrizIndicadorId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625040703_Auth_InitialBaseline'
)
BEGIN
    CREATE INDEX [IX_AuditoriaTransaccional_UsuarioId] ON [dbo].[AuditoriaTransaccional] ([UsuarioId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625040703_Auth_InitialBaseline'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Catalogos_Codigo] ON [Catalogos] ([Codigo]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625040703_Auth_InitialBaseline'
)
BEGIN
    CREATE UNIQUE INDEX [IX_EntidadesPublicas_Codigo] ON [EntidadesPublicas] ([Codigo]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625040703_Auth_InitialBaseline'
)
BEGIN
    CREATE INDEX [IX_EntidadesPublicas_PeriodoPlanificacionId] ON [EntidadesPublicas] ([PeriodoPlanificacionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625040703_Auth_InitialBaseline'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ItemsCatalogo_CatalogoId_Codigo] ON [ItemsCatalogo] ([CatalogoId], [Codigo]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625040703_Auth_InitialBaseline'
)
BEGIN
    CREATE INDEX [IX_MatricesIndicadores_ProgramaId] ON [MatricesIndicadores] ([ProgramaId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625040703_Auth_InitialBaseline'
)
BEGIN
    CREATE INDEX [IX_MetasTerritorial_MatrizIndicadorId] ON [MetasTerritorial] ([MatrizIndicadorId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625040703_Auth_InitialBaseline'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ObjetivosDesarrolloSostenible_Codigo] ON [ObjetivosDesarrolloSostenible] ([Codigo]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625040703_Auth_InitialBaseline'
)
BEGIN
    CREATE INDEX [IX_ObjetivosEstrategicos_PeiId] ON [ObjetivosEstrategicos] ([PeiId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625040703_Auth_InitialBaseline'
)
BEGIN
    CREATE INDEX [IX_ObjetivosEstrategicos_PlanNacionalPndId] ON [ObjetivosEstrategicos] ([PlanNacionalPndId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625040703_Auth_InitialBaseline'
)
BEGIN
    CREATE INDEX [IX_Pantalla_PantallaPadrId] ON [dbo].[Pantalla] ([PantallaPadrId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625040703_Auth_InitialBaseline'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PeriodosPlanificacion_Codigo] ON [PeriodosPlanificacion] ([Codigo]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625040703_Auth_InitialBaseline'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PlanesEstrategicos_Codigo] ON [PlanesEstrategicos] ([Codigo]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625040703_Auth_InitialBaseline'
)
BEGIN
    CREATE INDEX [IX_PlanesEstrategicos_EntidadPublicaId] ON [PlanesEstrategicos] ([EntidadPublicaId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625040703_Auth_InitialBaseline'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PlanesNacionalesDesarrollo_Codigo] ON [PlanesNacionalesDesarrollo] ([Codigo]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625040703_Auth_InitialBaseline'
)
BEGIN
    CREATE INDEX [IX_PlanesNacionalesDesarrollo_OdsId] ON [PlanesNacionalesDesarrollo] ([OdsId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625040703_Auth_InitialBaseline'
)
BEGIN
    CREATE INDEX [IX_ProgramasPresupuestarios_OeiId] ON [ProgramasPresupuestarios] ([OeiId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625040703_Auth_InitialBaseline'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProyectosInversion_Codigo] ON [ProyectosInversion] ([Codigo]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625040703_Auth_InitialBaseline'
)
BEGIN
    CREATE INDEX [IX_ProyectosInversion_ProgramaId] ON [ProyectosInversion] ([ProgramaId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625040703_Auth_InitialBaseline'
)
BEGIN
    CREATE INDEX [IX_Revisiones_PeiId] ON [Revisiones] ([PeiId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625040703_Auth_InitialBaseline'
)
BEGIN
    CREATE INDEX [IX_Revisiones_RevisorUsuarioId] ON [Revisiones] ([RevisorUsuarioId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625040703_Auth_InitialBaseline'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Rol_Nombre] ON [dbo].[Rol] ([Nombre]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625040703_Auth_InitialBaseline'
)
BEGIN
    CREATE INDEX [IX_RolPermiso_PantallaId] ON [dbo].[RolPermiso] ([PantallaId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625040703_Auth_InitialBaseline'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RolPermiso_RolId_PantallaId] ON [dbo].[RolPermiso] ([RolId], [PantallaId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625040703_Auth_InitialBaseline'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Usuario_Email] ON [dbo].[Usuario] ([Email]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625040703_Auth_InitialBaseline'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Usuario_NombreUsuario] ON [dbo].[Usuario] ([NombreUsuario]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625040703_Auth_InitialBaseline'
)
BEGIN
    CREATE INDEX [IX_UsuarioRol_RolId] ON [dbo].[UsuarioRol] ([RolId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625040703_Auth_InitialBaseline'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UsuarioRol_UsuarioId_RolId] ON [dbo].[UsuarioRol] ([UsuarioId], [RolId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625040703_Auth_InitialBaseline'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260625040703_Auth_InitialBaseline', N'8.0.0');
END;
GO

COMMIT;
GO

