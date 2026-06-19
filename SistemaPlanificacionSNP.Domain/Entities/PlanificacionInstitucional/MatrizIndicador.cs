namespace SistemaPlanificacionSNP.Domain.Entities.PlanificacionInstitucional
{
    /// <summary>
    /// Entidad que representa una Matriz de Indicador
    /// </summary>
    public class MatrizIndicador
    {
        public int MatrizIndicadorId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public int ProgramaId { get; set; }
        public string TipoIndicador { get; set; } = string.Empty; // Cuantitativo, Cualitativo, Mixto
        public string Unidad { get; set; } = string.Empty;
        public decimal ValorBase { get; set; }
        public decimal ValorMeta { get; set; }
        public int Orden { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        // Navegaciones
        public ProgramaPresupuestario ProgramaPresupuestario { get; set; } = null!;
        public ICollection<MetaTerritorial> MetasTerritorial { get; set; } = new List<MetaTerritorial>();
    }
}
