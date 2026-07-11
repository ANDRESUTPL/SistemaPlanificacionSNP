namespace SistemaPlanificacionSNP.Infrastructure.DTOs
{
    public class PlanesEstrategicoQueryDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Estado { get; set; }
        public string? Entidad { get; set; }
        public int? PeriodoInicio { get; set; }
        public int? PeriodoFin { get; set; }
        public string SortBy { get; set; } = "PlanEstrategicoId";
        public string SortDirection { get; set; } = "asc";
    }

    public class PlanesEstrategicoReadDto
    {
        public int PlanEstrategicoId { get; set; }
        public string Entidad { get; set; } = string.Empty;
        public int PeriodoInicio { get; set; }
        public int PeriodoFin { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
    }

    public class PlanesEstrategicoDetailDto : PlanesEstrategicoReadDto
    {
        public List<ProyectosInversionReadDto> Proyectos { get; set; } = new();
    }

    public class PlanesEstrategicoCreateDto
    {
        public string Entidad { get; set; } = string.Empty;
        public int PeriodoInicio { get; set; }
        public int PeriodoFin { get; set; }
        public string Estado { get; set; } = "Borrador";
    }

    public class PlanesEstrategicoUpdateDto
    {
        public string? Entidad { get; set; }
        public int? PeriodoInicio { get; set; }
        public int? PeriodoFin { get; set; }
        public string? Estado { get; set; }
    }

    public class ProyectosInversionQueryDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int? PlanEstrategicoId { get; set; }
        public string? Estado { get; set; }
        public string? CodigoProyecto { get; set; }
        public string? Nombre { get; set; }
        public string SortBy { get; set; } = "ProyectoInversionId";
        public string SortDirection { get; set; } = "asc";
    }

    public class ProyectosInversionReadDto
    {
        public int ProyectoInversionId { get; set; }
        public int PlanEstrategicoId { get; set; }
        public string CodigoProyecto { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public string Estado { get; set; } = string.Empty;
    }

    public class ProyectosInversionDetailDto : ProyectosInversionReadDto
    {
        public string EntidadPlan { get; set; } = string.Empty;
        public int PeriodoInicioPlan { get; set; }
        public int PeriodoFinPlan { get; set; }
    }

    public class ProyectosInversionCreateDto
    {
        public int PlanEstrategicoId { get; set; }
        public string CodigoProyecto { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public string Estado { get; set; } = "Formulacion";
    }

    public class ProyectosInversionUpdateDto
    {
        public string? Nombre { get; set; }
        public decimal? Monto { get; set; }
        public string? Estado { get; set; }
    }

    public class PlanificacionInstitucionalDashboardDbFirstDto
    {
        public int TotalPlanes { get; set; }
        public int TotalPlanesActivos { get; set; }
        public int TotalProyectos { get; set; }
        public int TotalProyectosActivos { get; set; }
        public decimal MontoTotalProyectosActivos { get; set; }
    }
}
