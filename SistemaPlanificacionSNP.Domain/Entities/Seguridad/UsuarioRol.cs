namespace SistemaPlanificacionSNP.Domain.Entities.Seguridad
{
    /// <summary>
    /// Entidad que representa la relación entre Usuario y Rol
    /// </summary>
    public class UsuarioRol
    {
        public int UsuarioRolId { get; set; }
        public int UsuarioId { get; set; }
        public int RolId { get; set; }
        public DateTime FechaAsignacion { get; set; } = DateTime.UtcNow;

        // Navegaciones
        public Usuario Usuario { get; set; } = null!;
        public Rol Rol { get; set; } = null!;
    }
}
