Este archivo documenta la estructura completada en Fase 1.

# 📊 ESTRUCTURA DE PROYECTOS - FASE 1 COMPLETADA

## Árbol de Archivos Generados

```
c:\ProyectoAndres\SistemaPlanificacionSNP\
│
├── SistemaPlanificacionSNP.sln ⬅️ SOLUCIÓN PRINCIPAL
│
├── SistemaPlanificacionSNP.Domain\
│   ├── SistemaPlanificacionSNP.Domain.csproj
│   ├── Program.cs
│   └── Entities\
│       ├── Seguridad\
│       │   ├── Usuario.cs
│       │   ├── Rol.cs
│       │   ├── UsuarioRol.cs
│       │   ├── Pantalla.cs
│       │   ├── RolPermiso.cs
│       │   └── AuditoriaTransaccional.cs
│       ├── Parametrizacion\
│       │   ├── PeriodoPlanificacion.cs
│       │   ├── EntidadPublica.cs
│       │   ├── Catalogo.cs
│       │   └── ItemCatalogo.cs
│       ├── MacroPlanificacion\
│       │   ├── ObjetivoDesarrolloSostenible.cs
│       │   └── PlanNacionalDesarrollo.cs
│       └── PlanificacionInstitucional\
│           ├── PlanEstrategicoInstitucional.cs
│           ├── ObjetivoEstrategico.cs
│           ├── ProgramaPresupuestario.cs
│           ├── MatrizIndicador.cs
│           ├── MetaTerritorial.cs
│           ├── ProyectoInversion.cs
│           └── RevisionSNP.cs
│
├── SistemaPlanificacionSNP.Infrastructure\
│   ├── SistemaPlanificacionSNP.Infrastructure.csproj
│   ├── Program.cs
│   └── Data\
│       └── ApplicationDbContext.cs ⬅️ 19 ENTIDADES CONFIGURADAS
│
├── SistemaPlanificacionSNP.Auth.Api\
│   ├── SistemaPlanificacionSNP.Auth.Api.csproj
│   └── appsettings.json
│
├── SistemaPlanificacionSNP.Parametrizacion.Api\
│   ├── SistemaPlanificacionSNP.Parametrizacion.Api.csproj
│   └── appsettings.json
│
├── SistemaPlanificacionSNP.MacroPlanificacion.Api\
│   ├── SistemaPlanificacionSNP.MacroPlanificacion.Api.csproj
│   └── appsettings.json
│
├── SistemaPlanificacionSNP.PlanificacionInstitucional.Api\
│   ├── SistemaPlanificacionSNP.PlanificacionInstitucional.Api.csproj
│   └── appsettings.json
│
├── SistemaPlanificacionSNP.ControlCalidad.Api\
│   ├── SistemaPlanificacionSNP.ControlCalidad.Api.csproj
│   └── appsettings.json
│
├── SistemaPlanificacionSNP.ApiGateway\
│   ├── SistemaPlanificacionSNP.ApiGateway.csproj
│   └── appsettings.json
│
├── SistemaPlanificacionSNP.Web\
│   ├── SistemaPlanificacionSNP.Web.csproj
│   └── appsettings.json
│
├── README.md ⬅️ Descripción general
├── INICIO_RAPIDO.md ⬅️ Instrucciones de inicio
├── FASE1_MIGRACIONES.md ⬅️ Instalación NuGet y migraciones
├── RESUMEN_FASE1.md ⬅️ Resumen completo de Fase 1
├── ESTRUCTURA.md ⬅️ Este archivo
│
└── .gitignore ⬅️ Configuración Git
```

## Estadísticas

| Métrica | Cantidad |
|---------|----------|
| **Proyectos** | 9 (2 librerías + 7 APIs) |
| **Entidades de Dominio** | 19 |
| **Archivos de Entidades** | 19 |
| **DbSets en Context** | 19 |
| **Relaciones Configuradas** | 25+ |
| **Índices Únicos** | 15+ |
| **Archivos appsettings.json** | 7 |
| **Documentos de Configuración** | 5 |
| **Paquetes NuGet Base** | 8 |

## Entidades por Categoría

### Seguridad (6 entidades)
1. Usuario (con PasswordHash)
2. Rol
3. UsuarioRol
4. Pantalla (jerárquica)
5. RolPermiso (CRUD dinámico)
6. AuditoriaTransaccional

### Parametrización (4 entidades)
7. PeriodoPlanificacion
8. EntidadPublica
9. Catalogo
10. ItemCatalogo

### Macro Planificación (2 entidades)
11. ObjetivoDesarrolloSostenible
12. PlanNacionalDesarrollo

### Planificación Institucional (7 entidades)
13. PlanEstrategicoInstitucional
14. ObjetivoEstrategico
15. ProgramaPresupuestario
16. MatrizIndicador
17. MetaTerritorial
18. ProyectoInversion
19. RevisionSNP

## Configuraciones Fluent API

Cada entidad tiene:
- ✓ Clave primaria (HasKey)
- ✓ Propiedades requeridas (IsRequired)
- ✓ Restricciones de longitud (HasMaxLength)
- ✓ Índices únicos (IsUnique)
- ✓ Relaciones configuradas (HasMany, WithOne, HasForeignKey)
- ✓ Comportamiento de eliminación (OnDelete)
- ✓ Precisión de decimales (HasPrecision)
- ✓ Tipos de columna específicos (HasColumnType)

## Relaciones Principales

### Jerárquicas
- Pantalla → PantallaPadre (auto-referencia)
- PlanEstrategicoInstitucional → ObjetivosEstrategicos → ProgramasPresupuestarios

### De Seguridad
- Usuario ↔ Rol (muchos a muchos vía UsuarioRol)
- Rol ↔ Pantalla (muchos a muchos vía RolPermiso)

### De Planificación
- EntidadPublica → PlanEstrategicoInstitucional
- PlanNacionalDesarrollo ← ObjetivoEstrategico
- ObjetivoDesarrolloSostenible → PlanNacionalDesarrollo

### De Indicadores
- ProgramaPresupuestario → MatrizIndicador → MetaTerritorial
- ProgramaPresupuestario → ProyectoInversion

### De Auditoría
- Usuario → AuditoriaTransaccional
- Usuario → RevisionSNP (como revisor)

## Paquetes NuGet Incluidos

```xml
<!-- Infrastructure -->
Microsoft.EntityFrameworkCore v8.0.0
Microsoft.EntityFrameworkCore.SqlServer v8.0.0
Microsoft.EntityFrameworkCore.Tools v8.0.0

<!-- Auth & Security -->
Microsoft.AspNetCore.Authentication.JwtBearer v8.0.0
BCrypt.Net-Next v4.0.3

<!-- Mapping -->
AutoMapper v13.0.1
AutoMapper.Extensions.Microsoft.DependencyInjection v12.0.1

<!-- Gateway -->
Ocelot v21.0.0
```

## Configuración por Proyecto

### Domain
- Proyecto de clase de librería
- Solo contiene entidades (sin lógica)
- Sin dependencias externas

### Infrastructure
- Proyecto de clase de librería
- Contiene ApplicationDbContext
- Contendrá repositorios en Fase 2
- Depende de Domain

### Auth.Api
- WebAPI
- Depende de Infrastructure y Domain
- Contendrá AuthController (Fase 3)

### Otros APIs
- Misma estructura que Auth.Api
- Cada uno para un módulo específico

### ApiGateway
- WebAPI
- Usa Ocelot para enrutamiento
- Valida JWT centralmente

### Web
- ASP.NET Core MVC
- Frontend de la aplicación
- Consume APIs a través del gateway

## Convenciones Aplicadas

✓ Namespaces: `SistemaPlanificacionSNP.[Módulo].[Capa]`
✓ Entidades: Sin prefijo/sufijo (Usuario, Rol)
✓ Program.cs: Estructura clásica sin Top-level statements
✓ DTOs: Sufijo Dto (próximo en Fase 3)
✓ Interfaces: Prefijo I (próximo en Fase 2)
✓ Métodos async: Sufijo Async (próximo en Fase 3)
✓ Índices: Configurados para performance
✓ Cascadas: Configuradas apropiadamente

## Próximos Archivos (Fase 2)

```
Repositories/
├── IRepository.cs
├── Repository.cs
├── IPlanificacionRepository.cs
├── PlanificacionRepository.cs
├── IAuditoriaRepository.cs
├── AuditoriaRepository.cs
├── IUsuarioRepository.cs
└── UsuarioRepository.cs

UnitOfWork/
├── IUnitOfWork.cs
└── UnitOfWork.cs

Services/
├── PasswordHashService.cs
└── AuditoriaService.cs
```

## Verificación Post-Migración

Después de ejecutar `Update-Database`, verificar en SQL Server:

```sql
-- Ver todas las tablas
SELECT name FROM sys.tables 
WHERE database_id = DB_ID('SistemaPlanificacionSNP')
ORDER BY name;

-- Ver relaciones
SELECT * FROM sys.foreign_keys 
WHERE database_id = DB_ID('SistemaPlanificacionSNP');

-- Ver índices
SELECT * FROM sys.indexes 
WHERE database_id = DB_ID('SistemaPlanificacionSNP');
```

## Estado Actual

- ✅ Estructura de solución
- ✅ 19 entidades del dominio
- ✅ ApplicationDbContext con Fluent API
- ✅ appsettings.json configurados
- ✅ Documentación completa
- ⏳ Migraciones (ejecutar manualmente)
- ⏳ Repositorios (Fase 2)
- ⏳ APIs y DTOs (Fase 3)
- ⏳ Frontend MVC (Fase 4)

---

**Fase 1**: ✅ COMPLETADA

Próximo: **Fase 2 - Gestión de Base de Datos (Repositorios y Seguridad)**
