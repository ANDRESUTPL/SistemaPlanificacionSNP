namespace SistemaPlanificacionSNP.Domain.Entities.PlanificacionInstitucional;

public partial class RespaldoEjecucion
{
    public int RespaldoEjecucionId { get; set; }

    public int ProyectoInversionId { get; set; }

    public string NombreArchivo { get; set; } = null!;

    public string RutaArchivo { get; set; } = null!;

    public string TipoContenido { get; set; } = null!;

    public long TamanoBytes { get; set; }

    public DateTime FechaCarga { get; set; }

    public virtual ProyectosInversion ProyectoInversion { get; set; } = null!;
}
