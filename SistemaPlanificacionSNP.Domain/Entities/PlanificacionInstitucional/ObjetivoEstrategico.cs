namespace SistemaPlanificacionSNP.Domain.Entities.PlanificacionInstitucional
{
    /// <summary>
    /// Entidad que representa un Objetivo Estratégico Institucional (OEI)
    /// </summary>
    public class ObjetivoEstrategico
    {
        public int OeiId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public int PeiId { get; set; }
        public int? PndId { get; set; }
        public int Orden { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        // Navegaciones
        public PlanEstrategicoInstitucional PlanEstrategico { get; set; } = null!;
        public MacroPlanificacion.PlanNacionalDesarrollo? PlanNacional { get; set; }
        public ICollection<ProgramaPresupuestario> ProgramasPresupuestarios { get; set; } = new List<ProgramaPresupuestario>();
    }
}
