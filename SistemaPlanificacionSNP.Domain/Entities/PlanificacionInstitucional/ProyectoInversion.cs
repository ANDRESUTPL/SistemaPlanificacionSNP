namespace SistemaPlanificacionSNP.Domain.Entities.PlanificacionInstitucional
{
    /// <summary>
    /// Entidad que representa un Proyecto de Inversión
    /// </summary>
    public class ProyectoInversion
    {
        public int ProyectoId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public int ProgramaId { get; set; }
        public decimal CostoTotal { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public string Estado { get; set; } = "Formulacion"; // Formulacion, Ejecucion, Cierre
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        // Navegaciones
        public ProgramaPresupuestario ProgramaPresupuestario { get; set; } = null!;
    }
}
