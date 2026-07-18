-- =====================================================================
-- SCRIPT DE SEED DATA REALISTA
-- Base de Datos: SNP_MacroPlanificacion
-- =====================================================================

USE [SNP_MacroPlanificacion];
GO

PRINT 'Limpiando base de datos SNP_MacroPlanificacion...';
DELETE FROM [dbo].[ObjetivosEstrategicos];
DELETE FROM [dbo].[PlanesNacionalesDesarrollo];
GO

PRINT 'Insertando Plan Nacional de Desarrollo...';

SET IDENTITY_INSERT [dbo].[PlanesNacionalesDesarrollo] ON;
INSERT INTO [dbo].[PlanesNacionalesDesarrollo] ([PlanNacionalId], [Nombre], [PeriodoInicio], [PeriodoFin], [Estado], [FechaCreacion])
VALUES 
(1, 'Plan Nacional de Desarrollo: Creación de Oportunidades 2026-2030', 2026, 2030, 'Vigente', SYSUTCDATETIME());
SET IDENTITY_INSERT [dbo].[PlanesNacionalesDesarrollo] OFF;

PRINT 'Insertando Objetivos Estratégicos Macro...';

SET IDENTITY_INSERT [dbo].[ObjetivosEstrategicos] ON;
INSERT INTO [dbo].[ObjetivosEstrategicos] ([ObjetivoEstrategicoId], [PlanNacionalId], [Codigo], [Nombre], [Descripcion])
VALUES 
(1, 1, 'EJE-1', 'Erradicar la pobreza extrema y reducir las desigualdades', 'Garantizar inclusión económica y social para grupos vulnerables.'),
(2, 1, 'EJE-2', 'Garantizar el derecho a la salud integral', 'Fortalecer la red de salud pública, prevención y atención primaria.'),
(3, 1, 'EJE-3', 'Impulsar el sistema educativo inclusivo y de calidad', 'Reducir la brecha digital y mejorar la infraestructura educativa.');
SET IDENTITY_INSERT [dbo].[ObjetivosEstrategicos] OFF;

PRINT '¡Seed completado para SNP_MacroPlanificacion!';
GO