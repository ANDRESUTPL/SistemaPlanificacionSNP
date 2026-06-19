namespace SistemaPlanificacionSNP.Domain.Entities.Seguridad
{
    /// <summary>
    /// Entidad que registra las transacciones y cambios en el sistema
    /// </summary>
    public class AuditoriaTransaccional
    {
        public int AuditoriaId { get; set; }
        public int UsuarioId { get; set; }
        public string Entidad { get; set; } = string.Empty;
        public string TipoOperacion { get; set; } = string.Empty; // CREATE, UPDATE, DELETE
        public int? IdRegistro { get; set; }
        public string? DatosAnteriores { get; set; }
        public string? DatosNuevos { get; set; }
        public DateTime FechaOperacion { get; set; } = DateTime.UtcNow;
        public string? Descripcion { get; set; }

        // Navegaciones
        public Usuario Usuario { get; set; } = null!;
    }
}
