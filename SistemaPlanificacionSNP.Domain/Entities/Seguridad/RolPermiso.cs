namespace SistemaPlanificacionSNP.Domain.Entities.Seguridad
{
    /// <summary>
    /// Entidad que representa el permiso de acceso a una pantalla por rol
    /// </summary>
    public class RolPermiso
    {
        public int RolPermisoId { get; set; }
        public int RolId { get; set; }
        public int PantallaId { get; set; }
        public bool Lectura { get; set; } = true;
        public bool Creacion { get; set; }
        public bool Edicion { get; set; }
        public bool Eliminacion { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        // Navegaciones
        public Rol Rol { get; set; } = null!;
        public Pantalla Pantalla { get; set; } = null!;
    }
}
