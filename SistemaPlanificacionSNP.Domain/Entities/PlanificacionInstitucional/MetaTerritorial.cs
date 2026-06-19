namespace SistemaPlanificacionSNP.Domain.Entities.PlanificacionInstitucional
{
    /// <summary>
    /// Entidad que representa una Meta Territorial
    /// </summary>
    public class MetaTerritorial
    {
        public int MetaTerritorialId { get; set; }
        public int MatrizIndicadorId { get; set; }
        public string Territorio { get; set; } = string.Empty; // Departamento, Provincia, etc.
        public decimal MetaFisica { get; set; }
        public decimal MetaFinanciera { get; set; }
        public int Orden { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        // Navegaciones
        public MatrizIndicador MatrizIndicador { get; set; } = null!;
    }
}
