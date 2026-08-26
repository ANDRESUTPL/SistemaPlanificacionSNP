namespace SistemaPlanificacionSNP.Web.Models
{
    public class DashboardEstadoConteoDto
    {
        public string Estado { get; set; } = string.Empty;
        public int Cantidad { get; set; }
    }

    public class DashboardAvanceProyectoDto
    {
        public string CodigoProyecto { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public decimal AvanceFisico { get; set; }
        public decimal AvanceFinanciero { get; set; }
    }

    public class DashboardPlanProximoDto
    {
        public int PlanEstrategicoId { get; set; }
        public string Entidad { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public int PeriodoInicio { get; set; }
        public int PeriodoFin { get; set; }
        public int CantidadProyectos { get; set; }
    }

    public class DashboardResumenDto
    {
        public int TotalPlanesActivos { get; set; }
        public int TotalProyectosActivos { get; set; }
        public int TotalProyectos { get; set; }
        public decimal InversionTotal { get; set; }
        public List<DashboardEstadoConteoDto> EstadoDistribucion { get; set; } = new();
        public List<DashboardAvanceProyectoDto> AvanceProyectos { get; set; } = new();
        public List<DashboardPlanProximoDto> PlanesProximo { get; set; } = new();
    }
}
