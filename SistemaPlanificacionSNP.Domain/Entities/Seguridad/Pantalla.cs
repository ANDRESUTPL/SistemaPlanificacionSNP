namespace SistemaPlanificacionSNP.Domain.Entities.Seguridad
{
    /// <summary>
    /// Entidad que representa una pantalla del sistema
    /// </summary>
    public class Pantalla
    {
        public int PantallaId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Ruta { get; set; } = string.Empty;
        public string Icono { get; set; } = string.Empty;
        public int? PantallaPadrId { get; set; }
        public int Orden { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        // Relaciones
        public Pantalla? PantallaPadre { get; set; }
        public ICollection<Pantalla> PantallasHijas { get; set; } = new List<Pantalla>();
        public ICollection<RolPermiso> RolPermisos { get; set; } = new List<RolPermiso>();
    }
}
