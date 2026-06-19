namespace SistemaPlanificacionSNP.Domain.Entities.Parametrizacion
{
    /// <summary>
    /// Entidad que representa un ítem dentro de un catálogo
    /// </summary>
    public class ItemCatalogo
    {
        public int ItemCatalogoId { get; set; }
        public int CatalogoId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public int Orden { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        // Navegaciones
        public Catalogo Catalogo { get; set; } = null!;
    }
}
