using FluentValidation;
using SistemaPlanificacionSNP.Infrastructure.DTOs;

namespace SistemaPlanificacionSNP.PlanificacionInstitucional.Api.Validators
{
    public class PlanesEstrategicoCreateDtoValidator : AbstractValidator<PlanesEstrategicoCreateDto>
    {
        public PlanesEstrategicoCreateDtoValidator()
        {
            RuleFor(x => x.Entidad)
                .NotEmpty().WithMessage("La entidad es requerida")
                .MaximumLength(200).WithMessage("La entidad no puede superar 200 caracteres");

            RuleFor(x => x.PeriodoInicio)
                .InclusiveBetween(1900, 3000).WithMessage("PeriodoInicio fuera de rango");

            RuleFor(x => x.PeriodoFin)
                .InclusiveBetween(1900, 3000).WithMessage("PeriodoFin fuera de rango")
                .GreaterThanOrEqualTo(x => x.PeriodoInicio).WithMessage("PeriodoFin no puede ser menor a PeriodoInicio");

            RuleFor(x => x.Estado)
                .NotEmpty().WithMessage("El estado es requerido")
                .MaximumLength(30).WithMessage("El estado no puede superar 30 caracteres");
        }
    }

    public class PlanesEstrategicoUpdateDtoValidator : AbstractValidator<PlanesEstrategicoUpdateDto>
    {
        public PlanesEstrategicoUpdateDtoValidator()
        {
            RuleFor(x => x.Entidad)
                .MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.Entidad))
                .WithMessage("La entidad no puede superar 200 caracteres");

            RuleFor(x => x.PeriodoInicio)
                .InclusiveBetween(1900, 3000).When(x => x.PeriodoInicio.HasValue)
                .WithMessage("PeriodoInicio fuera de rango");

            RuleFor(x => x.PeriodoFin)
                .InclusiveBetween(1900, 3000).When(x => x.PeriodoFin.HasValue)
                .WithMessage("PeriodoFin fuera de rango");

            RuleFor(x => x)
                .Must(x => !x.PeriodoInicio.HasValue || !x.PeriodoFin.HasValue || x.PeriodoFin.Value >= x.PeriodoInicio.Value)
                .WithMessage("PeriodoFin no puede ser menor a PeriodoInicio");

            RuleFor(x => x.Estado)
                .MaximumLength(30).When(x => !string.IsNullOrWhiteSpace(x.Estado))
                .WithMessage("El estado no puede superar 30 caracteres");
        }
    }

    public class ProyectosInversionCreateDtoValidator : AbstractValidator<ProyectosInversionCreateDto>
    {
        public ProyectosInversionCreateDtoValidator()
        {
            RuleFor(x => x.PlanEstrategicoId)
                .GreaterThan(0).WithMessage("PlanEstrategicoId es requerido");

            RuleFor(x => x.CodigoProyecto)
                .NotEmpty().WithMessage("El código del proyecto es requerido")
                .MaximumLength(50).WithMessage("El código del proyecto no puede superar 50 caracteres");

            RuleFor(x => x.Nombre)
                .NotEmpty().WithMessage("El nombre es requerido")
                .MaximumLength(250).WithMessage("El nombre no puede superar 250 caracteres");

            RuleFor(x => x.Monto)
                .GreaterThanOrEqualTo(0).WithMessage("El monto no puede ser negativo");

            RuleFor(x => x.Estado)
                .NotEmpty().WithMessage("El estado es requerido")
                .MaximumLength(30).WithMessage("El estado no puede superar 30 caracteres");
        }
    }

    public class ProyectosInversionUpdateDtoValidator : AbstractValidator<ProyectosInversionUpdateDto>
    {
        public ProyectosInversionUpdateDtoValidator()
        {
            RuleFor(x => x.Nombre)
                .MaximumLength(250).When(x => !string.IsNullOrWhiteSpace(x.Nombre))
                .WithMessage("El nombre no puede superar 250 caracteres");

            RuleFor(x => x.Monto)
                .GreaterThanOrEqualTo(0).When(x => x.Monto.HasValue)
                .WithMessage("El monto no puede ser negativo");

            RuleFor(x => x.Estado)
                .MaximumLength(30).When(x => !string.IsNullOrWhiteSpace(x.Estado))
                .WithMessage("El estado no puede superar 30 caracteres");
        }
    }
}
