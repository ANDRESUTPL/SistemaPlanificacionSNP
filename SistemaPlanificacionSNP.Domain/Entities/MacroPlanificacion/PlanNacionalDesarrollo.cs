namespace SistemaPlanificacionSNP.Domain.Entities.MacroPlanificacion
{
    /// <summary>
    /// Entidad que representa el Plan Nacional de Desarrollo (PND)
    /// </summary>
    public class PlanNacionalDesarrollo
    {
        public int PndId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public int OdsId { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        // Navegaciones
        public ObjetivoDesarrolloSostenible Ods { get; set; } = null!;
    }
}
