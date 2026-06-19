# 📐 DIAGRAMA DE ENTIDADES Y RELACIONES

## Modelo Entidad-Relación (MER)

```
╔════════════════════════════════════════════════════════════════════════════════╗
║                          SISTEMA DE SEGURIDAD Y ACCESOS                        ║
╚════════════════════════════════════════════════════════════════════════════════╝

    ┌─────────────────┐         ┌──────────────────┐
    │    Usuario      │◄────────┤   UsuarioRol     │────────►┌─────────────────┐
    ├─────────────────┤  1:N    └──────────────────┘  N:1    │      Rol        │
    │ UsuarioId (PK)  │                                        ├─────────────────┤
    │ NombreUsuario*  │                                        │ RolId (PK)      │
    │ Email*          │                                        │ Nombre*         │
    │ PasswordHash*   │◄───────────────┐                       │ Descripcion     │
    │ Nombre          │                │                       └─────────────────┤
    │ Apellido        │                │                            │
    │ Activo          │                │                            │ 1:N
    │ FechaCreacion   │                │                            │
    │ FechaUltimoLogin│                │                            ▼
    │ RefreshToken    │                │       ┌──────────────────────────────────┐
    │ RefreshTokenExp │                │       │      RolPermiso                  │
    └─────────────────┤                │       ├──────────────────────────────────┤
           │          │ 1:N            │       │ RolPermisoId (PK)               │
           │          │                │       │ RolId (FK)                      │
           │          │                │       │ PantallaId (FK)                 │
           │          └────────────────┼──────►│ Lectura                         │
           │                           │       │ Creacion                        │
           │                           │       │ Edicion                         │
           │                           │       │ Eliminacion                     │
           │                           │       │ FechaCreacion                   │
           │ 1:N    ┌──────────────────┼────────┼──────────────────────────────────┤
           └───────►│ AuditoriaTransaccion       │             │
                    ├──────────────────┐        │             │ N:M
                    │ AuditoriaId (PK) │        │             │
                    │ UsuarioId (FK)   │        │             ▼
                    │ Entidad*         │        │    ┌─────────────────────┐
                    │ TipoOperacion*   │        │    │      Pantalla       │
                    │ IdRegistro       │        │    ├─────────────────────┤
                    │ DatosAnteriores  │        │    │ PantallaId (PK)     │
                    │ DatosNuevos      │        │    │ Nombre*             │
                    │ FechaOperacion*  │        │    │ Ruta*               │
                    │ Descripcion      │        │    │ Icono               │
                    └──────────────────┘        │    │ PantallaPadrId (FK) │◄─────┐
                                                │    │ Orden               │      │
                                                │    │ Activo              │    1:N
                                                │    │ FechaCreacion       │      │
                                                └────┼─────────────────────┤      │
                                                     │ (Jerarquía)         │──────┘
                                                     └─────────────────────┘

╔════════════════════════════════════════════════════════════════════════════════╗
║                      SISTEMA DE PARAMETRIZACIÓN                                ║
╚════════════════════════════════════════════════════════════════════════════════╝

    ┌──────────────────────────┐              ┌───────────────────────┐
    │ PeriodoPlanificacion     │────1:N───────│  EntidadPublica       │
    ├──────────────────────────┤              ├───────────────────────┤
    │ PeriodoPlanificacionId(PK)              │ EntidadPublicaId (PK) │
    │ Codigo*                  │              │ Codigo*               │
    │ Nombre*                  │              │ Nombre*               │
    │ FechaInicio              │              │ Sigla*                │
    │ FechaFin                 │              │ Mision*               │
    │ Activo                   │              │ PeriodoPlanificacionId│
    │ FechaCreacion            │              │ Activo                │
    └──────────────────────────┘              │ FechaCreacion         │
                                              └───────────────────────┤
                                                     │
    ┌──────────────────────┐                        │ 1:N
    │    Catalogo          │◄───────────────────────┘
    ├──────────────────────┤        (Vinculado a período)
    │ CatalogoId (PK)      │
    │ Codigo*              │        Nota: EntidadPublica vinculada a período,
    │ Nombre*              │        pero se comparte para todos los años
    │ Descripcion          │        de planificación
    │ Activo               │
    │ FechaCreacion        │
    └──────────────────────┤
         │                 │
       1:N                 │
         │                 │
         ▼                 │
    ┌──────────────────────────┐
    │   ItemCatalogo           │
    ├──────────────────────────┤
    │ ItemCatalogoId (PK)      │
    │ CatalogoId (FK)          │
    │ Codigo*                  │
    │ Nombre*                  │
    │ Descripcion              │
    │ Orden                    │
    │ Activo                   │
    │ FechaCreacion            │
    └──────────────────────────┘

╔════════════════════════════════════════════════════════════════════════════════╗
║                    MACROPLANIFICACIÓN (NIVEL SNP)                             ║
╚════════════════════════════════════════════════════════════════════════════════╝

    ┌──────────────────────────────────┐
    │ ObjetivoDesarrolloSostenible      │
    ├──────────────────────────────────┤
    │ OdsId (PK)                       │
    │ Codigo*                          │
    │ Nombre*                          │
    │ Descripcion                      │
    │ Activo                           │
    │ FechaCreacion                    │
    └──────────────────────────────────┤
           │                           │
         1:N                           │
           │                           │
           ▼                           │
    ┌──────────────────────────────────┴──┐
    │ PlanNacionalDesarrollo              │
    ├─────────────────────────────────────┤
    │ PndId (PK)                          │
    │ Codigo*                             │
    │ Nombre*                             │
    │ Descripcion                         │
    │ OdsId (FK)                          │
    │ Activo                              │
    │ FechaCreacion                       │
    └─────────────────────────────────────┘

╔════════════════════════════════════════════════════════════════════════════════╗
║              PLANIFICACIÓN INSTITUCIONAL (NIVEL ENTIDAD)                       ║
╚════════════════════════════════════════════════════════════════════════════════╝

    ┌────────────────────────────────────┐
    │ PlanEstrategicoInstitucional (PEI) │
    ├────────────────────────────────────┤
    │ PeiId (PK)                         │
    │ Codigo*                            │
    │ Nombre*                            │
    │ Descripcion                        │
    │ EntidadPublicaId (FK)              │◄─────────────┐
    │ FechaInicio                        │              │
    │ FechaFin                           │              │
    │ Estado* (Borrador/Enviado/Aprobado)              │
    │ Activo                             │              │
    │ FechaCreacion                      │              │
    └────────────────────────────────────┤              │
         │                               │              │
         │ 1:N (Cascade)                 │              │
         │                               │         EntidadPublica
         ▼                               │         (Parametrización)
    ┌──────────────────────────────┐    │              │
    │ ObjetivoEstrategico (OEI)    │    │              │
    ├──────────────────────────────┤    │              │
    │ OeiId (PK)                   │    │              │
    │ Codigo*                      │    │              │
    │ Nombre*                      │    │              │
    │ Descripcion                  │    │              │
    │ PeiId (FK)                   │    │              │
    │ PndId (FK) [Opcional]        │────┼──────────────┘
    │ Orden                        │    │
    │ Activo                       │    │
    │ FechaCreacion                │    │
    └──────────────────────────────┤    │
         │                         │    │
         │ 1:N (Cascade)           │    │
         │                         │    │
         ▼                         │    │
    ┌────────────────────────────────────────────┐
    │ ProgramaPresupuestario                     │
    ├────────────────────────────────────────────┤
    │ ProgramaId (PK)                            │
    │ Codigo*                                    │
    │ Nombre*                                    │
    │ Descripcion                                │
    │ OeiId (FK)                                 │
    │ PresupuestoAsignado (Decimal 18,2)         │
    │ Orden                                      │
    │ Activo                                     │
    │ FechaCreacion                              │
    └────────────────────────────────────────────┤
         │                    │
         │ 1:N               │ 1:N
         │                   │
         ▼                   ▼
    ┌────────────────────┐  ┌──────────────────┐
    │ MatrizIndicador    │  │ ProyectoInversion│
    ├────────────────────┤  ├──────────────────┤
    │ MatrizIndicadorId  │  │ ProyectoId (PK)  │
    │ Codigo*            │  │ Codigo*          │
    │ Nombre*            │  │ Nombre*          │
    │ Descripcion        │  │ Descripcion      │
    │ ProgramaId (FK)    │  │ ProgramaId (FK)  │
    │ TipoIndicador      │  │ CostoTotal       │
    │ Unidad             │  │ FechaInicio      │
    │ ValorBase          │  │ FechaFin         │
    │ ValorMeta          │  │ Estado*          │
    │ Orden              │  │ Activo           │
    │ Activo             │  │ FechaCreacion    │
    │ FechaCreacion      │  └──────────────────┘
    └────────────────────┤
         │               │
         │ 1:N (Cascade) │
         │               │
         ▼               │
    ┌────────────────────┴─────────────────┐
    │ MetaTerritorial                      │
    ├────────────────────────────────────┐ │
    │ MetaTerritorialId (PK)             │ │
    │ MatrizIndicadorId (FK)             │ │
    │ Territorio*                        │ │
    │ MetaFisica (Decimal 18,4)          │ │
    │ MetaFinanciera (Decimal 18,2)      │ │
    │ Orden                              │ │
    │ Activo                             │ │
    │ FechaCreacion                      │ │
    └────────────────────────────────────┘ │
                                            │
    (Metas por territorio: Departamento,    │
     Provincia, Municipio, etc.)           │

╔════════════════════════════════════════════════════════════════════════════════╗
║                      RETROALIMENTACIÓN Y AUDITORÍA                             ║
╚════════════════════════════════════════════════════════════════════════════════╝

    ┌──────────────────────┐
    │  PlanEstrategico     │
    │  Institucional (PEI) │
    └──────────────────────┤
           │               │
           │ 1:N (Cascade) │
           │               │
           ▼               │
    ┌──────────────────────────────────┐
    │ RevisionSNP                      │
    ├──────────────────────────────────┤
    │ RevisionId (PK)                  │
    │ PeiId (FK)                       │
    │ Estado* (Pendiente/Aprobado...)  │
    │ Comentarios                      │
    │ UsuarioRevisor (FK) [Opcional]   │◄────────────────┐
    │ FechaRevision                    │                 │
    │ Activo                           │                 │
    └──────────────────────────────────┘            Usuario
                                                  (Revisor)

═════════════════════════════════════════════════════════════════════════════════

LEYENDA:
────► Relación 1:N (Uno a Muchos)
◄───── Relación N:1 (Muchos a Uno)
N:M    Relación Muchos a Muchos (vía tabla intermedia)
*      Campo requerido
(FK)   Clave Foránea
(PK)   Clave Primaria

ÍNDICES ÚNICOS CREADOS:
├─ Usuario.NombreUsuario
├─ Usuario.Email
├─ Rol.Nombre
├─ PeriodoPlanificacion.Codigo
├─ EntidadPublica.Codigo
├─ Catalogo.Codigo
├─ ItemCatalogo (CatalogoId, Codigo)
├─ ObjetivoDesarrolloSostenible.Codigo
├─ PlanNacionalDesarrollo.Codigo
├─ PlanEstrategicoInstitucional.Codigo
├─ ProyectoInversion.Codigo
└─ RolPermiso (RolId, PantallaId)

RELACIONES CON CASCADA (DELETE CASCADE):
├─ Rol → UsuarioRoles
├─ Rol → RolPermisos
├─ Catalogo → ItemsCatalogo
├─ ObjetivoDesarrolloSostenible → PlanesNacionalesDesarrollo
├─ PlanEstrategicoInstitucional → ObjetivosEstrategicos
├─ PlanEstrategicoInstitucional → Revisiones
├─ ObjetivoEstrategico → ProgramasPresupuestarios
├─ ProgramaPresupuestario → MatricesIndicadores
├─ ProgramaPresupuestario → ProyectosInversion
├─ MatrizIndicador → MetasTerritorial
└─ Pantalla → RolPermisos
