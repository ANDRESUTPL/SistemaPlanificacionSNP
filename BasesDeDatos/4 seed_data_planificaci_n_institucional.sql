-- =====================================================================
-- SCRIPT DE SEED DATA REALISTA
-- Base de Datos: SNP_PlanificacionInstitucional
-- =====================================================================

USE [SNP_PlanificacionInstitucional];
GO

PRINT 'Limpiando base de datos SNP_PlanificacionInstitucional...';
-- Limpiar en orden inverso de dependencias
DELETE FROM [dbo].[ProyectosInversion];
DELETE FROM [dbo].[PlanesEstrategicos];
DELETE FROM [dbo].[ObjetivosEstrategico];
DELETE FROM [dbo].[PlanesNacionalesDesarrollo];
DELETE FROM [dbo].[PlanNacionalDesarrollo];
DELETE FROM [dbo].[ObjetivoDesarrolloSostenible];
GO

PRINT 'Insertando Datos de Macro Planificación (Vistas/Espejos)...';

SET IDENTITY_INSERT [dbo].[ObjetivoDesarrolloSostenible] ON;
INSERT INTO [dbo].[ObjetivoDesarrolloSostenible] ([OdsId], [Codigo], [Nombre], [Descripcion], [Activo], [FechaCreacion])
VALUES 
(1, 'ODS-1', 'Fin de la Pobreza', 'Poner fin a la pobreza en todas sus formas', 1, SYSUTCDATETIME()),
(2, 'ODS-3', 'Salud y Bienestar', 'Garantizar una vida sana y promover el bienestar', 1, SYSUTCDATETIME()),
(3, 'ODS-4', 'Educación de Calidad', 'Garantizar una educación inclusiva y equitativa de calidad', 1, SYSUTCDATETIME());
SET IDENTITY_INSERT [dbo].[ObjetivoDesarrolloSostenible] OFF;

SET IDENTITY_INSERT [dbo].[PlanNacionalDesarrollo] ON;
INSERT INTO [dbo].[PlanNacionalDesarrollo] ([PndId], [Codigo], [Nombre], [Descripcion], [OdsId], [Activo], [FechaCreacion])
VALUES 
(1, 'PND-01', 'Eje Social PND 2026', 'Alineado al bienestar social y salud', 2, 1, SYSUTCDATETIME());
SET IDENTITY_INSERT [dbo].[PlanNacionalDesarrollo] OFF;

SET IDENTITY_INSERT [dbo].[PlanesNacionalesDesarrollo] ON;
INSERT INTO [dbo].[PlanesNacionalesDesarrollo] ([PlanNacionalId], [Nombre], [PeriodoInicio], [PeriodoFin], [Estado], [FechaCreacion])
VALUES 
(1, 'Plan Nacional de Desarrollo 2026-2030', 2026, 2030, 'Vigente', SYSUTCDATETIME());
SET IDENTITY_INSERT [dbo].[PlanesNacionalesDesarrollo] OFF;

SET IDENTITY_INSERT [dbo].[ObjetivosEstrategico] ON;
INSERT INTO [dbo].[ObjetivosEstrategico] ([ObjetivoEstrategicoId], [PlanNacionalId], [Codigo], [Nombre], [Descripcion])
VALUES 
(1, 1, 'EJE-2', 'Garantizar el derecho a la salud integral', 'Fortalecer la red de salud pública');
SET IDENTITY_INSERT [dbo].[ObjetivosEstrategico] OFF;


PRINT 'Insertando Datos de Planificación Institucional (PEI y Proyectos)...';

SET IDENTITY_INSERT [dbo].[PlanesEstrategicos] ON;
INSERT INTO [dbo].[PlanesEstrategicos] ([PlanEstrategicoId], [Entidad], [PeriodoInicio], [PeriodoFin], [Estado], [FechaCreacion])
VALUES 
(1, 'Ministerio de Salud Pública (MSP)', 2026, 2030, 'Aprobado', SYSUTCDATETIME()),
(2, 'Ministerio de Educación (MINEDUC)', 2026, 2030, 'Borrador', SYSUTCDATETIME());
SET IDENTITY_INSERT [dbo].[PlanesEstrategicos] OFF;

SET IDENTITY_INSERT [dbo].[ProyectosInversion] ON;
INSERT INTO [dbo].[ProyectosInversion] ([ProyectoInversionId], [PlanEstrategicoId], [CodigoProyecto], [Nombre], [Monto], [Estado])
VALUES 
(1, 1, 'CUP-MSP-2026-001', 'Construcción y Equipamiento del Hospital General del Norte', 45500000.00, 'Ejecucion'),
(2, 1, 'CUP-MSP-2026-002', 'Plan Nacional de Vacunación Integral', 12300000.00, 'Aprobado'),
(3, 2, 'CUP-MINEDUC-2026-001', 'Dotación de Laboratorios de Computación en Escuelas Rurales', 8500000.00, 'Formulacion');
SET IDENTITY_INSERT [dbo].[ProyectosInversion] OFF;

PRINT '¡Seed completado para SNP_PlanificacionInstitucional!';
GO