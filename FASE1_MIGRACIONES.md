# Fase 1: Modelo de Base de Datos (SQL Server 2019) y Migraciones

## Resumen
Este documento proporciona los pasos para instalar los paquetes NuGet necesarios y generar la migración inicial de la base de datos para SQL Server 2019.

## 1. Instalación de Paquetes NuGet

### Para el Proyecto: SistemaPlanificacionSNP.Infrastructure

Ejecuta los siguientes comandos en la **Consola del Administrador de Paquetes NuGet** (Package Manager Console) en Visual Studio:

```powershell
# Asegúrate de que el proyecto SistemaPlanificacionSNP.Infrastructure esté seleccionado como proyecto predeterminado

# Instalar Entity Framework Core
Install-Package Microsoft.EntityFrameworkCore -Version 8.0.0

# Instalar el proveedor de SQL Server
Install-Package Microsoft.EntityFrameworkCore.SqlServer -Version 8.0.0

# Instalar herramientas de Entity Framework Core (incluye migración)
Install-Package Microsoft.EntityFrameworkCore.Tools -Version 8.0.0

# Instalar paquete de utilidades comunes
Install-Package Microsoft.EntityFrameworkCore.Design -Version 8.0.0
```

### Para los Proyectos de API (Auth.Api, Parametrizacion.Api, etc.)

```powershell
# Instalar autenticación JWT
Install-Package Microsoft.AspNetCore.Authentication.JwtBearer -Version 8.0.0

# Instalar BCrypt para hashing seguro de contraseñas
Install-Package BCrypt.Net-Next -Version 4.0.3

# Instalar AutoMapper para mapeo de DTOs
Install-Package AutoMapper -Version 13.0.1
Install-Package AutoMapper.Extensions.Microsoft.DependencyInjection -Version 12.0.1
```

### Para el API Gateway (Ocelot)

```powershell
# Instalar Ocelot para el API Gateway
Install-Package Ocelot -Version 21.0.0
```

## 2. Configuración de la Cadena de Conexión

Edita el archivo `appsettings.json` en el proyecto correspondiente:

### Opción 1: Usar LocalDB (Desarrollo Local)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(LocalDb)\\MSSQLLocalDB;Database=SistemaPlanificacionSNP;Trusted_Connection=true;Encrypt=false;"
  }
}
```

### Opción 2: Usar SQL Server 2019 Remoto
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER_NAME;Database=SistemaPlanificacionSNP;User Id=sa;Password=YOUR_PASSWORD;Encrypt=false;"
  }
}
```

**Reemplaza:**
- `YOUR_SERVER_NAME`: Nombre del servidor SQL Server (ej: "localhost", "192.168.1.100", "sqlserver.dominio.com")
- `YOUR_PASSWORD`: Contraseña de la cuenta `sa`

## 3. Generar la Migración Inicial

### Paso 1: Abre la Consola del Administrador de Paquetes NuGet
En Visual Studio: **Herramientas > Administrador de Paquetes NuGet > Consola del Administrador de Paquetes**

### Paso 2: Asegúrate de que el Proyecto Correcto esté Seleccionado
En el dropdown de la consola, selecciona `SistemaPlanificacionSNP.Infrastructure`

### Paso 3: Ejecuta el Comando de Migración
```powershell
Add-Migration InitialCreate -Project SistemaPlanificacionSNP.Infrastructure
```

Este comando creará un archivo de migración en la carpeta `Migrations/` con el nombre: `[timestamp]_InitialCreate.cs`

### Paso 4: Aplicar la Migración a la Base de Datos
```powershell
Update-Database -Project SistemaPlanificacionSNP.Infrastructure
```

Esto creará la base de datos `SistemaPlanificacionSNP` en tu servidor SQL Server con todas las tablas.

## 4. Verificación

Para verificar que la migración se aplicó correctamente:

1. Abre **SQL Server Management Studio (SSMS)**
2. Conéctate al servidor SQL Server
3. Verifica que exista la base de datos `SistemaPlanificacionSNP`
4. Expande la base de datos y verifica que contenga las siguientes tablas:
   - Usuarios
   - Roles
   - UsuarioRoles
   - Pantallas
   - RolPermisos
   - Auditorias
   - PeriodosPlanificacion
   - EntidadesPublicas
   - Catalogos
   - ItemsCatalogo
   - ObjetivosDesarrolloSostenible
   - PlanesNacionalesDesarrollo
   - PlanesEstrategicos
   - ObjetivosEstrategicos
   - ProgramasPresupuestarios
   - MatricesIndicadores
   - MetasTerritorial
   - ProyectosInversion
   - Revisiones

## 5. Comandos Adicionales Útiles

### Crear una nueva migración después de cambios en el modelo
```powershell
Add-Migration NombreDeLaMigracion -Project SistemaPlanificacionSNP.Infrastructure
```

### Ver el estado de las migraciones
```powershell
Get-Migration -Project SistemaPlanificacionSNP.Infrastructure
```

### Revertir la última migración
```powershell
Update-Database -Migration [NombreDeLaMigracionAnterior] -Project SistemaPlanificacionSNP.Infrastructure
```

### Generar SQL de la migración sin aplicarla
```powershell
Script-Migration -From 0 -To InitialCreate -Project SistemaPlanificacionSNP.Infrastructure
```

## Notas Importantes

- **Seguridad**: La cadena de conexión usa `Encrypt=false` solo para desarrollo. En producción, debes usar `Encrypt=true` con certificados SSL/TLS válidos.
- **Estructura de Capas**: 
  - `Domain`: Contiene solo las entidades del dominio (sin lógica)
  - `Infrastructure`: Contiene el `DbContext`, configuraciones de Fluent API y migraciones
  - APIs: Proyectos que exponen los endpoints y utilizan la infraestructura
- **Fluent API**: Todas las configuraciones de relaciones y restricciones están en el método `OnModelCreating` del `ApplicationDbContext`
