-- =====================================================================
-- SCRIPT DE SEED DATA REALISTA
-- Base de Datos: SNP_ControlCalidad
-- =====================================================================

USE [SNP_ControlCalidad];
GO

PRINT 'Limpiando base de datos SNP_ControlCalidad...';
IF OBJECT_ID(N'dbo.AuditoriaDocumentos', N'U') IS NOT NULL
BEGIN
	DELETE FROM [dbo].[AuditoriaDocumentos];
END;
DELETE FROM [dbo].[Auditorias];
DELETE FROM [dbo].[Revisiones];
GO

PRINT 'Insertando Revisiones Técnicas...';

-- IDs alineados con "4 seed_data_planificaci_n_institucional.sql":
-- PEI 1 = MSP (entidad 1, proyectos 1 y 2), PEI 2 = MINEDUC (entidad 2, proyecto 3).
SET IDENTITY_INSERT [dbo].[Revisiones] ON;
INSERT INTO [dbo].[Revisiones] ([RevisionId], [CodigoRevision], [Modulo], [PlanEstrategicoId], [ProyectoInversionId], [EntidadPublicaId], [EntidadNombre], [CodigoProyecto], [Estado], [FechaRevision], [Observaciones])
VALUES 
(1, 'REV-CUP-MSP-2026-001', 'MSP · PEI #1 · CUP-MSP-2026-001', 1, 1, 1, 'Ministerio de Salud Pública (MSP)', 'CUP-MSP-2026-001', 'Aprobada', DATEADD(DAY, -15, SYSUTCDATETIME()), 'El proyecto hospitalario cumple con los lineamientos del PND.'),
(2, 'REV-CUP-MINEDUC-001', 'MINEDUC · PEI #2 · CUP-MINEDUC-2026-001', 2, 3, 2, 'Ministerio de Educación (MINEDUC)', 'CUP-MINEDUC-2026-001', 'Pendiente', SYSUTCDATETIME(), 'A la espera de justificación técnica del presupuesto.'),
(3, 'REV-CUP-MSP-2026-002', 'MSP · PEI #1 · CUP-MSP-2026-002', 1, 2, 1, 'Ministerio de Salud Pública (MSP)', 'CUP-MSP-2026-002', 'Rechazada', DATEADD(DAY, -5, SYSUTCDATETIME()), 'Falta alinear el plan de vacunación al EJE-2 del PND.');
SET IDENTITY_INSERT [dbo].[Revisiones] OFF;

PRINT 'Insertando Auditorías...';

SET IDENTITY_INSERT [dbo].[Auditorias] ON;
INSERT INTO [dbo].[Auditorias] ([AuditoriaId], [RevisionId], [Tipo], [Resultado], [Responsable], [FechaRegistro])
VALUES 
(1, 1, 'Auditoría Interna', 'Conforme', 'Ana Planificadora (MSP)', DATEADD(DAY, -16, SYSUTCDATETIME())),
(2, 1, 'Revisión de Cumplimiento', 'Conforme', 'Carlos Administrador (SNP)', DATEADD(DAY, -15, SYSUTCDATETIME())),
(3, 3, 'Auditoría Externa', 'No Conforme', 'Luis Auditor Externo', DATEADD(DAY, -5, SYSUTCDATETIME()));
SET IDENTITY_INSERT [dbo].[Auditorias] OFF;

PRINT '¡Seed completado para SNP_ControlCalidad!';
GO