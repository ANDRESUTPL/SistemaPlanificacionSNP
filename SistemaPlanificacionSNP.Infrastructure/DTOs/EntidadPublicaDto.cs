namespace SistemaPlanificacionSNP.Infrastructure.DTOs
{
    /// <summary>
    /// DTO para Entidad Pública
    /// </summary>
    public class EntidadPublicaDto
    {
        public int EntidadPublicaId { get; set; }
        public string Nombre { get; set; } = null!;
        public string Sigla { get; set; } = null!;
        public string Tipo { get; set; } = null!;
        public string NivelGobierno { get; set; } = null!;
        public bool Activa { get; set; }
        public DateTime FechaCreacion { get; set; }
    }

    /// <summary>
    /// DTO para crear/actualizar Entidad Pública
    /// </summary>
    public class EntidadPublicaCreateUpdateDto
    {
        public string Nombre { get; set; } = null!;
        public string Sigla { get; set; } = null!;
        public string Tipo { get; set; } = null!;
        public string NivelGobierno { get; set; } = null!;
    }
}
