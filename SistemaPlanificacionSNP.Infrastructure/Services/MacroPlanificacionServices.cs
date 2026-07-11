using SistemaPlanificacionSNP.Domain.Entities.MacroPlanificacion;
using SistemaPlanificacionSNP.Infrastructure.DTOs;
using SistemaPlanificacionSNP.Infrastructure.UnitOfWork;

namespace SistemaPlanificacionSNP.Infrastructure.Services
{
    public interface IMacroPlanNacionalService
    {
        Task<(List<PlanesNacionalesDesarrollo> Items, int Total)> GetPagedAsync(MacroPlanNacionalQueryDto query);
        Task<PlanesNacionalesDesarrollo?> GetByIdAsync(int planNacionalId);
        Task<PlanesNacionalesDesarrollo?> GetByIdWithObjetivosAsync(int planNacionalId);
        Task<PlanesNacionalesDesarrollo> CreateAsync(MacroPlanNacionalCreateDto dto, string actorId);
        Task<PlanesNacionalesDesarrollo?> UpdateAsync(int planNacionalId, MacroPlanNacionalUpdateDto dto, string actorId);
        Task<bool> DeleteAsync(int planNacionalId, string actorId);
        Task<MacroPlanNacionalResumenDto> GetResumenAsync();
    }

    public interface IMacroObjetivoEstrategicoService
    {
        Task<(List<ObjetivosEstrategico> Items, int Total)> GetPagedAsync(MacroObjetivoEstrategicoQueryDto query);
        Task<ObjetivosEstrategico?> GetByIdAsync(int objetivoEstrategicoId);
        Task<List<ObjetivosEstrategico>> GetByPlanNacionalIdAsync(int planNacionalId);
        Task<ObjetivosEstrategico> CreateAsync(MacroObjetivoEstrategicoCreateDto dto, string actorId);
        Task<ObjetivosEstrategico?> UpdateAsync(int objetivoEstrategicoId, MacroObjetivoEstrategicoUpdateDto dto, string actorId);
        Task<bool> DeleteAsync(int objetivoEstrategicoId, string actorId);
    }

    public class MacroPlanNacionalService : IMacroPlanNacionalService
    {
        private readonly IMacroPlanificacionUnitOfWork _unitOfWork;

        public MacroPlanNacionalService(IMacroPlanificacionUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<(List<PlanesNacionalesDesarrollo> Items, int Total)> GetPagedAsync(MacroPlanNacionalQueryDto query)
        {
            NormalizePaging(query.PageNumber, query.PageSize, out var pageNumber, out var pageSize);
            query.PageNumber = pageNumber;
            query.PageSize = pageSize;

            var items = await _unitOfWork.PlanesNacionales.GetPagedAsync(query);
            var total = await _unitOfWork.PlanesNacionales.CountFilteredAsync(query);
            return (items, total);
        }

        public async Task<PlanesNacionalesDesarrollo?> GetByIdAsync(int planNacionalId)
        {
            return await _unitOfWork.PlanesNacionales.GetByIdAsync(planNacionalId);
        }

        public async Task<PlanesNacionalesDesarrollo?> GetByIdWithObjetivosAsync(int planNacionalId)
        {
            return await _unitOfWork.PlanesNacionales.GetByIdWithObjetivosAsync(planNacionalId);
        }

        public async Task<PlanesNacionalesDesarrollo> CreateAsync(MacroPlanNacionalCreateDto dto, string actorId)
        {
            ValidateActor(actorId);
            ValidateCreate(dto);

            var entity = new PlanesNacionalesDesarrollo
            {
                Nombre = dto.Nombre.Trim(),
                PeriodoInicio = dto.PeriodoInicio,
                PeriodoFin = dto.PeriodoFin,
                Estado = dto.Estado.Trim(),
                FechaCreacion = DateTime.UtcNow
            };

            await _unitOfWork.PlanesNacionales.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return entity;
        }

        public async Task<PlanesNacionalesDesarrollo?> UpdateAsync(int planNacionalId, MacroPlanNacionalUpdateDto dto, string actorId)
        {
            ValidateActor(actorId);

            var entity = await _unitOfWork.PlanesNacionales.GetByIdAsync(planNacionalId);
            if (entity == null)
            {
                return null;
            }

            if (dto.Nombre != null)
            {
                if (string.IsNullOrWhiteSpace(dto.Nombre) || dto.Nombre.Trim().Length > 200)
                {
                    throw new InvalidOperationException("Nombre inválido");
                }
                entity.Nombre = dto.Nombre.Trim();
            }

            if (dto.PeriodoInicio.HasValue)
            {
                entity.PeriodoInicio = dto.PeriodoInicio.Value;
            }

            if (dto.PeriodoFin.HasValue)
            {
                entity.PeriodoFin = dto.PeriodoFin.Value;
            }

            if (dto.Estado != null)
            {
                if (string.IsNullOrWhiteSpace(dto.Estado) || dto.Estado.Trim().Length > 30)
                {
                    throw new InvalidOperationException("Estado inválido");
                }
                entity.Estado = dto.Estado.Trim();
            }

            if (entity.PeriodoInicio > entity.PeriodoFin)
            {
                throw new InvalidOperationException("PeriodoInicio no puede ser mayor a PeriodoFin");
            }

            await _unitOfWork.PlanesNacionales.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return entity;
        }

        public async Task<bool> DeleteAsync(int planNacionalId, string actorId)
        {
            ValidateActor(actorId);

            var entity = await _unitOfWork.PlanesNacionales.GetByIdWithObjetivosAsync(planNacionalId);
            if (entity == null)
            {
                return false;
            }

            if (entity.ObjetivosEstrategicos.Count > 0)
            {
                throw new InvalidOperationException("No se puede eliminar el plan porque tiene objetivos asociados");
            }

            await _unitOfWork.PlanesNacionales.RemoveAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<MacroPlanNacionalResumenDto> GetResumenAsync()
        {
            return new MacroPlanNacionalResumenDto
            {
                TotalPlanes = await _unitOfWork.PlanesNacionales.CountTotalAsync(),
                TotalObjetivos = await _unitOfWork.ObjetivosEstrategicos.CountTotalAsync(),
                PlanesPorEstado = await _unitOfWork.PlanesNacionales.GetConteoPorEstadoAsync(),
                PlanesPorVigencia = await _unitOfWork.PlanesNacionales.GetConteoPorVigenciaAsync()
            };
        }

        private static void ValidateCreate(MacroPlanNacionalCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre) || string.IsNullOrWhiteSpace(dto.Estado))
            {
                throw new InvalidOperationException("Nombre y estado son requeridos");
            }

            if (dto.Nombre.Trim().Length > 200)
            {
                throw new InvalidOperationException("El nombre supera el máximo de 200 caracteres");
            }

            if (dto.Estado.Trim().Length > 30)
            {
                throw new InvalidOperationException("El estado supera el máximo de 30 caracteres");
            }

            if (dto.PeriodoInicio > dto.PeriodoFin)
            {
                throw new InvalidOperationException("PeriodoInicio no puede ser mayor a PeriodoFin");
            }
        }

        private static void ValidateActor(string actorId)
        {
            if (string.IsNullOrWhiteSpace(actorId))
            {
                throw new InvalidOperationException("No se pudo determinar la identidad del usuario desde los claims");
            }
        }

        private static void NormalizePaging(int pageNumber, int pageSize, out int normalizedPageNumber, out int normalizedPageSize)
        {
            normalizedPageNumber = pageNumber < 1 ? 1 : pageNumber;
            normalizedPageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 100);
        }
    }

    public class MacroObjetivoEstrategicoService : IMacroObjetivoEstrategicoService
    {
        private readonly IMacroPlanificacionUnitOfWork _unitOfWork;

        public MacroObjetivoEstrategicoService(IMacroPlanificacionUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<(List<ObjetivosEstrategico> Items, int Total)> GetPagedAsync(MacroObjetivoEstrategicoQueryDto query)
        {
            NormalizePaging(query.PageNumber, query.PageSize, out var pageNumber, out var pageSize);
            query.PageNumber = pageNumber;
            query.PageSize = pageSize;

            var items = await _unitOfWork.ObjetivosEstrategicos.GetPagedAsync(query);
            var total = await _unitOfWork.ObjetivosEstrategicos.CountFilteredAsync(query);
            return (items, total);
        }

        public async Task<ObjetivosEstrategico?> GetByIdAsync(int objetivoEstrategicoId)
        {
            return await _unitOfWork.ObjetivosEstrategicos.GetByIdAsync(objetivoEstrategicoId);
        }

        public async Task<List<ObjetivosEstrategico>> GetByPlanNacionalIdAsync(int planNacionalId)
        {
            return await _unitOfWork.ObjetivosEstrategicos.GetByPlanNacionalIdAsync(planNacionalId);
        }

        public async Task<ObjetivosEstrategico> CreateAsync(MacroObjetivoEstrategicoCreateDto dto, string actorId)
        {
            ValidateActor(actorId);
            ValidateCreate(dto);

            var plan = await _unitOfWork.PlanesNacionales.GetByIdAsync(dto.PlanNacionalId);
            if (plan == null)
            {
                throw new InvalidOperationException("El plan nacional no existe");
            }

            var codigoExistente = await _unitOfWork.ObjetivosEstrategicos.GetByCodigoAsync(dto.PlanNacionalId, dto.Codigo.Trim());
            if (codigoExistente != null)
            {
                throw new InvalidOperationException("Ya existe un objetivo con el mismo código para el plan nacional");
            }

            var entity = new ObjetivosEstrategico
            {
                PlanNacionalId = dto.PlanNacionalId,
                Codigo = dto.Codigo.Trim(),
                Nombre = dto.Nombre.Trim(),
                Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim()
            };

            await _unitOfWork.ObjetivosEstrategicos.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return entity;
        }

        public async Task<ObjetivosEstrategico?> UpdateAsync(int objetivoEstrategicoId, MacroObjetivoEstrategicoUpdateDto dto, string actorId)
        {
            ValidateActor(actorId);

            var entity = await _unitOfWork.ObjetivosEstrategicos.GetByIdAsync(objetivoEstrategicoId);
            if (entity == null)
            {
                return null;
            }

            if (dto.Codigo != null)
            {
                if (string.IsNullOrWhiteSpace(dto.Codigo) || dto.Codigo.Trim().Length > 30)
                {
                    throw new InvalidOperationException("Código inválido");
                }

                var duplicado = await _unitOfWork.ObjetivosEstrategicos.GetByCodigoAsync(entity.PlanNacionalId, dto.Codigo.Trim());
                if (duplicado != null && duplicado.ObjetivoEstrategicoId != entity.ObjetivoEstrategicoId)
                {
                    throw new InvalidOperationException("Ya existe un objetivo con el mismo código para el plan nacional");
                }

                entity.Codigo = dto.Codigo.Trim();
            }

            if (dto.Nombre != null)
            {
                if (string.IsNullOrWhiteSpace(dto.Nombre) || dto.Nombre.Trim().Length > 300)
                {
                    throw new InvalidOperationException("Nombre inválido");
                }

                entity.Nombre = dto.Nombre.Trim();
            }

            if (dto.Descripcion != null)
            {
                if (dto.Descripcion.Length > 600)
                {
                    throw new InvalidOperationException("La descripción supera el máximo de 600 caracteres");
                }

                entity.Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim();
            }

            await _unitOfWork.ObjetivosEstrategicos.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return entity;
        }

        public async Task<bool> DeleteAsync(int objetivoEstrategicoId, string actorId)
        {
            ValidateActor(actorId);

            var entity = await _unitOfWork.ObjetivosEstrategicos.GetByIdAsync(objetivoEstrategicoId);
            if (entity == null)
            {
                return false;
            }

            await _unitOfWork.ObjetivosEstrategicos.RemoveAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        private static void ValidateCreate(MacroObjetivoEstrategicoCreateDto dto)
        {
            if (dto.PlanNacionalId <= 0)
            {
                throw new InvalidOperationException("PlanNacionalId es requerido");
            }

            if (string.IsNullOrWhiteSpace(dto.Codigo) || string.IsNullOrWhiteSpace(dto.Nombre))
            {
                throw new InvalidOperationException("Código y nombre son requeridos");
            }

            if (dto.Codigo.Trim().Length > 30)
            {
                throw new InvalidOperationException("El código supera el máximo de 30 caracteres");
            }

            if (dto.Nombre.Trim().Length > 300)
            {
                throw new InvalidOperationException("El nombre supera el máximo de 300 caracteres");
            }

            if (!string.IsNullOrWhiteSpace(dto.Descripcion) && dto.Descripcion.Length > 600)
            {
                throw new InvalidOperationException("La descripción supera el máximo de 600 caracteres");
            }
        }

        private static void ValidateActor(string actorId)
        {
            if (string.IsNullOrWhiteSpace(actorId))
            {
                throw new InvalidOperationException("No se pudo determinar la identidad del usuario desde los claims");
            }
        }

        private static void NormalizePaging(int pageNumber, int pageSize, out int normalizedPageNumber, out int normalizedPageSize)
        {
            normalizedPageNumber = pageNumber < 1 ? 1 : pageNumber;
            normalizedPageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 100);
        }
    }
}