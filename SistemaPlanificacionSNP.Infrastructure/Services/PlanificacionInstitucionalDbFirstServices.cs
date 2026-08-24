using SistemaPlanificacionSNP.Domain.Entities.PlanificacionInstitucional;
using Microsoft.EntityFrameworkCore;
using SistemaPlanificacionSNP.Infrastructure.Data;
using SistemaPlanificacionSNP.Infrastructure.DTOs;
using SistemaPlanificacionSNP.Infrastructure.UnitOfWork;

namespace SistemaPlanificacionSNP.Infrastructure.Services
{
    public interface IPlanesEstrategicosPiService
    {
        Task<(List<PlanesEstrategico> Items, int Total)> GetPagedAsync(PlanesEstrategicoQueryDto query);
        Task<PlanesEstrategico?> GetByIdAsync(int planId, bool includeProyectos = false);
        Task<PlanesEstrategico> CreateAsync(PlanesEstrategicoCreateDto dto);
        Task<PlanesEstrategico?> UpdateAsync(int planId, PlanesEstrategicoUpdateDto dto);
        Task<bool> SoftDeleteAsync(int planId);
        Task<PlanificacionInstitucionalDashboardDbFirstDto> GetDashboardAsync();
    }

    public interface IProyectosInversionPiService
    {
        Task<(List<ProyectosInversion> Items, int Total)> GetPagedAsync(ProyectosInversionQueryDto query);
        Task<ProyectosInversion?> GetByIdAsync(int proyectoId, bool includePlan = false);
        Task<ProyectosInversion> CreateAsync(ProyectosInversionCreateDto dto);
        Task<ProyectosInversion?> UpdateAsync(int proyectoId, ProyectosInversionUpdateDto dto);
        Task<bool> SoftDeleteAsync(int proyectoId);
        Task<List<RespaldoEjecucion>> AddRespaldosAsync(int proyectoId, IEnumerable<RespaldoEjecucionCreateDto> respaldos);
    }

    public class PlanesEstrategicosPiService : IPlanesEstrategicosPiService
    {
        private readonly IPlanificacionInstitucionalUnitOfWork _unitOfWork;
        private readonly MacroPlanificacionDbContext _macroPlanificacionContext;

        public PlanesEstrategicosPiService(
            IPlanificacionInstitucionalUnitOfWork unitOfWork,
            MacroPlanificacionDbContext macroPlanificacionContext)
        {
            _unitOfWork = unitOfWork;
            _macroPlanificacionContext = macroPlanificacionContext;
        }

        public async Task<(List<PlanesEstrategico> Items, int Total)> GetPagedAsync(PlanesEstrategicoQueryDto query)
        {
            NormalizePaging(query.PageNumber, query.PageSize, out var pageNumber, out var pageSize);
            query.PageNumber = pageNumber;
            query.PageSize = pageSize;

            var items = await _unitOfWork.PlanesEstrategicos.GetPagedAsync(query);
            var total = await _unitOfWork.PlanesEstrategicos.CountFilteredAsync(query);
            return (items, total);
        }

        public async Task<PlanesEstrategico?> GetByIdAsync(int planId, bool includeProyectos = false)
        {
            return includeProyectos
                ? await _unitOfWork.PlanesEstrategicos.GetByIdWithProyectosAsync(planId)
                : await _unitOfWork.PlanesEstrategicos.GetByIdAsync(planId);
        }

        public async Task<PlanesEstrategico> CreateAsync(PlanesEstrategicoCreateDto dto)
        {
            ValidateCreate(dto);
            await SincronizarPeriodoConPlanNacionalAsync(dto);

            var entidad = dto.Entidad.Trim();
            var estado = dto.Estado.Trim();

            var exists = await _unitOfWork.PlanesEstrategicos.ExistsByEntidadPublicaPeriodoAsync(dto.EntidadPublicaId, dto.PeriodoInicio, dto.PeriodoFin, periodoPlanificacionId: dto.PeriodoPlanificacionId);
            if (exists)
            {
                throw new InvalidOperationException("Ya existe un plan para la entidad y periodo indicado");
            }

            var entity = new PlanesEstrategico
            {
                Entidad = entidad,
                EntidadPublicaId = dto.EntidadPublicaId,
                PlanNacionalId = dto.PlanNacionalId,
                PeriodoPlanificacionId = dto.PeriodoPlanificacionId,
                PeriodoInicio = dto.PeriodoInicio,
                PeriodoFin = dto.PeriodoFin,
                Estado = estado,
                FechaCreacion = DateTime.UtcNow
            };

            await _unitOfWork.PlanesEstrategicos.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return entity;
        }

        public async Task<PlanesEstrategico?> UpdateAsync(int planId, PlanesEstrategicoUpdateDto dto)
        {
            var entity = await _unitOfWork.PlanesEstrategicos.GetByIdAsync(planId);
            if (entity == null)
            {
                return null;
            }

            var planNacionalId = dto.PlanNacionalId ?? entity.PlanNacionalId;
            if (!planNacionalId.HasValue || planNacionalId.Value <= 0)
            {
                throw new InvalidOperationException("PlanNacionalId es requerido para actualizar el plan");
            }

            var planNacional = await ObtenerPlanNacionalAsync(planNacionalId.Value);
            dto.PlanNacionalId = planNacional.PlanNacionalId;
            dto.PeriodoPlanificacionId = planNacional.PeriodoPlanificacionId;
            dto.PeriodoInicio = planNacional.PeriodoInicio;
            dto.PeriodoFin = planNacional.PeriodoFin;

            if (dto.PeriodoPlanificacionId.HasValue && dto.PeriodoPlanificacionId.Value <= 0)
            {
                throw new InvalidOperationException("PeriodoPlanificacionId inválido");
            }

            if (!dto.PlanNacionalId.HasValue || dto.PlanNacionalId.Value <= 0)
            {
                throw new InvalidOperationException("PlanNacionalId es requerido");
            }

            if (dto.EntidadPublicaId.HasValue && dto.EntidadPublicaId.Value <= 0)
            {
                throw new InvalidOperationException("EntidadPublicaId inválido");
            }

            if (!string.IsNullOrWhiteSpace(dto.Entidad))
            {
                if (dto.Entidad.Length > 200)
                {
                    throw new InvalidOperationException("La entidad supera el máximo de 200 caracteres");
                }
                entity.Entidad = dto.Entidad.Trim();
            }

            if (dto.EntidadPublicaId.HasValue)
            {
                entity.EntidadPublicaId = dto.EntidadPublicaId.Value;
            }

            if (dto.PeriodoInicio.HasValue)
            {
                ValidatePeriodo(dto.PeriodoInicio.Value, dto.PeriodoFin ?? entity.PeriodoFin);
                entity.PeriodoInicio = dto.PeriodoInicio.Value;
            }

            if (dto.PeriodoFin.HasValue)
            {
                ValidatePeriodo(dto.PeriodoInicio ?? entity.PeriodoInicio, dto.PeriodoFin.Value);
                entity.PeriodoFin = dto.PeriodoFin.Value;
            }

            if (dto.PeriodoPlanificacionId.HasValue)
            {
                entity.PeriodoPlanificacionId = dto.PeriodoPlanificacionId.Value;
            }

			if (dto.PlanNacionalId.HasValue)
			{
				entity.PlanNacionalId = dto.PlanNacionalId.Value;
			}

			if (!string.IsNullOrWhiteSpace(dto.Estado))
            {
                entity.Estado = dto.Estado.Trim();
            }

            if (!entity.EntidadPublicaId.HasValue)
            {
                throw new InvalidOperationException("EntidadPublicaId es requerido para actualizar el plan");
            }

            var duplicate = await _unitOfWork.PlanesEstrategicos.ExistsByEntidadPublicaPeriodoAsync(
                entity.EntidadPublicaId.Value,
                entity.PeriodoInicio,
                entity.PeriodoFin,
                entity.PlanEstrategicoId,
                entity.PeriodoPlanificacionId);

            if (duplicate)
            {
                throw new InvalidOperationException("Ya existe un plan para la entidad y periodo indicado");
            }

            await _unitOfWork.PlanesEstrategicos.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> SoftDeleteAsync(int planId)
        {
            var entity = await _unitOfWork.PlanesEstrategicos.GetByIdAsync(planId);
            if (entity == null)
            {
                return false;
            }

            var hasProjects = await _unitOfWork.PlanesEstrategicos.HasProjectsAsync(planId);
            if (hasProjects)
            {
                throw new InvalidOperationException("No se puede eliminar el plan porque tiene proyectos asociados");
            }

            entity.Estado = "Inactivo";
            await _unitOfWork.PlanesEstrategicos.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<PlanificacionInstitucionalDashboardDbFirstDto> GetDashboardAsync()
        {
            var totalPlanes = await _unitOfWork.PlanesEstrategicos.CountFilteredAsync(new PlanesEstrategicoQueryDto
            {
                PageNumber = 1,
                PageSize = 1_000_000
            });

            var totalPlanesActivos = await _unitOfWork.PlanesEstrategicos.CountFilteredAsync(new PlanesEstrategicoQueryDto
            {
                PageNumber = 1,
                PageSize = 1_000_000,
                Estado = "Activo"
            });

            var proyectos = await _unitOfWork.ProyectosInversion.GetPagedAsync(new ProyectosInversionQueryDto
            {
                PageNumber = 1,
                PageSize = 1_000_000
            });

            var proyectosActivos = proyectos
                .Where(x => !string.Equals(x.Estado, "Inactivo", StringComparison.OrdinalIgnoreCase))
                .ToList();

            return new PlanificacionInstitucionalDashboardDbFirstDto
            {
                TotalPlanes = totalPlanes,
                TotalPlanesActivos = totalPlanesActivos,
                TotalProyectos = proyectos.Count,
                TotalProyectosActivos = proyectosActivos.Count,
                MontoTotalProyectosActivos = proyectosActivos.Sum(x => x.Monto)
            };
        }

        private static void ValidateCreate(PlanesEstrategicoCreateDto dto)
        {
            if (dto.EntidadPublicaId <= 0)
            {
                throw new InvalidOperationException("EntidadPublicaId es requerido");
            }

            if (!dto.PlanNacionalId.HasValue || dto.PlanNacionalId.Value <= 0)
            {
                throw new InvalidOperationException("PlanNacionalId es requerido");
            }

            if (string.IsNullOrWhiteSpace(dto.Entidad) || string.IsNullOrWhiteSpace(dto.Estado))
            {
                throw new InvalidOperationException("Entidad y estado son requeridos");
            }

            if (dto.PeriodoPlanificacionId.HasValue && dto.PeriodoPlanificacionId.Value <= 0)
            {
                throw new InvalidOperationException("PeriodoPlanificacionId inválido");
            }

            if (dto.PeriodoInicio <= 0 || dto.PeriodoFin <= 0)
            {
                throw new InvalidOperationException("PeriodoInicio y PeriodoFin son requeridos");
            }

            if (dto.Entidad.Length > 200)
            {
                throw new InvalidOperationException("La entidad supera el máximo de 200 caracteres");
            }

            ValidatePeriodo(dto.PeriodoInicio, dto.PeriodoFin);
        }

        private async Task SincronizarPeriodoConPlanNacionalAsync(PlanesEstrategicoCreateDto dto)
        {
            var planNacional = await ObtenerPlanNacionalAsync(dto.PlanNacionalId!.Value);
            dto.PeriodoPlanificacionId = planNacional.PeriodoPlanificacionId;
            dto.PeriodoInicio = planNacional.PeriodoInicio;
            dto.PeriodoFin = planNacional.PeriodoFin;
        }

        private async Task<Domain.Entities.MacroPlanificacion.PlanesNacionalesDesarrollo> ObtenerPlanNacionalAsync(int planNacionalId)
        {
            var planNacional = await _macroPlanificacionContext.PlanesNacionalesDesarrollos
                .AsNoTracking()
                .SingleOrDefaultAsync(plan => plan.PlanNacionalId == planNacionalId);

            if (planNacional?.PeriodoPlanificacionId is not int periodoPlanificacionId || periodoPlanificacionId <= 0)
            {
                throw new InvalidOperationException("El Plan Nacional seleccionado no existe o no tiene un período de planificación válido");
            }

            ValidatePeriodo(planNacional.PeriodoInicio, planNacional.PeriodoFin);
            return planNacional;
        }

        private static void ValidatePeriodo(int periodoInicio, int periodoFin)
        {
            if (periodoInicio > periodoFin)
            {
                throw new InvalidOperationException("PeriodoInicio no puede ser mayor a PeriodoFin");
            }
        }

        private static void NormalizePaging(int pageNumber, int pageSize, out int normalizedPageNumber, out int normalizedPageSize)
        {
            normalizedPageNumber = pageNumber < 1 ? 1 : pageNumber;
            normalizedPageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 100);
        }
    }

    public class ProyectosInversionPiService : IProyectosInversionPiService
    {
        private readonly IPlanificacionInstitucionalUnitOfWork _unitOfWork;

        public ProyectosInversionPiService(IPlanificacionInstitucionalUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<(List<ProyectosInversion> Items, int Total)> GetPagedAsync(ProyectosInversionQueryDto query)
        {
            NormalizePaging(query.PageNumber, query.PageSize, out var pageNumber, out var pageSize);
            query.PageNumber = pageNumber;
            query.PageSize = pageSize;

            var items = await _unitOfWork.ProyectosInversion.GetPagedAsync(query);
            var total = await _unitOfWork.ProyectosInversion.CountFilteredAsync(query);
            return (items, total);
        }

        public async Task<ProyectosInversion?> GetByIdAsync(int proyectoId, bool includePlan = false)
        {
            return includePlan
                ? await _unitOfWork.ProyectosInversion.GetByIdWithPlanAsync(proyectoId)
                : await _unitOfWork.ProyectosInversion.GetByIdAsync(proyectoId);
        }

        public async Task<ProyectosInversion> CreateAsync(ProyectosInversionCreateDto dto)
        {
            ValidateCreate(dto);

            var plan = await _unitOfWork.PlanesEstrategicos.GetByIdAsync(dto.PlanEstrategicoId);
            if (plan == null)
            {
                throw new InvalidOperationException("El plan estratégico no existe");
            }

            var exists = await _unitOfWork.ProyectosInversion.ExistsCodigoAsync(dto.CodigoProyecto.Trim());
            if (exists)
            {
                throw new InvalidOperationException("Ya existe un proyecto con ese código");
            }

            var entity = new ProyectosInversion
            {
                PlanEstrategicoId = dto.PlanEstrategicoId,
                CodigoProyecto = dto.CodigoProyecto.Trim(),
                Nombre = dto.Nombre.Trim(),
                Monto = dto.Monto,
                Estado = dto.Estado.Trim()
            };

            await _unitOfWork.ProyectosInversion.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return entity;
        }

        public async Task<ProyectosInversion?> UpdateAsync(int proyectoId, ProyectosInversionUpdateDto dto)
        {
            var entity = await _unitOfWork.ProyectosInversion.GetByIdAsync(proyectoId);
            if (entity == null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(dto.Nombre))
            {
                if (dto.Nombre.Length > 250)
                {
                    throw new InvalidOperationException("El nombre supera el máximo de 250 caracteres");
                }
                entity.Nombre = dto.Nombre.Trim();
            }

            if (dto.Monto.HasValue)
            {
                if (dto.Monto.Value < 0)
                {
                    throw new InvalidOperationException("El monto no puede ser negativo");
                }
                entity.Monto = dto.Monto.Value;
            }

            if (!string.IsNullOrWhiteSpace(dto.Estado))
            {
                entity.Estado = dto.Estado.Trim();
            }

			if (dto.AvanceFisico.HasValue)
			{
				// Validar que no sea menor a 0 o mayor a 100 si es necesario
				entity.AvanceFisico = dto.AvanceFisico.Value;
			}

			if (dto.AvanceFinanciero.HasValue)
			{
				entity.AvanceFinanciero = dto.AvanceFinanciero.Value;
			}

			if (dto.Observaciones != null)
			{
				entity.Observaciones = dto.Observaciones.Trim();
			}

			await _unitOfWork.ProyectosInversion.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> SoftDeleteAsync(int proyectoId)
        {
            var entity = await _unitOfWork.ProyectosInversion.GetByIdAsync(proyectoId);
            if (entity == null)
            {
                return false;
            }

            entity.Estado = "Inactivo";
            await _unitOfWork.ProyectosInversion.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<List<RespaldoEjecucion>> AddRespaldosAsync(int proyectoId, IEnumerable<RespaldoEjecucionCreateDto> respaldos)
        {
            var proyecto = await _unitOfWork.ProyectosInversion.GetByIdAsync(proyectoId);
            if (proyecto == null)
            {
                throw new InvalidOperationException("Proyecto no encontrado");
            }

            var permitidas = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".jpg", ".jpeg", ".png" };
            var carpeta = Path.Combine(AppContext.BaseDirectory, "uploads", "respaldos-ejecucion", proyectoId.ToString());
            Directory.CreateDirectory(carpeta);
            var resultado = new List<RespaldoEjecucion>();

            foreach (var respaldo in respaldos)
            {
                var nombre = Path.GetFileName(respaldo.NombreArchivo);
                var extension = Path.GetExtension(nombre).ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(nombre) || !permitidas.Contains(extension))
                {
                    throw new InvalidOperationException("Solo se permiten respaldos PDF, Word, Excel o imágenes.");
                }
                if (respaldo.TamanoBytes <= 0 || respaldo.TamanoBytes > 10 * 1024 * 1024)
                {
                    throw new InvalidOperationException("Cada respaldo debe tener un tamaño máximo de 10 MB.");
                }

                var nombreGuardado = $"{Guid.NewGuid():N}{extension}";
                var ruta = Path.Combine(carpeta, nombreGuardado);
                await using (var archivo = File.Create(ruta))
                {
                    await respaldo.Contenido.CopyToAsync(archivo);
                }

                var entidad = new RespaldoEjecucion
                {
                    ProyectoInversionId = proyectoId,
                    NombreArchivo = nombre,
                    RutaArchivo = Path.Combine("uploads", "respaldos-ejecucion", proyectoId.ToString(), nombreGuardado),
                    TipoContenido = respaldo.TipoContenido,
                    TamanoBytes = respaldo.TamanoBytes,
                    FechaCarga = DateTime.UtcNow
                };
                await _unitOfWork.ProyectosInversion.AddRespaldoAsync(entidad);
                resultado.Add(entidad);
            }

            await _unitOfWork.SaveChangesAsync();
            return resultado;
        }

        private static void ValidateCreate(ProyectosInversionCreateDto dto)
        {
            if (dto.PlanEstrategicoId <= 0)
            {
                throw new InvalidOperationException("PlanEstrategicoId es requerido");
            }

            if (string.IsNullOrWhiteSpace(dto.CodigoProyecto) || string.IsNullOrWhiteSpace(dto.Nombre) || string.IsNullOrWhiteSpace(dto.Estado))
            {
                throw new InvalidOperationException("Código, nombre y estado son requeridos");
            }

            if (dto.CodigoProyecto.Length > 50)
            {
                throw new InvalidOperationException("El código del proyecto supera el máximo de 50 caracteres");
            }

            if (dto.Nombre.Length > 250)
            {
                throw new InvalidOperationException("El nombre supera el máximo de 250 caracteres");
            }

            if (dto.Monto < 0)
            {
                throw new InvalidOperationException("El monto no puede ser negativo");
            }
        }

        private static void NormalizePaging(int pageNumber, int pageSize, out int normalizedPageNumber, out int normalizedPageSize)
        {
            normalizedPageNumber = pageNumber < 1 ? 1 : pageNumber;
            normalizedPageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 100);
        }
    }
}
