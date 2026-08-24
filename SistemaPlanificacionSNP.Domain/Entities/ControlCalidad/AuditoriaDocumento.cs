namespace SistemaPlanificacionSNP.Domain.Entities.ControlCalidad;

public partial class AuditoriaDocumento
{
    public int AuditoriaDocumentoId { get; set; }

    public int AuditoriaId { get; set; }

    public string NombreArchivo { get; set; } = null!;

    public string RutaArchivo { get; set; } = null!;

    public string TipoContenido { get; set; } = null!;

    public long TamanoBytes { get; set; }

    public DateTime FechaCarga { get; set; }

    public virtual Auditoria Auditoria { get; set; } = null!;
}