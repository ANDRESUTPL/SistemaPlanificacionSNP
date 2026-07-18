-- =====================================================================
-- SCRIPT DE SEED DATA REALISTA
-- Base de Datos: SNP_Parametrizacion (Microservicio de Parametrización)
-- Actualizado con estructura de Periodos, Entidades y Descripciones
-- =====================================================================

USE [SNP_Parametrizacion];
GO

PRINT 'Limpiando base de datos SNP_Parametrizacion...';
-- El orden de eliminación es importante por las Foreign Keys
DELETE FROM [dbo].[EntidadesPublicas];
DELETE FROM [dbo].[PeriodosPlanificacion];
DELETE FROM [dbo].[ItemsCatalogo];
DELETE FROM [dbo].[Catalogos];
GO

-- ==========================================
-- 1. PERIODOS DE PLANIFICACIÓN
-- ==========================================
PRINT 'Insertando Periodos de Planificación...';

SET IDENTITY_INSERT [dbo].[PeriodosPlanificacion] ON;
INSERT INTO [dbo].[PeriodosPlanificacion] ([PeriodoPlanificacionId], [Codigo], [Nombre], [FechaInicio], [FechaFin], [Activo])
VALUES 
(1, 'PER-2026-2030', 'Planificación Nacional de Desarrollo 2026-2030', '2026-01-01', '2030-12-31', 1);
SET IDENTITY_INSERT [dbo].[PeriodosPlanificacion] OFF;

-- ==========================================
-- 2. ENTIDADES PÚBLICAS
-- ==========================================
PRINT 'Insertando Entidades Públicas...';

SET IDENTITY_INSERT [dbo].[EntidadesPublicas] ON;
INSERT INTO [dbo].[EntidadesPublicas] ([EntidadPublicaId], [Codigo], [Nombre], [Sigla], [Mision], [PeriodoPlanificacionId], [Activo])
VALUES 
(1, 'ENT-MSP', 'Ministerio de Salud Pública', 'MSP', 'Garantizar el derecho a la salud integral de la población.', 1, 1),
(2, 'ENT-MINEDUC', 'Ministerio de Educación', 'MINEDUC', 'Garantizar una educación inclusiva, equitativa y de calidad.', 1, 1),
(3, 'ENT-MIES', 'Ministerio de Inclusión Económica y Social', 'MIES', 'Definir y ejecutar políticas para la inclusión social de grupos vulnerables.', 1, 1);
SET IDENTITY_INSERT [dbo].[EntidadesPublicas] OFF;

-- ==========================================
-- 3. CATÁLOGOS MAESTROS
-- ==========================================
PRINT 'Insertando Catálogos Core del Sistema...';

SET IDENTITY_INSERT [dbo].[Catalogos] ON;
INSERT INTO [dbo].[Catalogos] ([CatalogoId], [Codigo], [Nombre], [Descripcion], [Activo], [FechaCreacion])
VALUES 
(1, 'ESTADOS_PLAN', 'Estados de un Plan o Proyecto', 'Define el ciclo de vida operativo de los instrumentos de planificación', 1, GETDATE()),
(2, 'TIPOS_AUDITORIA', 'Tipos de Auditoría de Calidad', 'Clasificación de las revisiones de cumplimiento y calidad técnica', 1, GETDATE()),
(3, 'NIVELES_GOBIERNO', 'Niveles de Gobierno', 'Clasificación territorial y administrativa del estado', 1, GETDATE());
SET IDENTITY_INSERT [dbo].[Catalogos] OFF;

-- ==========================================
-- 4. ÍTEMS DE CATÁLOGOS
-- ==========================================
PRINT 'Insertando Items de Catálogo...';

SET IDENTITY_INSERT [dbo].[ItemsCatalogo] ON;
-- Se incluye [Valor] (antiguo campo si se requiere como NULL) y la nueva [FechaCreacion] usando GETDATE()
INSERT INTO [dbo].[ItemsCatalogo] ([ItemCatalogoId], [CatalogoId], [Codigo], [Nombre], [Valor], [Orden], [Activo], [Descripcion], [FechaCreacion])
VALUES 
(1, 1, 'EST_BORRADOR', 'Borrador', NULL, 1, 1, 'En fase de formulación interna', GETDATE()),
(2, 1, 'EST_REVISION', 'En Revisión', NULL, 2, 1, 'Enviado a SNP para control de calidad', GETDATE()),
(3, 1, 'EST_APROBADO', 'Aprobado', NULL, 3, 1, 'Plan o Proyecto vigente y validado', GETDATE()),
(4, 1, 'EST_CERRADO', 'Cerrado', NULL, 4, 1, 'Ejecución finalizada y liquidada', GETDATE()),

(5, 2, 'AUD_INTERNA', 'Auditoría Interna', NULL, 1, 1, 'Control de calidad ejecutado por la misma institución', GETDATE()),
(6, 2, 'AUD_EXTERNA', 'Auditoría Externa', NULL, 2, 1, 'Revisión técnica ejecutada por el ente rector (SNP)', GETDATE()),

(7, 3, 'NIV_CENTRAL', 'Gobierno Central', NULL, 1, 1, 'Ministerios, Secretarías y Consejos de Estado', GETDATE()),
(8, 3, 'NIV_GAD_PROV', 'GAD Provincial', NULL, 2, 1, 'Gobiernos Autónomos Provinciales', GETDATE()),
(9, 3, 'NIV_GAD_MUN', 'GAD Municipal', NULL, 3, 1, 'Gobiernos Autónomos Municipales', GETDATE());
SET IDENTITY_INSERT [dbo].[ItemsCatalogo] OFF;

PRINT '¡Seed completado con éxito para SNP_Parametrizacion!';
GO