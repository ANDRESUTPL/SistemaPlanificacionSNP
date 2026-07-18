-- =====================================================================
-- SCRIPT DE SEED DATA REALISTA
-- Base de Datos: SNP_ControlCalidad
-- =====================================================================

USE [SNP_ControlCalidad];
GO

PRINT 'Limpiando base de datos SNP_ControlCalidad...';
DELETE FROM [dbo].[Auditorias];
DELETE FROM [dbo].[Revisiones];
GO

PRINT 'Insertando Revisiones Técnicas...';

SET IDENTITY_INSERT [dbo].[Revisiones] ON;
INSERT INTO [dbo].[Revisiones] ([RevisionId], [CodigoRevision], [Modulo], [Estado], [FechaRevision], [Observaciones])
VALUES 
(1, 'REV-PEI-MSP-2026', 'Plan Estratégico Ministerio de Salud Pública', 'Aprobada', DATEADD(DAY, -15, SYSUTCDATETIME()), 'El PEI cumple con los lineamientos del PND.'),
(2, 'REV-CUP-MINEDUC-001', 'Proyecto: Laboratorios Escuelas Rurales', 'Pendiente', SYSUTCDATETIME(), 'A la espera de justificación técnica del presupuesto.'),
(3, 'REV-PEI-MIES-2026', 'Plan Estratégico MIES', 'Rechazada', DATEADD(DAY, -5, SYSUTCDATETIME()), 'Falta alinear los programas al EJE-1 del PND.');
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