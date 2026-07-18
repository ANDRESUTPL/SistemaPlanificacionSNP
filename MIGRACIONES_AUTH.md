# Migraciones EF Core para Auth (SNP_Auth)

Este documento define el flujo oficial para mantener sincronizados el modelo (entidades + Fluent API) y la base de datos `SNP_Auth`.

## 1. Regla de oro

- Fuente principal: modelo EF Core (entidades + `ApplicationDbContext`).
- Excepciones db-first: solo casos justificados y con reconciliacion obligatoria en codigo.

## 2. Comandos base

Ejecutar desde la raiz de la solucion:

```powershell
dotnet ef migrations add NombreCambioAuth --project .\SistemaPlanificacionSNP.Infrastructure\SistemaPlanificacionSNP.Infrastructure.csproj --startup-project .\SistemaPlanificacionSNP.Auth.Api\SistemaPlanificacionSNP.Auth.Api.csproj --context ApplicationDbContext --output-dir Migrations
```

```powershell
dotnet ef database update --project .\SistemaPlanificacionSNP.Infrastructure\SistemaPlanificacionSNP.Infrastructure.csproj --startup-project .\SistemaPlanificacionSNP.Auth.Api\SistemaPlanificacionSNP.Auth.Api.csproj --context ApplicationDbContext
```

```powershell
dotnet ef migrations script --project .\SistemaPlanificacionSNP.Infrastructure\SistemaPlanificacionSNP.Infrastructure.csproj --startup-project .\SistemaPlanificacionSNP.Auth.Api\SistemaPlanificacionSNP.Auth.Api.csproj --context ApplicationDbContext --idempotent --output .\BasesDeDatos\SNP_Auth\migrations_auth_idempotente.sql
```

## 3. Flujo estandar (code-first)

1. Modificar entidades y/o Fluent API.
2. Crear migracion con nombre descriptivo (ejemplo: `Auth_AjusteLongitudesUsuario`).
3. Revisar archivo de migracion y `ModelSnapshot` antes de aplicar.
4. Aplicar migracion local con `database update`.
5. Probar endpoints criticos (`login`, `refresh-token`, `logout`).
6. Generar script SQL idempotente para despliegue.
7. Versionar codigo + migracion + script.

## 4. Flujo excepcional (db-first)

1. Detectar cambio manual en SQL.
2. Reflejar el cambio en entidad/Fluent API.
3. Generar migracion de reconciliacion para registrar el estado en historial EF.
4. Verificar que no se intenten recrear cambios ya aplicados en el entorno objetivo.

## 5. Validaciones minimas por cambio

- La API de Auth arranca sin errores de mapeo.
- No hay migraciones pendientes inesperadas.
- La consulta por `NombreUsuario` funciona sin error de columnas.
- El log de inicio confirma servidor y base de datos esperados.
