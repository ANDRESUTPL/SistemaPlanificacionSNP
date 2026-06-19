using SistemaPlanificacionSNP.Domain.Entities.PlanificacionInstitucional;

namespace SistemaPlanificacionSNP.Domain.Entities.Parametrizacion
{
    /// <summary>
    /// Entidad que representa una entidad pública
    /// </summary>
    public class EntidadPublica
    {
        public int EntidadPublicaId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Sigla { get; set; } = string.Empty;
        public string Mision { get; set; } = string.Empty;
        public int PeriodoPlanificacionId { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        // Navegaciones
        public PeriodoPlanificacion PeriodoPlanificacion { get; set; } = null!;
        public ICollection<PlanEstrategicoInstitucional> PlanesEstrategicos { get; set; } = new List<PlanEstrategicoInstitucional>();
    }
}
