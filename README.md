# Sistema Integral de Planificación e Inversión Pública (SIPeIP)

## Descripción General

**SistemaPlanificacionSNP** es una solución empresarial integral para la gestión de planificación e inversión pública de la Secretaría Nacional de Planificación (SNP). Implementa una **arquitectura de Microservicios** con componentes desacoplados para garantizar escalabilidad, mantenibilidad y seguridad.

## Arquitectura

La solución está estructurada en las siguientes capas y módulos:

### 1. **Capas Transversales**
- **Domain**: Modelos y entidades del dominio (sin dependencias externas)
- **Infrastructure**: Acceso a datos, DbContext, repositorios y patrones de persistencia

### 2. **Microservicios (APIs)**
- **Auth.Api**: Autenticación, generación de JWT y gestión de usuarios
- **Parametrizacion.Api**: Catálogos, períodos y entidades públicas
- **MacroPlanificacion.Api**: Objetivos de Desarrollo Sostenible (ODS) y Planes Nacionales
- **PlanificacionInstitucional.Api**: Planes Estratégicos Institucionales (PEI), objetivos y programas
- **ControlCalidad.Api**: Revisiones y aprobaciones de planificación

### 3. **API Gateway**
- **ApiGateway**: Enrutamiento centralizado, gestión de JWT y seguridad (Ocelot)

### 4. **Frontend**
- **Web**: Aplicación ASP.NET Core MVC con interfaz responsive

## Especificaciones Técnicas

| Característica | Valor |
|---|---|
| **Framework Backend** | .NET Core 8.0 |
| **Base de Datos** | SQL Server 2019 RTM (15.0.2000.5) |
| **ORM** | Entity Framework Core 8.0 |
| **Seguridad (Autenticación)** | JWT (JSON Web Tokens) |
| **Hashing de Contraseñas** | BCrypt |
| **API Gateway** | Ocelot 21.0.0 |
| **Mapeo de Objetos** | AutoMapper 13.0.1 |
| **Framework Frontend** | ASP.NET Core MVC 8.0 |
| **Estilos Frontend** | Bootstrap 5.x |
| **Librerías Frontend** | FontAwesome 7.2 Free, SweetAlert2, JavaScript Nativo |

## Estructura de Carpetas

```
SistemaPlanificacionSNP/
├── SistemaPlanificacionSNP.Domain/
│   └── Entities/
│       ├── Seguridad/
│       ├── Parametrizacion/
│       ├── MacroPlanificacion/
│       └── PlanificacionInstitucional/
├── SistemaPlanificacionSNP.Infrastructure/
│   ├── Data/
│   │   └── ApplicationDbContext.cs
│   ├── Repositories/
│   ├── Migrations/
│   └── Services/
├── SistemaPlanificacionSNP.Auth.Api/
├── SistemaPlanificacionSNP.Parametrizacion.Api/
├── SistemaPlanificacionSNP.MacroPlanificacion.Api/
├── SistemaPlanificacionSNP.PlanificacionInstitucional.Api/
├── SistemaPlanificacionSNP.ControlCalidad.Api/
├── SistemaPlanificacionSNP.ApiGateway/
└── SistemaPlanificacionSNP.Web/
```

## Fases de Desarrollo

### ✅ Fase 1: Modelo de Base de Datos (COMPLETADA)
- Creación de entidades del dominio
- Configuración de DbContext con Fluent API
- Migraciones para SQL Server 2019
- **Documentación**: Ver `FASE1_MIGRACIONES.md`

### 🔄 Fase 2: Gestión de Base de Datos (PRÓXIMO)
- Implementación del patrón Repository
- Lógica criptográfica BCrypt
- Patrón Unit of Work
- Auditoría de cambios

### 📋 Fase 3: APIs y API Gateway (PRÓXIMO)
- DTOs y AutoMapper profiles
- Controladores con autenticación JWT
- Configuración de Ocelot
- Respuestas estandarizadas

### 🎨 Fase 4: Frontend MVC (PRÓXIMO)
- Interfaz responsiva con Bootstrap
- Consumo seguro de APIs
- Menú dinámico por rol/usuario
- Vistas para formulación de PEI

## Entidades Principales

### Seguridad y Accesos
- **Usuario**: Usuarios del sistema con PasswordHash (BCrypt)
- **Rol**: Roles (Planificador, Revisor SNP, etc.)
- **Pantalla**: Pantallas/módulos del sistema
- **RolPermiso**: Permisos por rol y pantalla
- **AuditoriaTransaccional**: Historial de cambios

### Parametrización
- **PeriodoPlanificacion**: Períodos de planificación
- **EntidadPublica**: Entidades públicas participantes
- **Catalogo** / **ItemCatalogo**: Catálogos del sistema

### Flujo de Planificación
- **ObjetivoDesarrolloSostenible (ODS)**: Nivel macro nacional
- **PlanNacionalDesarrollo (PND)**: Planes nacionales
- **PlanEstrategicoInstitucional (PEI)**: Plan de cada entidad
- **ObjetivoEstrategico (OEI)**: Objetivos de la entidad
- **ProgramaPresupuestario**: Programas y presupuestos
- **MatrizIndicador**: Indicadores y metas
- **MetaTerritorial**: Metas por territorio
- **ProyectoInversion**: Proyectos de inversión
- **RevisionSNP**: Revisiones y aprobaciones

## Configuración Inicial

### Requisitos Previos
- Visual Studio 2022 (o superior)
- .NET SDK 8.0
- SQL Server 2019 (o LocalDB)
- Git

### Instalación

1. **Clona el repositorio**
   ```bash
   git clone https://github.com/tu-repositorio/SistemaPlanificacionSNP.git
   cd SistemaPlanificacionSNP
   ```

2. **Instala los paquetes NuGet**
   Ver `FASE1_MIGRACIONES.md` para detalles completos

3. **Configura la cadena de conexión**
   Edita `appsettings.json` en cada proyecto API

4. **Ejecuta las migraciones**
   ```powershell
   Update-Database -Project SistemaPlanificacionSNP.Infrastructure
   ```

5. **Inicia los proyectos**
   - API Gateway en puerto `5000`
   - Auth.Api en puerto `5001`
   - Otros microservicios en puertos específicos

## Convenciones de Código

- **Namespace**: `SistemaPlanificacionSNP.[Módulo].[Capa]`
- **Entidades**: Sufijo sin prefijo (ej: `Usuario`, `Rol`)
- **DTOs**: Sufijo `Dto` (ej: `UsuarioDto`, `CrearUsuarioDto`)
- **Interfaces**: Prefijo `I` (ej: `IRepository<T>`, `IUnitOfWork`)
- **Métodos async**: Sufijo `Async` (ej: `GetUsuarioAsync`)
- **Program.cs**: Sin "Top-level statements" - estructura clásica `namespace`, `class Program`, `Main`

## Seguridad

- ✅ **Contraseñas**: Hasheadas con BCrypt
- ✅ **Autenticación**: JWT con refresh tokens
- ✅ **Autorización**: Por rol y pantalla (dinámico desde BD)
- ✅ **Auditoría**: Registro transaccional de cambios
- ⚠️ **HTTPS**: Obligatorio en producción
- ⚠️ **Variables de Entorno**: Para conexión y JWT secret

## Scripts de Desarrollo

### Generar Migración
```powershell
Add-Migration [NombreMigracion] -Project SistemaPlanificacionSNP.Infrastructure
Update-Database -Project SistemaPlanificacionSNP.Infrastructure
```

### Actualizar Modelos desde BD (Reverse Engineering)
```powershell
Scaffold-DbContext "Connection-String" Microsoft.EntityFrameworkCore.SqlServer -OutputDir Models
```

## Documentación Adicional

- 📄 `FASE1_MIGRACIONES.md` - Instalación de paquetes y migraciones
- 📄 `FASE2_REPOSITORIOS.md` - Repositorios y seguridad (próximo)
- 📄 `FASE3_APIS.md` - APIs y API Gateway (próximo)
- 📄 `FASE4_FRONTEND.md` - Frontend MVC (próximo)

## Contribuciones

Para contribuir:
1. Crea una rama feature (`git checkout -b feature/NombreFeature`)
2. Commit tus cambios (`git commit -m 'Agrega NombreFeature'`)
3. Push a la rama (`git push origin feature/NombreFeature`)
4. Abre un Pull Request

## Licencia

Este proyecto está bajo licencia propietaria de la SNP.

## Contacto

- **Desarrollador Senior**: [Tu nombre/Correo]
- **Documentación**: Ver documentación interna de arquitectura
