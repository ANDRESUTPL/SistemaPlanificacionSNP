namespace SistemaPlanificacionSNP.Domain.Entities.PlanificacionInstitucional
{
    /// <summary>
    /// Entidad que representa el Plan Estratégico Institucional (PEI)
    /// </summary>
    public class PlanEstrategicoInstitucional
    {
        public int PeiId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public int EntidadPublicaId { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public string Estado { get; set; } = "Borrador"; // Borrador, Enviado, Aprobado
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        // Navegaciones
        public Parametrizacion.EntidadPublica EntidadPublica { get; set; } = null!;
        public ICollection<ObjetivoEstrategico> ObjetivosEstrategicos { get; set; } = new List<ObjetivoEstrategico>();
        public ICollection<RevisionSNP> Revisiones { get; set; } = new List<RevisionSNP>();
    }
}
