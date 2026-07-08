namespace SistemaPlanificacionSNP.Infrastructure.DTOs
{
    public class RevisioneDto
    {
        public int RevisionId { get; set; }
        public string CodigoRevision { get; set; } = null!;
        public string Modulo { get; set; } = null!;
        public string Estado { get; set; } = null!;
        public DateTime FechaRevision { get; set; }
        public string? Observaciones { get; set; }
        public List<AuditoriaDto> Auditorias { get; set; } = new();
    }

    public class RevisioneCreateDto
    {
        public string CodigoRevision { get; set; } = null!;
        public string Modulo { get; set; } = null!;
        public string Estado { get; set; } = null!;
        public DateTime? FechaRevision { get; set; }
        public string? Observaciones { get; set; }
    }

    public class RevisioneUpdateDto
    {
        public string? Modulo { get; set; }
        public string? Estado { get; set; }
        public DateTime? FechaRevision { get; set; }
        public string? Observaciones { get; set; }
    }

    public class AuditoriaDto
    {
        public int AuditoriaId { get; set; }
        public int RevisionId { get; set; }
        public string Tipo { get; set; } = null!;
        public string Resultado { get; set; } = null!;
        public string Responsable { get; set; } = null!;
        public DateTime FechaRegistro { get; set; }
    }

    public class AuditoriaCreateDto
    {
        public int RevisionId { get; set; }
        public string Tipo { get; set; } = null!;
        public string Resultado { get; set; } = null!;
        public DateTime? FechaRegistro { get; set; }
    }

    public class AuditoriaUpdateDto
    {
        public string? Tipo { get; set; }
        public string? Resultado { get; set; }
        public DateTime? FechaRegistro { get; set; }
    }

    public class RevisioneQueryDto
    {
        public string? Estado { get; set; }
        public string? Modulo { get; set; }
        public string? CodigoRevision { get; set; }
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "FechaRevision";
        public string SortDirection { get; set; } = "desc";
    }

    public class AuditoriaQueryDto
    {
        public int? RevisionId { get; set; }
        public string? Tipo { get; set; }
        public string? Resultado { get; set; }
        public string? Responsable { get; set; }
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "FechaRegistro";
        public string SortDirection { get; set; } = "desc";
    }

    public class ControlCalidadDashboardDto
    {
        public int TotalRevisiones { get; set; }
        public int RevisionesPendientes { get; set; }
        public int RevisionesAprobadas { get; set; }
        public int RevisionesRechazadas { get; set; }
        public int TotalAuditorias { get; set; }
        public int AuditoriasConformes { get; set; }
        public int AuditoriasNoConformes { get; set; }
    }
}
