namespace SistemaPlanificacionSNP.Domain.Entities.MacroPlanificacion
{
    /// <summary>
    /// Entidad que representa un Objetivo de Desarrollo Sostenible (ODS)
    /// </summary>
    public class ObjetivoDesarrolloSostenible
    {
        public int OdsId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        // Relaciones
        public ICollection<PlanNacionalDesarrollo> PlanesNacionales { get; set; } = new List<PlanNacionalDesarrollo>();
    }
}
