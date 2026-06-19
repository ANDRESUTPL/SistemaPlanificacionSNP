namespace SistemaPlanificacionSNP.Domain.Entities.Parametrizacion
{
    /// <summary>
    /// Entidad que representa un catálogo del sistema
    /// </summary>
    public class Catalogo
    {
        public int CatalogoId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        // Relaciones
        public ICollection<ItemCatalogo> Items { get; set; } = new List<ItemCatalogo>();
    }
}
