using AutoMapper;
using System.Linq;
using SistemaPlanificacionSNP.Domain.Entities.Seguridad;
using SistemaPlanificacionSNP.Domain.Entities.Parametrizacion;
using SistemaPlanificacionSNP.Infrastructure.DTOs;
using SistemaPlanificacionSNP.Domain.Entities.PlanificacionInstitucional;
using SistemaPlanificacionSNP.Domain.Entities.ControlCalidad;
using SistemaPlanificacionSNP.Domain.Entities.MacroPlanificacion;
using PlanificacionInstitucional = SistemaPlanificacionSNP.Domain.Entities.PlanificacionInstitucional;

namespace SistemaPlanificacionSNP.Infrastructure.Mapping
{
    /// <summary>
    /// Configuración de AutoMapper para todas las entidades
    /// </summary>
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // ==================== USUARIO ====================           
            CreateMap<UsuarioDto, Usuario>()
                .ForMember(dest => dest.UsuarioRols, opt => opt.Ignore());

            CreateMap<Usuario, UsuarioDto>()
                .ForMember(dest => dest.Roles, opt => opt.MapFrom(src => src.UsuarioRols.Select(ur => ur.Rol)));

            CreateMap<UsuarioRol, RolDto>()
                .ConvertUsing((src, _, context) => context.Mapper.Map<RolDto>(src.Rol));

            CreateMap<UsuarioCreateDto, Usuario>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore()); // Se asigna en el controller

            CreateMap<UsuarioUpdateDto, Usuario>()
                .ForMember(dest => dest.UsuarioId, opt => opt.Ignore())
                .ForMember(dest => dest.NombreUsuario, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore());

            // ==================== ROL ====================
            CreateMap<Rol, RolDto>()
                .ForMember(dest => dest.Permisos, opt => opt.MapFrom(src => src.RolPermisos));

            CreateMap<RolCreateUpdateDto, Rol>()
                .ForMember(dest => dest.RolId, opt => opt.Ignore())
                .ForMember(dest => dest.RolPermisos, opt => opt.Ignore());

            // ==================== ROL PERMISO ====================
            CreateMap<RolPermiso, RolPermisoDto>();
            CreateMap<RolPermiso, PermisoDto>()
                .ForMember(dest => dest.PermisoId, opt => opt.MapFrom(src => src.RolPermisoId))
                .ForMember(dest => dest.CodigoPermiso, opt => opt.MapFrom(src => $"PANTALLA_{src.PantallaId}"))
                .ForMember(dest => dest.NombrePantalla, opt => opt.MapFrom(src => src.Pantalla != null ? src.Pantalla.Nombre : string.Empty));

            // ==================== PANTALLA Y MENÚ ====================
            CreateMap<Pantalla, MenuPermisoDto>()
                .ForMember(dest => dest.RolPermisos, opt => opt.MapFrom(src => src.RolPermisos));

            // ==================== PLAN ESTRATÉGICO ====================
            CreateMap<PlanEstrategicoInstitucional, PlanEstrategicoInstitucionalDto>()
                .ForMember(dest => dest.PlanEstrategicoInstitucionalId, opt => opt.MapFrom(src => src.PeiId))
                .ForMember(dest => dest.ObjetivosEstrategicos, opt => opt.MapFrom(src => src.ObjetivosEstrategicos));

            CreateMap<PlanCreateUpdateDto, PlanEstrategicoInstitucional>()
                .ForMember(dest => dest.PeiId, opt => opt.Ignore())
                .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore())
                .ForMember(dest => dest.ObjetivosEstrategicos, opt => opt.Ignore());

            // ==================== PLANIFICACION DB-FIRST ====================
            CreateMap<PlanesEstrategico, PlanesEstrategicoReadDto>();

            CreateMap<PlanesEstrategico, PlanesEstrategicoDetailDto>()
                .ForMember(dest => dest.Proyectos, opt => opt.MapFrom(src => src.ProyectosInversions));

            CreateMap<PlanesEstrategicoCreateDto, PlanesEstrategico>()
                .ForMember(dest => dest.PlanEstrategicoId, opt => opt.Ignore())
                .ForMember(dest => dest.ProyectosInversions, opt => opt.Ignore())
                .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore());

            CreateMap<ProyectosInversion, ProyectosInversionReadDto>();

            CreateMap<ProyectosInversion, ProyectosInversionDetailDto>()
                .ForMember(dest => dest.EntidadPlan, opt => opt.MapFrom(src => src.PlanEstrategico.Entidad))
                .ForMember(dest => dest.PeriodoInicioPlan, opt => opt.MapFrom(src => src.PlanEstrategico.PeriodoInicio))
                .ForMember(dest => dest.PeriodoFinPlan, opt => opt.MapFrom(src => src.PlanEstrategico.PeriodoFin));

            CreateMap<ProyectosInversionCreateDto, ProyectosInversion>()
                .ForMember(dest => dest.ProyectoInversionId, opt => opt.Ignore())
                .ForMember(dest => dest.PlanEstrategico, opt => opt.Ignore());

            // ==================== OBJETIVO ESTRATÉGICO ====================
            CreateMap<ObjetivoEstrategico, ObjetivoEstrategicoDto>();

            // ==================== PROGRAMA PRESUPUESTARIO ====================
            CreateMap<ProgramaPresupuestario, ProgramaPresupuestarioDto>();

            // ==================== MATRIZ INDICADOR ====================
            CreateMap<MatrizIndicador, MatrizIndicadorDto>();

            // ==================== META TERRITORIAL ====================
            CreateMap<MetaTerritorial, MetaTerritorialDto>();

            // ==================== PROYECTO INVERSIÓN ====================
            CreateMap<ProyectoInversion, ProyectoInversionDto>();

            // ==================== ENTIDAD PÚBLICA ====================
            CreateMap<EntidadPublica, EntidadPublicaDto>();

            CreateMap<EntidadPublicaCreateUpdateDto, EntidadPublica>()
                .ForMember(dest => dest.EntidadPublicaId, opt => opt.Ignore())
                .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore())
                .ForMember(dest => dest.Activo, opt => opt.Ignore());

            // ==================== CONTROL CALIDAD ====================
            CreateMap<Revisione, RevisioneDto>()
                .ForMember(dest => dest.Auditorias, opt => opt.MapFrom(src => src.Auditoria));

            CreateMap<RevisioneCreateDto, Revisione>()
                .ForMember(dest => dest.RevisionId, opt => opt.Ignore())
                .ForMember(dest => dest.Auditoria, opt => opt.Ignore());

            CreateMap<RevisioneUpdateDto, Revisione>()
                .ForMember(dest => dest.RevisionId, opt => opt.Ignore())
                .ForMember(dest => dest.CodigoRevision, opt => opt.Ignore())
                .ForMember(dest => dest.Auditoria, opt => opt.Ignore());

            CreateMap<Auditoria, AuditoriaDto>();
            CreateMap<AuditoriaCreateDto, Auditoria>()
                .ForMember(dest => dest.AuditoriaId, opt => opt.Ignore())
                .ForMember(dest => dest.Revision, opt => opt.Ignore());
            CreateMap<AuditoriaUpdateDto, Auditoria>()
                .ForMember(dest => dest.AuditoriaId, opt => opt.Ignore())
                .ForMember(dest => dest.RevisionId, opt => opt.Ignore())
                .ForMember(dest => dest.Responsable, opt => opt.Ignore())
                .ForMember(dest => dest.Revision, opt => opt.Ignore());

            // ==================== MACRO PLANIFICACION ====================
            CreateMap<PlanesNacionalesDesarrollo, MacroPlanNacionalDto>();

            CreateMap<PlanesNacionalesDesarrollo, MacroPlanNacionalDetalleDto>()
                .ForMember(dest => dest.Objetivos, opt => opt.MapFrom(src => src.ObjetivosEstrategicos));

            CreateMap<MacroPlanNacionalCreateDto, PlanesNacionalesDesarrollo>()
                .ForMember(dest => dest.PlanNacionalId, opt => opt.Ignore())
                .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore())
                .ForMember(dest => dest.ObjetivosEstrategicos, opt => opt.Ignore());

            CreateMap<MacroPlanNacionalUpdateDto, PlanesNacionalesDesarrollo>()
                .ForMember(dest => dest.PlanNacionalId, opt => opt.Ignore())
                .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore())
                .ForMember(dest => dest.ObjetivosEstrategicos, opt => opt.Ignore())
                .ForAllMembers(opt =>
                    opt.Condition((_, _, srcMember) => srcMember != null));

            CreateMap<ObjetivosEstrategico, MacroObjetivoEstrategicoDto>();

            CreateMap<MacroObjetivoEstrategicoCreateDto, ObjetivosEstrategico>()
                .ForMember(dest => dest.ObjetivoEstrategicoId, opt => opt.Ignore())
                .ForMember(dest => dest.PlanNacional, opt => opt.Ignore());

            CreateMap<MacroObjetivoEstrategicoUpdateDto, ObjetivosEstrategico>()
                .ForMember(dest => dest.ObjetivoEstrategicoId, opt => opt.Ignore())
                .ForMember(dest => dest.PlanNacionalId, opt => opt.Ignore())
                .ForMember(dest => dest.PlanNacional, opt => opt.Ignore())
                .ForAllMembers(opt =>
                    opt.Condition((_, _, srcMember) => srcMember != null));

			// ==================== PARAMETRIZACIÓN (AGREGADOS) ====================
			CreateMap<Catalogo, CatalogoDto>();
			CreateMap<CatalogoCreateDto, Catalogo>()
				.ForMember(dest => dest.CatalogoId, opt => opt.Ignore())
				.ForMember(dest => dest.FechaCreacion, opt => opt.Ignore())
				.ForMember(dest => dest.Activo, opt => opt.Ignore());
			CreateMap<CatalogoUpdateDto, Catalogo>();

			CreateMap<ItemCatalogo, ItemCatalogoDto>();
			CreateMap<ItemCatalogoCreateDto, ItemCatalogo>()
				.ForMember(dest => dest.ItemCatalogoId, opt => opt.Ignore())
				.ForMember(dest => dest.FechaCreacion, opt => opt.Ignore())
				.ForMember(dest => dest.Activo, opt => opt.Ignore());
			CreateMap<ItemCatalogoUpdateDto, ItemCatalogo>();

			CreateMap<PeriodoPlanificacion, PeriodoPlanificacionDto>();
			CreateMap<PeriodoPlanificacionCreateUpdateDto, PeriodoPlanificacion>()
				.ForMember(dest => dest.PeriodoPlanificacionId, opt => opt.Ignore())
				.ForMember(dest => dest.FechaCreacion, opt => opt.Ignore());

			CreateMap<MacroPlanNacionalResumenDto, MacroPlanNacionalResumenApiDto>().ReverseMap();
		}
    }
}
