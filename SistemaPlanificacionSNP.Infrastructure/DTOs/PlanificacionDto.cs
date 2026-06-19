namespace SistemaPlanificacionSNP.Infrastructure.DTOs
{
    /// <summary>
    /// DTO para Plan Estratégico Institucional (con jerarquía completa)
    /// </summary>
    public class PlanEstrategicoInstitucionalDto
    {
        public int PlanEstrategicoInstitucionalId { get; set; }
        public int EntidadPublicaId { get; set; }
        public string Nombre { get; set; } = null!;
        public string Descripcion { get; set; } = null!;
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public string Estado { get; set; } = null!;
        public List<ObjetivoEstrategicoDto> ObjetivosEstrategicos { get; set; } = new();
    }

    /// <summary>
    /// DTO para Objetivo Estratégico
    /// </summary>
    public class ObjetivoEstrategicoDto
    {
        public int ObjetivoEstrategicoId { get; set; }
        public int PlanEstrategicoInstitucionalId { get; set; }
        public string Nombre { get; set; } = null!;
        public string Descripcion { get; set; } = null!;
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public List<ProgramaPresupuestarioDto> ProgramasPresupuestarios { get; set; } = new();
    }

    /// <summary>
    /// DTO para Programa Presupuestario
    /// </summary>
    public class ProgramaPresupuestarioDto
    {
        public int ProgramaPresupuestarioId { get; set; }
        public int ObjetivoEstrategicoId { get; set; }
        public string Nombre { get; set; } = null!;
        public string Descripcion { get; set; } = null!;
        public decimal Presupuesto { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public string Estado { get; set; } = null!;
        public List<MatrizIndicadorDto> MatricesIndicadores { get; set; } = new();
        public List<ProyectoInversionDto> ProyectosInversion { get; set; } = new();
    }

    /// <summary>
    /// DTO para Matriz de Indicadores
    /// </summary>
    public class MatrizIndicadorDto
    {
        public int MatrizIndicadorId { get; set; }
        public int ProgramaPresupuestarioId { get; set; }
        public string Nombre { get; set; } = null!;
        public string Formula { get; set; } = null!;
        public string Unidad { get; set; } = null!;
        public decimal Meta { get; set; }
        public List<MetaTerritorialDto> MetasTerritorial { get; set; } = new();
    }

    /// <summary>
    /// DTO para Meta Territorial
    /// </summary>
    public class MetaTerritorialDto
    {
        public int MetaTerritorialId { get; set; }
        public int MatrizIndicadorId { get; set; }
        public string CodigoGeografico { get; set; } = null!;
        public string NombreTerritorio { get; set; } = null!;
        public decimal MetaValor { get; set; }
        public decimal ValorAlcanzado { get; set; }
        public DateTime FechaActualizacion { get; set; }
    }

    /// <summary>
    /// DTO para Proyecto de Inversión
    /// </summary>
    public class ProyectoInversionDto
    {
        public int ProyectoInversionId { get; set; }
        public int ProgramaPresupuestarioId { get; set; }
        public string Nombre { get; set; } = null!;
        public string Descripcion { get; set; } = null!;
        public decimal InversionTotal { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public string Estado { get; set; } = null!;
        public decimal AvanceFisico { get; set; }
        public decimal AvanceFinanciero { get; set; }
    }

    /// <summary>
    /// DTO para crear/actualizar Plan Estratégico
    /// </summary>
    public class PlanCreateUpdateDto
    {
        public string Nombre { get; set; } = null!;
        public string Descripcion { get; set; } = null!;
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
    }

    /// <summary>
    /// DTO para dashboard de planificación (resumen)
    /// </summary>
    public class PlanificacionDashboardDto
    {
        public int TotalPlanesActivos { get; set; }
        public int TotalObjetivosEstrategicos { get; set; }
        public int TotalProgramasPresupuestarios { get; set; }
        public int TotalProyectosInversion { get; set; }
        public decimal InversionTotalAnualizada { get; set; }
        public decimal AvancePromedioPlanesActivos { get; set; }
        public List<PlanEstrategicoInstitucionalDto> PlanesProximo { get; set; } = new();
    }
}
