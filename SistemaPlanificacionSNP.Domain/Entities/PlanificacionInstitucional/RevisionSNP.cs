namespace SistemaPlanificacionSNP.Domain.Entities.PlanificacionInstitucional
{
    /// <summary>
    /// Entidad que representa una Revisión del PEI por parte de SNP
    /// </summary>
    public class RevisionSNP
    {
        public int RevisionId { get; set; }
        public int PeiId { get; set; }
        public string Estado { get; set; } = "Pendiente"; // Pendiente, Aprobado, Rechazado
        public string? Comentarios { get; set; }
        public int? UsuarioRevisor { get; set; }
        public DateTime FechaRevision { get; set; } = DateTime.UtcNow;
        public bool Activo { get; set; } = true;

        // Navegaciones
        public PlanEstrategicoInstitucional PlanEstrategico { get; set; } = null!;
        public Seguridad.Usuario? Revisor { get; set; }
    }
}
