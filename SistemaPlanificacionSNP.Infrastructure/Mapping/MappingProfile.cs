using AutoMapper;
using SistemaPlanificacionSNP.Domain.Entities.Seguridad;
using SistemaPlanificacionSNP.Domain.Entities.Parametrizacion;
using SistemaPlanificacionSNP.Infrastructure.DTOs;
using SistemaPlanificacionSNP.Domain.Entities.PlanificacionInstitucional;
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
            CreateMap<UsuarioDto, Usuario>();
            CreateMap<Usuario, UsuarioDto>();
            CreateMap<UsuarioCreateDto, Usuario>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore()); // Se asigna en el controller

            CreateMap<UsuarioUpdateDto, Usuario>()
                .ForMember(dest => dest.UsuarioId, opt => opt.Ignore())
                .ForMember(dest => dest.NombreUsuario, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore());

            // ==================== ROL ====================
            CreateMap<Rol, RolDto>()
                .ForMember(dest => dest.Permisos, opt => opt.MapFrom(src => src.RolPermisos.Select(rp => rp.Pantalla)));

            CreateMap<RolCreateUpdateDto, Rol>()
                .ForMember(dest => dest.RolId, opt => opt.Ignore())
                .ForMember(dest => dest.RolPermisos, opt => opt.Ignore());

            // ==================== ROL PERMISO ====================
            CreateMap<RolPermiso, RolPermisoDto>();

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
        }
    }
}
