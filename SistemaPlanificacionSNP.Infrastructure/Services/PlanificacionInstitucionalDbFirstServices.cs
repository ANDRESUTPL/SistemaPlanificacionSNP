using SistemaPlanificacionSNP.Domain.Entities.PlanificacionInstitucional;
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
    }

    public class PlanesEstrategicosPiService : IPlanesEstrategicosPiService
    {
        private readonly IPlanificacionInstitucionalUnitOfWork _unitOfWork;

        public PlanesEstrategicosPiService(IPlanificacionInstitucionalUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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

            var entidad = dto.Entidad.Trim();
            var estado = dto.Estado.Trim();

            var exists = await _unitOfWork.PlanesEstrategicos.ExistsByEntidadPeriodoAsync(entidad, dto.PeriodoInicio, dto.PeriodoFin);
            if (exists)
            {
                throw new InvalidOperationException("Ya existe un plan para la entidad y periodo indicado");
            }

            var entity = new PlanesEstrategico
            {
                Entidad = entidad,
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

            if (!string.IsNullOrWhiteSpace(dto.Entidad))
            {
                if (dto.Entidad.Length > 200)
                {
                    throw new InvalidOperationException("La entidad supera el máximo de 200 caracteres");
                }
                entity.Entidad = dto.Entidad.Trim();
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

            if (!string.IsNullOrWhiteSpace(dto.Estado))
            {
                entity.Estado = dto.Estado.Trim();
            }

            var duplicate = await _unitOfWork.PlanesEstrategicos.ExistsByEntidadPeriodoAsync(
                entity.Entidad,
                entity.PeriodoInicio,
                entity.PeriodoFin,
                entity.PlanEstrategicoId);

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

            var hasActiveProjects = await _unitOfWork.PlanesEstrategicos.HasActiveProjectsAsync(planId);
            if (hasActiveProjects)
            {
                throw new InvalidOperationException("No se puede inactivar el plan porque tiene proyectos activos");
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
            if (string.IsNullOrWhiteSpace(dto.Entidad) || string.IsNullOrWhiteSpace(dto.Estado))
            {
                throw new InvalidOperationException("Entidad y estado son requeridos");
            }

            if (dto.Entidad.Length > 200)
            {
                throw new InvalidOperationException("La entidad supera el máximo de 200 caracteres");
            }

            ValidatePeriodo(dto.PeriodoInicio, dto.PeriodoFin);
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
