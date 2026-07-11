namespace SistemaPlanificacionSNP.Infrastructure.DTOs
{
    public class MacroPlanNacionalDto
    {
        public int PlanNacionalId { get; set; }
        public string Nombre { get; set; } = null!;
        public int PeriodoInicio { get; set; }
        public int PeriodoFin { get; set; }
        public string Estado { get; set; } = null!;
        public DateTime FechaCreacion { get; set; }
    }

    public class MacroPlanNacionalDetalleDto : MacroPlanNacionalDto
    {
        public List<MacroObjetivoEstrategicoDto> Objetivos { get; set; } = new();
    }

    public class MacroPlanNacionalCreateDto
    {
        public string Nombre { get; set; } = null!;
        public int PeriodoInicio { get; set; }
        public int PeriodoFin { get; set; }
        public string Estado { get; set; } = null!;
    }

    public class MacroPlanNacionalUpdateDto
    {
        public string? Nombre { get; set; }
        public int? PeriodoInicio { get; set; }
        public int? PeriodoFin { get; set; }
        public string? Estado { get; set; }
    }

    public class MacroPlanNacionalQueryDto
    {
        public string? Estado { get; set; }
        public int? PeriodoInicio { get; set; }
        public int? PeriodoFin { get; set; }
        public string? Busqueda { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "PlanNacionalId";
        public string SortDirection { get; set; } = "desc";
    }

    public class MacroObjetivoEstrategicoDto
    {
        public int ObjetivoEstrategicoId { get; set; }
        public int PlanNacionalId { get; set; }
        public string Codigo { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public string? Descripcion { get; set; }
    }

    public class MacroObjetivoEstrategicoCreateDto
    {
        public int PlanNacionalId { get; set; }
        public string Codigo { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public string? Descripcion { get; set; }
    }

    public class MacroObjetivoEstrategicoUpdateDto
    {
        public string? Codigo { get; set; }
        public string? Nombre { get; set; }
        public string? Descripcion { get; set; }
    }

    public class MacroObjetivoEstrategicoQueryDto
    {
        public int? PlanNacionalId { get; set; }
        public string? Codigo { get; set; }
        public string? Busqueda { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "ObjetivoEstrategicoId";
        public string SortDirection { get; set; } = "desc";
    }

    public class MacroConteoEstadoDto
    {
        public string Estado { get; set; } = null!;
        public int Total { get; set; }
    }

    public class MacroConteoVigenciaDto
    {
        public int PeriodoInicio { get; set; }
        public int PeriodoFin { get; set; }
        public int TotalPlanes { get; set; }
    }

    public class MacroPlanNacionalResumenDto
    {
        public int TotalPlanes { get; set; }
        public int TotalObjetivos { get; set; }
        public List<MacroConteoEstadoDto> PlanesPorEstado { get; set; } = new();
        public List<MacroConteoVigenciaDto> PlanesPorVigencia { get; set; } = new();
    }
}