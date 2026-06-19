namespace SistemaPlanificacionSNP.Domain.Entities.Parametrizacion
{
    /// <summary>
    /// Entidad que representa un período de planificación
    /// </summary>
    public class PeriodoPlanificacion
    {
        public int PeriodoPlanificacionId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        // Relaciones
        public ICollection<EntidadPublica> EntidadesPublicas { get; set; } = new List<EntidadPublica>();
    }
}
