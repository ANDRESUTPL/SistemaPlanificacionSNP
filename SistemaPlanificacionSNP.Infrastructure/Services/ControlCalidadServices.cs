using SistemaPlanificacionSNP.Domain.Entities.ControlCalidad;
using SistemaPlanificacionSNP.Infrastructure.DTOs;
using SistemaPlanificacionSNP.Infrastructure.UnitOfWork;

namespace SistemaPlanificacionSNP.Infrastructure.Services
{
    public interface IRevisioneControlCalidadService
    {
        Task<(List<Revisione> Items, int Total)> GetPagedAsync(RevisioneQueryDto query);
        Task<Revisione?> GetByIdAsync(int revisionId, bool includeAuditorias = false);
        Task<Revisione?> GetByCodigoAsync(string codigoRevision);
        Task<Revisione> CreateAsync(RevisioneCreateDto dto);
        Task<Revisione?> UpdateAsync(int revisionId, RevisioneUpdateDto dto);
        Task<bool> DeleteAsync(int revisionId);
        Task<ControlCalidadDashboardDto> GetDashboardAsync();
    }

    public interface IAuditoriaControlCalidadService
    {
        Task<(List<Auditoria> Items, int Total)> GetPagedAsync(AuditoriaQueryDto query);
        Task<Auditoria?> GetByIdAsync(int auditoriaId);
        Task<List<Auditoria>> GetByRevisionIdAsync(int revisionId);
        Task<Auditoria> CreateAsync(AuditoriaCreateDto dto, string responsable);
        Task<Auditoria?> UpdateAsync(int auditoriaId, AuditoriaUpdateDto dto);
        Task<bool> DeleteAsync(int auditoriaId);
    }

    public class RevisioneControlCalidadService : IRevisioneControlCalidadService
    {
        private static readonly HashSet<string> EstadosPermitidos = new(StringComparer.OrdinalIgnoreCase)
        {
            "Pendiente", "Aprobada", "Rechazada"
        };

        private readonly IControlCalidadUnitOfWork _unitOfWork;

        public RevisioneControlCalidadService(IControlCalidadUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<(List<Revisione> Items, int Total)> GetPagedAsync(RevisioneQueryDto query)
        {
            NormalizePaging(query.PageNumber, query.PageSize, out var pageNumber, out var pageSize);
            query.PageNumber = pageNumber;
            query.PageSize = pageSize;

            var items = await _unitOfWork.Revisiones.GetPagedAsync(query);
            var total = await _unitOfWork.Revisiones.CountFilteredAsync(query);
            return (items, total);
        }

        public async Task<Revisione?> GetByIdAsync(int revisionId, bool includeAuditorias = false)
        {
            return includeAuditorias
                ? await _unitOfWork.Revisiones.GetByIdWithAuditoriasAsync(revisionId)
                : await _unitOfWork.Revisiones.GetByIdAsync(revisionId);
        }

        public async Task<Revisione?> GetByCodigoAsync(string codigoRevision)
        {
            return await _unitOfWork.Revisiones.GetByCodigoAsync(codigoRevision);
        }

        public async Task<Revisione> CreateAsync(RevisioneCreateDto dto)
        {
            ValidateCreate(dto);

            var alreadyExists = await _unitOfWork.Revisiones.GetByCodigoAsync(dto.CodigoRevision.Trim());
            if (alreadyExists != null)
            {
                throw new InvalidOperationException("El código de revisión ya existe");
            }

            var revisione = new Revisione
            {
                CodigoRevision = dto.CodigoRevision.Trim(),
                Modulo = dto.Modulo.Trim(),
                Estado = dto.Estado.Trim(),
                FechaRevision = dto.FechaRevision?.ToUniversalTime() ?? DateTime.UtcNow,
                Observaciones = string.IsNullOrWhiteSpace(dto.Observaciones) ? null : dto.Observaciones.Trim()
            };

            await _unitOfWork.Revisiones.AddAsync(revisione);
            await _unitOfWork.SaveChangesAsync();

            return revisione;
        }

        public async Task<Revisione?> UpdateAsync(int revisionId, RevisioneUpdateDto dto)
        {
            var revisione = await _unitOfWork.Revisiones.GetByIdAsync(revisionId);
            if (revisione == null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(dto.Modulo))
            {
                if (dto.Modulo.Length > 100)
                {
                    throw new InvalidOperationException("El módulo supera el máximo de 100 caracteres");
                }
                revisione.Modulo = dto.Modulo.Trim();
            }

            if (!string.IsNullOrWhiteSpace(dto.Estado))
            {
                if (!EstadosPermitidos.Contains(dto.Estado.Trim()))
                {
                    throw new InvalidOperationException("Estado no permitido");
                }
                revisione.Estado = dto.Estado.Trim();
            }

            if (dto.FechaRevision.HasValue)
            {
                if (dto.FechaRevision.Value > DateTime.UtcNow.AddMinutes(5))
                {
                    throw new InvalidOperationException("La fecha de revisión no puede estar en el futuro");
                }
                revisione.FechaRevision = dto.FechaRevision.Value.ToUniversalTime();
            }

            if (dto.Observaciones != null)
            {
                if (dto.Observaciones.Length > 500)
                {
                    throw new InvalidOperationException("Las observaciones superan el máximo de 500 caracteres");
                }
                revisione.Observaciones = string.IsNullOrWhiteSpace(dto.Observaciones) ? null : dto.Observaciones.Trim();
            }

            await _unitOfWork.Revisiones.UpdateAsync(revisione);
            await _unitOfWork.SaveChangesAsync();
            return revisione;
        }

        public async Task<bool> DeleteAsync(int revisionId)
        {
            var revisione = await _unitOfWork.Revisiones.GetByIdWithAuditoriasAsync(revisionId);
            if (revisione == null)
            {
                return false;
            }

            if (revisione.Auditoria.Count > 0)
            {
                throw new InvalidOperationException("No se puede eliminar una revisión con auditorías asociadas");
            }

            await _unitOfWork.Revisiones.RemoveAsync(revisione);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<ControlCalidadDashboardDto> GetDashboardAsync()
        {
            var revisionesTotal = await _unitOfWork.Revisiones.CountFilteredAsync(new RevisioneQueryDto());
            var totalAuditorias = await _unitOfWork.AuditoriasControlCalidad.CountAllAsync();

            return new ControlCalidadDashboardDto
            {
                TotalRevisiones = revisionesTotal,
                RevisionesPendientes = await _unitOfWork.Revisiones.CountByEstadoAsync("Pendiente"),
                RevisionesAprobadas = await _unitOfWork.Revisiones.CountByEstadoAsync("Aprobada"),
                RevisionesRechazadas = await _unitOfWork.Revisiones.CountByEstadoAsync("Rechazada"),
                TotalAuditorias = totalAuditorias,
                AuditoriasConformes = await _unitOfWork.AuditoriasControlCalidad.CountByResultadoAsync("Conforme"),
                AuditoriasNoConformes = await _unitOfWork.AuditoriasControlCalidad.CountByResultadoAsync("No Conforme")
            };
        }

        private static void ValidateCreate(RevisioneCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CodigoRevision) || string.IsNullOrWhiteSpace(dto.Modulo) || string.IsNullOrWhiteSpace(dto.Estado))
            {
                throw new InvalidOperationException("Código, módulo y estado son requeridos");
            }

            if (dto.CodigoRevision.Length > 40)
            {
                throw new InvalidOperationException("El código de revisión supera el máximo de 40 caracteres");
            }

            if (dto.Modulo.Length > 100)
            {
                throw new InvalidOperationException("El módulo supera el máximo de 100 caracteres");
            }

            if (!EstadosPermitidos.Contains(dto.Estado.Trim()))
            {
                throw new InvalidOperationException("Estado no permitido");
            }

            if (!string.IsNullOrWhiteSpace(dto.Observaciones) && dto.Observaciones.Length > 500)
            {
                throw new InvalidOperationException("Las observaciones superan el máximo de 500 caracteres");
            }

            if (dto.FechaRevision.HasValue && dto.FechaRevision.Value > DateTime.UtcNow.AddMinutes(5))
            {
                throw new InvalidOperationException("La fecha de revisión no puede estar en el futuro");
            }
        }

        private static void NormalizePaging(int pageNumber, int pageSize, out int normalizedPageNumber, out int normalizedPageSize)
        {
            normalizedPageNumber = pageNumber < 1 ? 1 : pageNumber;
            normalizedPageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 100);
        }
    }

    public class AuditoriaControlCalidadService : IAuditoriaControlCalidadService
    {
        private static readonly HashSet<string> TiposPermitidos = new(StringComparer.OrdinalIgnoreCase)
        {
            "Interna", "Externa", "Seguimiento", "Cumplimiento"
        };

        private static readonly HashSet<string> ResultadosPermitidos = new(StringComparer.OrdinalIgnoreCase)
        {
            "Conforme", "No Conforme", "Observado"
        };

        private readonly IControlCalidadUnitOfWork _unitOfWork;

        public AuditoriaControlCalidadService(IControlCalidadUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<(List<Auditoria> Items, int Total)> GetPagedAsync(AuditoriaQueryDto query)
        {
            NormalizePaging(query.PageNumber, query.PageSize, out var pageNumber, out var pageSize);
            query.PageNumber = pageNumber;
            query.PageSize = pageSize;

            var items = await _unitOfWork.AuditoriasControlCalidad.GetPagedAsync(query);
            var total = await _unitOfWork.AuditoriasControlCalidad.CountFilteredAsync(query);
            return (items, total);
        }

        public async Task<Auditoria?> GetByIdAsync(int auditoriaId)
        {
            return await _unitOfWork.AuditoriasControlCalidad.GetByIdAsync(auditoriaId);
        }

        public async Task<List<Auditoria>> GetByRevisionIdAsync(int revisionId)
        {
            return await _unitOfWork.AuditoriasControlCalidad.GetByRevisionIdAsync(revisionId);
        }

        public async Task<Auditoria> CreateAsync(AuditoriaCreateDto dto, string responsable)
        {
            ValidateCreate(dto, responsable);

            var revision = await _unitOfWork.Revisiones.GetByIdAsync(dto.RevisionId);
            if (revision == null)
            {
                throw new InvalidOperationException("La revisión asociada no existe");
            }

            var auditoria = new Auditoria
            {
                RevisionId = dto.RevisionId,
                Tipo = dto.Tipo.Trim(),
                Resultado = dto.Resultado.Trim(),
                Responsable = responsable,
                FechaRegistro = dto.FechaRegistro?.ToUniversalTime() ?? DateTime.UtcNow
            };

            await _unitOfWork.AuditoriasControlCalidad.AddAsync(auditoria);
            await _unitOfWork.SaveChangesAsync();
            return auditoria;
        }

        public async Task<Auditoria?> UpdateAsync(int auditoriaId, AuditoriaUpdateDto dto)
        {
            var auditoria = await _unitOfWork.AuditoriasControlCalidad.GetByIdAsync(auditoriaId);
            if (auditoria == null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(dto.Tipo))
            {
                if (!TiposPermitidos.Contains(dto.Tipo.Trim()))
                {
                    throw new InvalidOperationException("Tipo de auditoría no permitido");
                }
                auditoria.Tipo = dto.Tipo.Trim();
            }

            if (!string.IsNullOrWhiteSpace(dto.Resultado))
            {
                if (!ResultadosPermitidos.Contains(dto.Resultado.Trim()))
                {
                    throw new InvalidOperationException("Resultado no permitido");
                }
                auditoria.Resultado = dto.Resultado.Trim();
            }

            if (dto.FechaRegistro.HasValue)
            {
                if (dto.FechaRegistro.Value > DateTime.UtcNow.AddMinutes(5))
                {
                    throw new InvalidOperationException("La fecha de registro no puede estar en el futuro");
                }
                auditoria.FechaRegistro = dto.FechaRegistro.Value.ToUniversalTime();
            }

            await _unitOfWork.AuditoriasControlCalidad.UpdateAsync(auditoria);
            await _unitOfWork.SaveChangesAsync();
            return auditoria;
        }

        public async Task<bool> DeleteAsync(int auditoriaId)
        {
            var auditoria = await _unitOfWork.AuditoriasControlCalidad.GetByIdAsync(auditoriaId);
            if (auditoria == null)
            {
                return false;
            }

            await _unitOfWork.AuditoriasControlCalidad.RemoveAsync(auditoria);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        private static void ValidateCreate(AuditoriaCreateDto dto, string responsable)
        {
            if (dto.RevisionId <= 0)
            {
                throw new InvalidOperationException("La revisión es requerida");
            }

            if (string.IsNullOrWhiteSpace(dto.Tipo) || string.IsNullOrWhiteSpace(dto.Resultado))
            {
                throw new InvalidOperationException("Tipo y resultado son requeridos");
            }

            if (string.IsNullOrWhiteSpace(responsable))
            {
                throw new InvalidOperationException("No se pudo determinar el responsable desde los claims");
            }

            if (dto.Tipo.Length > 50)
            {
                throw new InvalidOperationException("El tipo supera el máximo de 50 caracteres");
            }

            if (dto.Resultado.Length > 30)
            {
                throw new InvalidOperationException("El resultado supera el máximo de 30 caracteres");
            }

            if (responsable.Length > 120)
            {
                throw new InvalidOperationException("El responsable supera el máximo de 120 caracteres");
            }

            if (!TiposPermitidos.Contains(dto.Tipo.Trim()))
            {
                throw new InvalidOperationException("Tipo de auditoría no permitido");
            }

            if (!ResultadosPermitidos.Contains(dto.Resultado.Trim()))
            {
                throw new InvalidOperationException("Resultado no permitido");
            }

            if (dto.FechaRegistro.HasValue && dto.FechaRegistro.Value > DateTime.UtcNow.AddMinutes(5))
            {
                throw new InvalidOperationException("La fecha de registro no puede estar en el futuro");
            }
        }

        private static void NormalizePaging(int pageNumber, int pageSize, out int normalizedPageNumber, out int normalizedPageSize)
        {
            normalizedPageNumber = pageNumber < 1 ? 1 : pageNumber;
            normalizedPageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 100);
        }
    }
}
