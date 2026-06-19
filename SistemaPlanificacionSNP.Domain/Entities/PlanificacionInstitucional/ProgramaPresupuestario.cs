namespace SistemaPlanificacionSNP.Domain.Entities.PlanificacionInstitucional
{
    /// <summary>
    /// Entidad que representa un Programa Presupuestario
    /// </summary>
    public class ProgramaPresupuestario
    {
        public int ProgramaId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public int OeiId { get; set; }
        public decimal PresupuestoAsignado { get; set; }
        public int Orden { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        // Navegaciones
        public ObjetivoEstrategico ObjetivoEstrategico { get; set; } = null!;
        public ICollection<MatrizIndicador> MatricesIndicadores { get; set; } = new List<MatrizIndicador>();
        public ICollection<ProyectoInversion> ProyectosInversion { get; set; } = new List<ProyectoInversion>();
    }
}
